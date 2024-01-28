import torch
import transformers
from transformers import TrainingArguments, DataCollatorWithPadding
from datasets import load_dataset
from utils import ModifiedTrainer, data_collator, tokenize_data, data_collator_ex
from utils import ModelArguments, DataArguments
from transformers import AutoTokenizer, LlamaForCausalLM, AutoModelForCausalLM


def main():
    parser = transformers.HfArgumentParser(
        (ModelArguments, DataArguments, TrainingArguments)
    )
    model_args, data_args, training_args = parser.parse_args_into_dataclasses()
    # assert torch.cuda.is_available()
    # device = torch.device("cuda") if torch.cuda.is_available() else torch.device("cpu")

    model_name = model_args.model_name_or_path
    tokenizer = AutoTokenizer.from_pretrained(
        f"{model_name}", add_prefix_space=True
    )
    tokenizer.pad_token = tokenizer.eos_token
    model = LlamaForCausalLM.from_pretrained(
        f"{model_name}", use_cache=False).cuda()

    data_name = data_args.data_name_or_path
    dataset = load_dataset("json", data_files=data_name,
                           cache_dir='/import/snvm-sc-podscratch3/chenw/datasets', streaming=True, split='train')
    dataset = dataset.with_format('torch')

    def preprocess_function(example):
        return tokenizer(example['completion'], truncation=True, max_length=1024, padding="max_length")

    tokenized_dataset = dataset.map(preprocess_function, batched=True)

    model.gradient_checkpointing_enable()
    model.is_parallelizable = True
    model.model_parallel = True
    model.cuda()
    model.train()
    # print(model.generation_config)
    # print(next(iter(tokenized_dataset)))
    trainer = ModifiedTrainer(
        model=model,
        train_dataset=tokenized_dataset,
        args=training_args,
        data_collator=data_collator_ex,
    )
    trainer.train()


if __name__ == "__main__":
    main()
