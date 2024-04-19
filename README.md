# LLM-fine-tuning

This project is for fine-tuning LLM.

## Installation

```bash
pip install -r requirements.txt
```

<!-- ### Tokenize programming language with tree-sitter
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
-->
## Local Service
```
pip install manifest-ml
pip install manifest-ml[api]

sh scripts/launch_manifest.sh
```

```
from manifest import Manifest
manifest = Manifest(client_name = "huggingface", client_connection ="http://172.22.146.79:5000")
print(manifest.client_pool.get_current_client().get_model_params())
print(manifest.client_pool.get_current_client().get_model_inputs())
print(manifest.run('tell me a joke.', max_tokens=128, client_timeout=3600))
```
