import json
import argparse
import torch
from transformers import AutoModelForCausalLM, AutoTokenizer


def generate(mistral_models_path: str, messages: list):
    tokenizer = AutoTokenizer.from_pretrained(mistral_models_path)
    # Load the model on the appropriate device (GPU or CPU)
    model = AutoModelForCausalLM.from_pretrained(
        mistral_models_path, device_map="cpu")
    model.eval()
    inputs = tokenizer.apply_chat_template(
                messages,
                add_generation_prompt=False,
                return_dict=True,
                return_tensors="pt",
    ).to(model.device)
    with torch.no_grad():
        outputs = model.generate(**inputs, max_new_tokens=64)
    print(tokenizer.decode(outputs[0], skip_special_tokens=True))


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--model_path", type=str, default='/home/wuc/Mistral-7B-Instruct-v0_3'
    )
    parser.add_argument("--sample_file", type=str, default='raw_request.json')
    return parser.parse_args()


if __name__ == '__main__':
    args = parse_args()
    with open(args.sample_file) as f:
        request = json.load(f)
    generate(args.model_path, request['messages'])
