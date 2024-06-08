cd /import/snvm-sc-scratch1/chenw/LLM-Fine-Tuning/mistral
source /import/snvm-sc-scratch1/chenw/gpu_env/bin/activate

SKIP_STEPS=0
WARMUP_STEPS=0
LOG_STEPS=10
SAVE_INTERVAL=1
LEARNING_RATE=1e-5
NUM_TRAIN_EPOCHS=8
GRAD_ACC_STEPS=4
PER_DEV_BZ=2
SAVE_STEPS=16
LR_SCHE_TYPE=constant

DEEPSPEED_CONF=../ds_configs/z3_ds_config.json
DEEPSPEED_PORT=9902
DEEPSPEED_GPUS=2

DATASET_CACHE=/import/snvm-sc-podscratch3/chenw/dataset_cache
TRAINING_TYPE=causal_lm

# DATA_PATH=/import/snvm-sc-scratch1/chenw/data/processed_data/article_data.jsonl
DATA_PATH=/import/snvm-sc-scratch1/chenw/data/post_processed_data_wiki/splits/train_1_of_10.jsonl
CACHE_DIR=/import/snvm-sc-podscratch3/chenw/dataset_cache

MODEL_PATH=/import/snvm-sc-scratch2/reidg/models--mistralai--Mistral-7B-Instruct-v0.1/snapshots/9ab9e76e2b09f9f29ea2d56aa5bd139e4445c59e/
OUTPUT_DIR=/import/snvm-sc-podscratch3/chenw/model/mistral_7b_ft_gpu_percent_10

# deepspeed --num_gpus=$DEEPSPEED_GPUS --master_port 9901 finetune.py \
#     --model_name_or_path $MODEL_PATH \
#     --data_name_or_path $DATA_PATH \
#     --per_device_train_batch_size 1 \
#     --num_train_epochs $NUM_TRAIN_EPOCHS --max_steps 1000 \
#     --logging_steps 10 --fp16 \
#     --save_steps 16 \
#     --output_dir $OUTPUT_DIR --overwrite_output_dir \
#     --deepspeed z3_ds_config.json \

NCCL_SHM_DISABLE=1 deepspeed --num_gpus=$DEEPSPEED_GPUS --master_port $DEEPSPEED_PORT trainer.py \
    --model_name_or_path $MODEL_PATH \
    --data_name_or_path $DATA_PATH \
    --data_cache_dir $CACHE_DIR \
    --per_device_train_batch_size $PER_DEV_BZ \
    --learning_rate $LEARNING_RATE \
    --gradient_accumulation_steps $GRAD_ACC_STEPS \
    --num_train_epochs $NUM_TRAIN_EPOCHS --max_steps 1000 \
    --logging_steps $LOG_STEPS --fp16 \
    --save_steps $SAVE_STEPS --lr_scheduler_type $LR_SCHE_TYPE \
    --output_dir $OUTPUT_DIR --overwrite_output_dir \
    --training_type $TRAINING_TYPE \
    --report_to wandb \
    --deepspeed $DEEPSPEED_CONF \
    