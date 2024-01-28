deepspeed --num_gpus=2 finetune.py \
    --model_name_or_path /import/ml-sc-nlpcheckpoints-scratch/jonathanl/generic_checkpoints/llama_2/Llama-2-7b-hf  \
    --data_name_or_path /import/snvm-sc-scratch1/chenw/data/processed_data/article_data.jsonl \
    --per_device_train_batch_size 8 --max_steps 500000 \
    --num_train_epochs 8 \
    --logging_steps 10 --fp16 \
    --deepspeed z3_ds_config.json \
    --output_dir output --overwrite_output_dir