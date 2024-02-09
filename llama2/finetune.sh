SKIP_STEPS=0
WARMUP_STEPS=0
LOG_STEPS=10
SAVE_INTERVAL=1
LEARNING_RATE=1e-5
NUM_TRAIN_EPOCHS=8
GRAD_ACC_STEPS=2
PER_DEV_BZ=4

DEEPSPEED_CONF=../ds_configs/z3_ds_config.json
DEEPSPEED_PORT=9902
DEEPSPEED_GPUS=2

DATASET_CACHE=/import/snvm-sc-podscratch3/chenw/dataset_cache
TRAINING_TYPE=conversation_lm

# DATA_PATH=/import/snvm-sc-scratch1/chenw/data/processed_data/article_data.jsonl
DATA_PATH=/import/snvm-sc-scratch2/fengluh/web_master/training_data/train/booking_and_home_search.jsonl
CACHE_DIR=/import/snvm-sc-podscratch3/chenw/dataset_cache

MODEL_PATH=/import/ml-sc-nlpcheckpoints-scratch/jonathanl/generic_checkpoints/llama_2/Llama-2-7b-hf
OUTPUT_DIR=/import/snvm-sc-podscratch3/chenw/model/llama2_7b_ft_gpu

# deepspeed --num_gpus=$DEEPSPEED_GPUS --master_port 9901 finetune.py \
#     --model_name_or_path $MODEL_PATH \
#     --data_name_or_path $DATA_PATH \
#     --per_device_train_batch_size 1 \
#     --num_train_epochs $NUM_TRAIN_EPOCHS --max_steps 1000 \
#     --logging_steps 10 --fp16 \
#     --save_steps 16 \
#     --output_dir $OUTPUT_DIR --overwrite_output_dir \
#     --deepspeed z3_ds_config.json \

deepspeed --num_gpus=$DEEPSPEED_GPUS --master_port $DEEPSPEED_PORT finetune.py \
    --model_name_or_path $MODEL_PATH \
    --data_name_or_path $DATA_PATH \
    --data_cache_dir $CACHE_DIR \
    --per_device_train_batch_size $PER_DEV_BZ \
    --learning_rate $LEARNING_RATE \
    --gradient_accumulation_steps $GRAD_ACC_STEPS \
    --num_train_epochs $NUM_TRAIN_EPOCHS --max_steps 1000 \
    --logging_steps $LOG_STEPS --fp16 \
    --save_steps 16 --lr_scheduler_type constant \
    --output_dir $OUTPUT_DIR --overwrite_output_dir \
    --training_type $TRAINING_TYPE \
    --deepspeed $DEEPSPEED_CONF \
    