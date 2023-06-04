CKPT_PATH=path_to_specific_checkpoint


deepspeed --num_gpus=8 finetune.py \
    --model_name_or_path bigscience/bloom-7b1  \
    --per_device_train_batch_size 2 \
    --num_train_epochs 8 \
    --logging_steps 10 --fp16 \
    --deepspeed z3_ds_config.json \
    --resume_from_checkpoint $CKPT_PATH \
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

# python finetune-alpaca.py \
#     --model_name_or_path bigscience/bloom-7b1 \
#     --per_device_train_batch_size 2 \
#     --gradient_accumulation_steps 1 \
#     --num_train_epochs 2 \
#     --learning_rate 2e-5 \
#     --fp16 True \
#     --logging_steps 10 \
#     --output_dir output --overwrite_output_dir