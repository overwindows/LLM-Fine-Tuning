from dataclasses import dataclass, field
from typing import List, Optional
import torch
import tqdm
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