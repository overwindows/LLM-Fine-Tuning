import transformers
from transformers import TrainingArguments
from datasets import load_dataset, Dataset
from utils import ModifiedTrainer, data_collator_ex, conv_gen, ConvTrainer
from utils import ModelArguments, DataArguments, tokenize_data
from transformers import AutoTokenizer, LlamaForCausalLM, Trainer



def main():
    parser = transformers.HfArgumentParser(
        (ModelArguments, DataArguments, TrainingArguments)
    )
    model_args, data_args, training_args = parser.parse_args_into_dataclasses()
    training_type = model_args.training_type
    data_cache_dir = data_args.data_cache_dir
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
                               cache_dir=data_cache_dir, streaming=True, split='train')
    dataset = dataset.with_format('torch')
    def preprocess_function(example):
        return tokenizer(example['completion'], truncation=True, max_length=1024, padding="max_length")
    def preprocess_function_ex(example):
        # Encode the prompts and completions together
        encoding = tokenizer.encode_plus(
            example['prompt'],
            example['completion'],
            truncation=True,
            max_length=2048,
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
    if training_type == "causal_lm":
        tokenized_dataset = dataset.map(preprocess_function, batched=True)
    elif training_type == "instruction_lm":
        tokenized_dataset = dataset.map(preprocess_function_ex, batched=True)
    elif training_type == "conversation_lm":
        # print(dataset)
        # print(dataset[0]['conv'], len(dataset[0]['conv']))
        # print(dataset[0]['conv'][1])
        tokenized_dataset = dataset.map(tokenize_data, batched=False, fn_kwargs={
                                        'tokenizer': tokenizer, 'max_length': 1024})
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
        trainer = ConvTrainer(
            model=model,
            train_dataset=tokenized_dataset,
            args=training_args,
        )
    else:
        raise ValueError("Invalid training type")
    trainer.train()


if __name__ == "__main__":
    main()
