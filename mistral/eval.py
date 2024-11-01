from transformers import AutoModelForCausalLM, AutoTokenizer


def inference(mistral_models_path: str):
    tokenizer = AutoTokenizer.from_pretrained(mistral_models_path)
    model = AutoModelForCausalLM.from_pretrained(mistral_models_path)
   
    conversation = [{"role": "user", "content": "Tell me a joke"}]

    # format and tokenize the tool use prompt 
    inputs = tokenizer.apply_chat_template(
                conversation,
                add_generation_prompt=True,
                return_dict=True,
                return_tensors="pt",
    )

    outputs = model.generate(**inputs, max_new_tokens=64)
    print(tokenizer.decode(outputs[0], skip_special_tokens=True))

    # out_tokens, _ = generate([tokens], model, max_tokens=64, temperature=0.0, eos_id=tokenizer.instruct_tokenizer.tokenizer.eos_id)
    # result = tokenizer.instruct_tokenizer.tokenizer.decode(out_tokens[0])

    # print(result)

if __name__ == '__main__':
    model_path = 'mistralai/Mistral-7B-Instruct-v0.3'
    inference(model_path)