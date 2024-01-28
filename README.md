#
ssh sc-vnc1
sngpu --interactive --time 5:59:59 --cpu 8 --mem 65536 --gpu 1
source ~/venv_yoda/bin/activate




# LLM-fine-tuning

This project is for fine-tuning BLOOM. The repo contains:
- We use [Stanford Alpaca](https://github.com/tatsu-lab/stanford_alpaca).

Try it on Google Colab! <a href="https://colab.research.google.com/github/hyintell/BLOOM-fine-tuning/blob/main/finetune.ipynb"> 
        <img alt="Build" src="https://colab.research.google.com/assets/colab-badge.svg">
    </a>

## Installation

```bash
pip install -r requirements.txt
```

Data: [alpaca](https://huggingface.co/datasets/tatsu-lab/alpaca)

### Tokenize programming language with tree-sitter
To get parser/my-languages.so, you need to build the Tree-sitter parser for the desired language (C# in this case) and create a directory named parser to store the compiled language library. 

Clone the Tree-sitter repository for the desired language. For C#, you can clone the tree-sitter-c-sharp repository.
```
git clone https://github.com/tree-sitter/tree-sitter-c-sharp.git 
git clone https://github.com/tree-sitter/tree-sitter-rust.git
git clone https://github.com/tree-sitter/tree-sitter-json.git
git clone https://github.com/tree-sitter/tree-sitter-typescript.git
git clone https://github.com/tree-sitter/tree-sitter-julia.git
git clone https://github.com/tree-sitter/tree-sitter-bash.git
git clone https://github.com/tree-sitter/tree-sitter-haskell.git
git clone https://github.com/tree-sitter/tree-sitter-html.git
git clone https://github.com/tree-sitter/tree-sitter-c.git
git clone https://github.com/tree-sitter/tree-sitter-scala.git
git clone https://github.com/tree-sitter/tree-sitter-php.git
git clone https://github.com/tree-sitter/tree-sitter-ruby.git
git clone https://github.com/tree-sitter/tree-sitter-go.git
git clone https://github.com/tree-sitter/tree-sitter-swift.git
```

## Training

### alpaca

```bash
python finetune-alpaca.py \
    --model_name_or_path bigscience/bloom-7b1 \
    --per_device_train_batch_size 2 \
    --gradient_accumulation_steps 1 \
    --num_train_epochs 2 \
    --learning_rate 2e-5 \
    --fp16 True \
    --logging_steps 10 \
    --output_dir output
```

## Progress
- [x] Add Code Corpus
- [x] Test bloom-560m
- [x] Test bloom-1b7
- [X] Test bloom-7b1
- [ ] Support Evaluation
