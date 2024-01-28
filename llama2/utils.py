from dataclasses import dataclass, field
from typing import Optional

import torch
import tqdm
from transformers import Trainer


class ModifiedTrainer(Trainer):
    def compute_loss(self, model, inputs, return_outputs=False):
        # print(inputs['input_ids'].size())
        return model(
            input_ids=inputs["input_ids"],
            attention_mask=torch.ones_like(inputs["input_ids"]).bool(),
            labels=inputs["input_ids"],
            use_cache=False
        ).loss


def data_collator_ex(features) -> dict:
    return {"input_ids": torch.stack([torch.LongTensor(f['input_ids']) for f in features])}
    
def data_collator(features: list) -> dict:
    return {"input_ids": torch.stack([torch.LongTensor(f) for f in features])}


def tokenize_data(dataset, tokenizer, max_seq_length=512):
    tokenised_list = []
    ix = 0
    for elem in tqdm.tqdm(dataset):
        tokenised_list.append(
            tokenizer.encode(
                # elem["text"],
                elem["content"],
                # elem["whole_func_string"],
                max_length=max_seq_length,
                padding="max_length",
                truncation=True,
            )
        )
        ix += 1
        if ix > 500000:
            break
    return tokenised_list


@dataclass
class ModelArguments:
    model_name_or_path: Optional[str] = field(default="bigscience/bloom-560m")


@dataclass
class DataArguments:
    data_name_or_path: str = field(
        default="tatsu-lab/alpaca", metadata={"help": "Path to the training data."}
    )
