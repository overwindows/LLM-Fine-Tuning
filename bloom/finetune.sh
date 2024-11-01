deepspeed --num_gpus=2 finetune.py \
    --model_name_or_path /import/snvm-sc-podscratch3/chenw/bloom-7b1  \
    --data_name_or_path codeparrot/codeparrot-clean-train \
    --per_device_train_batch_size 8 --max_steps 500000 \
    --num_train_epochs 8 \
    --logging_steps 10 --fp16 \
    --deepspeed z3_ds_config.json \
    --output_dir output --overwrite_output_dir

# deepspeed --num_gpus=4 finetune-alpaca.py \
#     --model_name_or_path bigscience/bloom-1b7 \
#     --per_device_train_batch_size 32 \
#     --num_train_epochs 2 \
#     --logging_steps 10 \
#     --deepspeed z2_ds_config.json \
#     --output_dir output --overwrite_output_dir

# torchrun --nproc_per_node=2 finetune-alpaca.py \
#     --model_name_or_path bigscience/bloom-7b1 \
#     --per_device_train_batch_size 2 \
#     --gradient_accumulation_steps 1 \
#     --num_train_epochs 2 \
#     --learning_rate 2e-5 \
#     --fp16 True \
#     --logging_steps 10 \
#     --output_dir output

# deepspeed --num_gpus=2 finetune.py \
#     --model_name_or_path /import/snvm-sc-podscratch3/chenw/bloom-7b1 \
#     --data_name_or_path codeparrot/codeparrot-clean-train \
#     --per_device_train_batch_size 8 --gradient_accumulation_steps 4 --max_steps 500000 \
#     --learning_rate 1e-05 --weight_decay 0.01 --warmup_steps 300 \
#     --lr_scheduler_type='cosine' \
#     --gradient_checkpointing \
#     --num_train_epochs 3 \
#     --logging_steps 10 --fp16 \
#     --output_dir output \
#     --overwrite_output_dir \
#     --deepspeed z3_ds_config.json
