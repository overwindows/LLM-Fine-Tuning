import torch
import transformers
from transformers import TrainingArguments, DataCollatorWithPadding
from datasets import load_dataset, Dataset
from utils import ModifiedTrainer, data_collator, \
    data_collator_ex, conv_gen
from utils import ModelArguments, DataArguments
from transformers import AutoTokenizer, LlamaForCausalLM, AutoModelForCausalLM, Trainer


def main():
    parser = transformers.HfArgumentParser(
        (ModelArguments, DataArguments, TrainingArguments)
    )
    model_args, data_args, training_args = parser.parse_args_into_dataclasses()
    training_type = model_args.training_type
    # device = torch.device("cuda") if torch.cuda.is_available() else torch.device("cpu")

    model_name = model_args.model_name_or_path
    tokenizer = AutoTokenizer.from_pretrained(
        f"{model_name}", add_prefix_space=True
    )
    tokenizer.pad_token = tokenizer.eos_token
    model = LlamaForCausalLM.from_pretrained(
        f"{model_name}", use_cache=False).cuda()

    data_name = data_args.data_name_or_path

    if training_type == "conversation_lm":
        dataset = Dataset.from_generator(
            conv_gen, gen_kwargs={"data_files": data_name})
    else:
        dataset = load_dataset("json", data_files=data_name,
                               cache_dir='/import/snvm-sc-podscratch3/chenw/datasets', streaming=True, split='train')
    dataset = dataset.with_format('torch')

    def preprocess_function(example):
        return tokenizer(example['completion'], truncation=True, max_length=1024, padding="max_length")

    def preprocess_function_ex(example):
        # Encode the prompts and completions together
        encoding = tokenizer.encode_plus(
            example['prompt'],
            example['completion'],
            truncation=True,
            max_length=1024,
            padding="max_length",
            return_tensors="pt"
        )
        # Prepare the labels by shifting the tokens, setting the prompt tokens to -100
        labels = encoding.input_ids.clone()
        labels[:, :encoding['prompt_length']] = -100
        return {
            'input_ids': encoding.input_ids,
            'attention_mask': encoding.attention_mask,
            'labels': labels
        }

    def tokenize_data(example):
        input_idss = []
        attention_masks = []
        labelss = []
        for prompt_completion in example['conv']:
            prompt = prompt_completion["prompt"]
            completion = prompt_completion["completion"]

            prompt_encoding = tokenizer.encode(prompt, truncation=False, max_length=2048, add_special_tokens=False)
            prompt_length = len(prompt_encoding)

            encoding = tokenizer.encode_plus(
                prompt,
                completion,
                truncation=True,
                max_length=1024,
                padding="max_length",
                return_tensors="pt",
                return_attention_mask=True,
            )
            labels = encoding.input_ids.clone()
            labels[:, :prompt_length] = -100

            input_idss.append(encoding.input_ids)
            attention_masks.append(encoding.attention_mask)
            labelss.append(labels)

            # print(len(input_idss), len(attention_masks), len(labelss))
            # print(attention_masks[0].shape)
            concat_attention_masks = torch.cat(attention_masks, dim=-1)
            concat_input_ids = torch.cat(input_idss, dim=-1)
            concat_labels = torch.cat(labelss, dim=-1)
            # print(concat_attention_masks.shape, concat_input_ids.shape, concat_labels.shape)
        return {
            'input_ids': concat_input_ids.squeeze(),
            'attention_mask': concat_attention_masks.squeeze(),
            'labels': concat_labels.squeeze()
        }

    if training_type == "causal_lm":
        tokenized_dataset = dataset.map(preprocess_function, batched=True)
    elif training_type == "instruction_lm":
        tokenized_dataset = dataset.map(preprocess_function_ex, batched=True)
    elif training_type == "conversation_lm":
        # print(dataset)
        # print(dataset[0]['conv'], len(dataset[0]['conv']))
        # print(dataset[0]['conv'][1])
        tokenized_dataset = dataset.map(tokenize_data, batched=False)
    else:
        raise ValueError("Invalid training type")

    model.gradient_checkpointing_enable()
    model.is_parallelizable = True
    model.model_parallel = True
    model.cuda()
    model.train()
    # print(model.generation_config)
    if training_type == "causal_lm":
        trainer = ModifiedTrainer(
            model=model,
            train_dataset=tokenized_dataset,
            args=training_args,
            data_collator=data_collator_ex,
        )
    elif training_type == "instruction_lm":
        trainer = Trainer(
            model=model,
            train_dataset=tokenized_dataset,
            args=training_args,
        )
    elif training_type == "conversation_lm":
        trainer = Trainer(
            model=model,
            train_dataset=tokenized_dataset,
            args=training_args,
        )
    else:
        raise ValueError("Invalid training type")

    trainer.train()


if __name__ == "__main__":
    main()
