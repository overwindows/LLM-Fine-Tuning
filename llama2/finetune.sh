SKIP_STEPS=0
WARMUP_STEPS=0
LOG_STEPS=1
SAVE_INTERVAL=1
LEARNING_RATE=1e-5

NUM_TRAIN_EPOCHS=8

DATA_PATH=/import/snvm-sc-scratch1/chenw/data/processed_data/article_data.jsonl
MODEL_PATH=/import/ml-sc-nlpcheckpoints-scratch/jonathanl/generic_checkpoints/llama_2/Llama-2-7b-hf

OUTPUT_DIR=/import/snvm-sc-podscratch3/chenw/model/llama2_7b_ft_gpu

deepspeed --num_gpus=4 --master_port 9901 finetune.py \
    --model_name_or_path $MODEL_PATH \
    --data_name_or_path $DATA_PATH \
    --per_device_train_batch_size 1 \
    --num_train_epochs $NUM_TRAIN_EPOCHS --max_steps 1000 \
    --logging_steps 10 --fp16 \
    --save_steps 16 \
    --output_dir $OUTPUT_DIR --overwrite_output_dir \
    --deepspeed z3_ds_config.json \
    