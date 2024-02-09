from dataclasses import dataclass, field
from typing import List, Optional
import torch
import json
from transformers import Trainer


class ModifiedTrainer(Trainer):
    def compute_loss(self, model, inputs, return_outputs=False):
        return model(
            input_ids=inputs["input_ids"],
            attention_mask=torch.ones_like(inputs["input_ids"]).bool(),
            labels=inputs["input_ids"],
            use_cache=False
        ).loss


class ConvTrainer(Trainer):
    def compute_loss(self, model, inputs, return_outputs=False):
        loss = model(
            input_ids=inputs["input_ids"],
            attention_mask=inputs["attention_mask"],
            labels=inputs["labels"],
            use_cache=False
        ).loss
        # print(loss, loss.shape, inputs["input_ids"].shape)
        return loss


def tokenize_conv_data(example, tokenizer, max_length):
    input_idss = []
    attention_masks = []
    labelss = []
    for prompt_completion in example['conv']:
        prompt = prompt_completion["prompt"]
        completion = prompt_completion["completion"]

        prompt_encoding = tokenizer.encode(
            prompt, truncation=True,
            add_special_tokens=False
        )
        prompt_length = len(prompt_encoding)
        encoding = tokenizer.encode_plus(
            prompt,
            completion,
            truncation=True,
            add_special_tokens=False,
            return_tensors="pt",
            return_attention_mask=True,
        )
        labels = encoding.input_ids.clone()
        labels[:, :prompt_length] = -100

        input_idss.append(encoding.input_ids)
        attention_masks.append(encoding.attention_mask)
        labelss.append(labels)

    assert len(input_idss) == len(attention_masks) == len(labelss)

    concat_attention_masks = torch.cat(attention_masks, dim=-1)
    concat_input_ids = torch.cat(input_idss, dim=-1)
    concat_labels = torch.cat(labelss, dim=-1)

    conv_input_ids = concat_input_ids.squeeze()
    conv_attention_masks = concat_attention_masks.squeeze()
    conv_labels = concat_labels.squeeze()

    fixed_length = 2048
    padded_input_ids = torch.nn.functional.pad(
        conv_input_ids, (0, fixed_length - len(conv_input_ids)), "constant", 0)
    padded_attention_masks = torch.nn.functional.pad(
        conv_attention_masks, (0, fixed_length - len(conv_attention_masks)), "constant", 0)
    padded_labels = torch.nn.functional.pad(
        conv_labels, (0, fixed_length - len(conv_labels)), "constant", -100)

    return {
        'input_ids': padded_input_ids,
        'attention_mask': padded_attention_masks,
        'labels': padded_labels
    }


def data_collator_ex(features) -> dict:
    return {"input_ids": torch.stack([torch.LongTensor(f['input_ids']) for f in features])}


def data_collator(features: list) -> dict:
    return {"input_ids": torch.stack([torch.LongTensor(f) for f in features])}


@dataclass
class ModelArguments:
    model_name_or_path: Optional[str] = field(default="bigscience/bloom-560m")
    training_type: Optional[str] = field(
        default="causal_lm",
        # choices=["causal_lm", "instruction_lm",'conversation_lm']
    )


@dataclass
class DataArguments:
    data_name_or_path: str = field(
        default="tatsu-lab/alpaca", metadata={"help": "Path to the training data."}
    )
    data_cache_dir: str = field(
        default="/import/snvm-sc-podscratch3/chenw/datasets",
        metadata={"help": "Path to the training data."}
    )


def conv_gen(data_files):
    if not isinstance(data_files, List):
        data_files = [data_files]
    for file in data_files:
        with open(file, 'r') as f:
            for line in f:
                yield {'conv': json.loads(line)}

# from datasets import Dataset
# data_files = ["/import/snvm-sc-scratch2/fengluh/web_master/training_data/train/booking_and_home_search.jsonl"]
# ds = Dataset.from_generator(conv_gen, gen_kwargs={"data_files": data_files})
# print(ds[0])
