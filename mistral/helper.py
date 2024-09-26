from dataclasses import dataclass, field
from typing import List, Optional
import torch
import json
from transformers import Trainer, AdamW


class CustomTrainer(Trainer):
    def compute_loss(self, model, inputs, return_outputs=False):
        # print(inputs["input_ids"].shape,
        #       inputs["attention_mask"].shape, inputs["labels"].shape)
        loss = model(
            input_ids=inputs["input_ids"],
            attention_mask=inputs["attention_mask"],
            labels=inputs["labels"],
            use_cache=False
        ).loss
        # print(loss, loss.shape, inputs["input_ids"].shape)
        return loss

    def create_optimizer(self):
        if self.optimizer is not None:
            print("Optimizer already created")
            # print the parameters and learning rates in the optimizer
            # for param_group in self.optimizer.param_groups:
            #     print(param_group['lr'])
            # print(self.optimizer)
            return self.optimizer
        print("Creating optimizer")
        model = self.model
        base_lr = self.args.learning_rate
        print(f"Base learning rate: {base_lr}")
        decay_rate = 0.9

        optimizer_grouped_parameters = []

        layers = model.model.layers
        num_layers = len(layers)
        print(f"Number of layers: {num_layers}")
        # Assign learning rates
        for i, layer in enumerate(layers):
            lr = base_lr * (decay_rate ** (i+1))
            # print(layer)
            optimizer_grouped_parameters.append({
                'params': layer.parameters(),
                'lr': lr,
            })

        # Embedding layer
        embeddings_lr = base_lr
        optimizer_grouped_parameters.append({
            'params': model.model.embed_tokens.parameters(),
            'lr': embeddings_lr,
        })

        # Other parameters (e.g., LayerNorm, output heads)
        other_params = []
        for name, param in model.named_parameters():
            if 'model.layers' not in name and 'model.embed_tokens' not in name:
                print(name)
                other_params.append(param)

        optimizer_grouped_parameters.append({
            'params': other_params,
            'lr': base_lr,
        })

        # Create optimizer
        self.optimizer = AdamW(
            optimizer_grouped_parameters,
            betas=(self.args.adam_beta1, self.args.adam_beta2),
            eps=self.args.adam_epsilon
        )

        return self.optimizer


class ModifiedTrainer(Trainer):
    def create_optimizer(self):
        # assert self.optimizer is None, "An optimizer was already created"
        if self.optimizer is not None:
            print("Optimizer already created")
            # print the parameters and learning rates in the optimizer
            # for param_group in self.optimizer.param_groups:
            #     print(param_group['lr'])
            # print(self.optimizer)
            return self.optimizer
        print("Creating optimizer")
        model = self.model
        base_lr = self.args.learning_rate
        decay_rate = 0.85

        optimizer_grouped_parameters = []

        layers = model.model.layers
        num_layers = len(layers)
        print(f"Number of layers: {num_layers}")
        # Assign learning rates
        for i, layer in enumerate(layers):
            lr = base_lr * (decay_rate ** (i + 1))
            # print(layer)
            optimizer_grouped_parameters.append({
                'params': layer.parameters(),
                'lr': lr,
            })

        # Embedding layer
        embeddings_lr = base_lr
        optimizer_grouped_parameters.append({
            'params': model.model.embed_tokens.parameters(),
            'lr': embeddings_lr,
        })

        # Other parameters (e.g., LayerNorm, output heads)
        other_params = []
        for name, param in model.named_parameters():
            if 'model.layers' not in name and 'model.embed_tokens' not in name:
                print(name)
                other_params.append(param)

        optimizer_grouped_parameters.append({
            'params': other_params,
            'lr': base_lr,
        })

        # Create optimizer
        self.optimizer = AdamW(
            optimizer_grouped_parameters,
            betas=(self.args.adam_beta1, self.args.adam_beta2),
            eps=self.args.adam_epsilon
        )

        # for param_group in self.optimizer.param_groups:
        #     print(param_group['lr'])
        # print(self.optimizer)
        return self.optimizer

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


def preprocess_func4it(example, tokenizer, max_length):
    """
    Preprocess a single example for model training.

    Args:
        example (dict): A dictionary containing 'prompt' and 'completion'.
        tokenizer: The tokenizer to encode the text.
        max_length (int): The maximum length for tokenization.

    Returns:
        dict: A dictionary with 'input_ids', 'attention_mask', and 'labels'.
    """

    # Ensure the prompt is empty before setting it
    if example['prompt'] != "":
        raise ValueError(f"Prompt is not empty: {example['prompt']}")

    # Set the default prompt
    example['prompt'] = "[INST] Please continue the following text. [/INST]"

    # Encode the prompt to get its length
    prompt = example['prompt']
    completion = example['completion']
    
    # Encode the prompt and completion
    encoded_prompt = tokenizer(prompt, return_tensors='pt', add_special_tokens=False, truncation=True)
    encoded_completion = tokenizer(completion, return_tensors='pt', padding='max_length', truncation=True, add_special_tokens=False, max_length=max_length)

    # Combine the encoded prompt and completion
    input_ids = torch.cat((encoded_prompt['input_ids'], encoded_completion['input_ids']), dim=1)

    # Create labels only for the completion
    labels = torch.full_like(input_ids, -100)  # Fill with -100
    labels[:, encoded_prompt['input_ids'].shape[1]:] = encoded_completion['input_ids']  # Only completion tokens are labeled

    # Create attention mask
    attention_mask = torch.cat((encoded_prompt['attention_mask'], encoded_completion['attention_mask']), dim=1)
    # print(input_ids.shape, attention_mask.shape, labels.shape)
    return {
        'input_ids': input_ids.squeeze(),
        'attention_mask': attention_mask.squeeze(),
        'labels': labels.squeeze()
    }


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

        # print(prompt_length, encoding.input_ids.shape, encoding.attention_mask.shape, labels.shape)

    assert len(input_idss) == len(attention_masks) == len(labelss)

    concat_attention_masks = torch.cat(attention_masks, dim=-1)
    concat_input_ids = torch.cat(input_idss, dim=-1)
    concat_labels = torch.cat(labelss, dim=-1)

    # print(concat_input_ids.shape, concat_attention_masks.shape, concat_labels.shape)

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
