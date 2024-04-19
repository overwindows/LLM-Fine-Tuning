MODEL_PATH=/home/wuc/Mistral-7B-Instruct-v0.2

python3 -m manifest.api.app \
    --model_type huggingface \
    --model_name_or_path ${MODEL_PATH} \
    --model_generation_type text-generation \
    --fp16 \
    --device 0
