import torch
import requests
from PIL import Image
from torchvision import models, transforms
from io import BytesIO
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import DataLoader, Dataset
import pandas as pd
from sklearn.model_selection import train_test_split
import tqdm
import hashlib
import os

# Load model directly
import torch
from PIL import Image
from transformers import AutoModelForImageClassification, ViTImageProcessor

class ImageClassifier(nn.Module):
    def __init__(self, model_name='resnet50', num_classes=2):
        super(ImageClassifier, self).__init__()
        assert torch.cuda.is_available(), "CUDA is not available. Training is not supported."
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        self.backbone = self.load_backbone(model_name)
        self.backbone = self.backbone.to(self.device)
        self.freeze_backbone()
        self.classifier = self.add_classifier(num_classes)
        self.classifier = self.classifier.to(self.device)
        self.preprocess = self.define_transforms()

    def load_backbone(self, model_name):
        model = getattr(models, model_name)(pretrained=True)
        backbone = nn.Sequential(*list(model.children())[:-1])  # Remove the final fully connected layer
        return backbone

    def freeze_backbone(self):
        for param in self.backbone.parameters():
            param.requires_grad = False

    def add_classifier(self, num_classes):
        return nn.Sequential(
            nn.Flatten(),
            nn.Linear(2048, 512),  # 2048 for resnet50, adjust if using a different model
            nn.ReLU(),
            nn.Dropout(0.5),
            nn.Linear(512, num_classes)
        )

    def define_transforms(self):
        return transforms.Compose([
            transforms.Resize(256),
            transforms.CenterCrop(224),
            transforms.ToTensor(),
            transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
        ])

    def load_image_from_url(self, url):
        response = requests.get(url)
        image = Image.open(BytesIO(response.content)).convert('RGB')
        return self.preprocess(image).unsqueeze(0).to(self.device)

    def forward(self, x):
        x = self.backbone(x)
        x = self.classifier(x)
        return x

    def train_model(self, train_loader, num_epochs=5, learning_rate=0.001, class_weights=None):
        self.train()
        criterion = nn.CrossEntropyLoss(weight=class_weights)
        optimizer = optim.Adam(self.classifier.parameters(), lr=learning_rate)

        for epoch in range(num_epochs):
            print(f"Epoch [{epoch + 1}/{num_epochs}]")
            running_loss = 0.0
            for inputs, labels in tqdm.tqdm(train_loader, total=len(train_loader)):
                inputs, labels = inputs.to(self.device), labels.to(self.device)
                optimizer.zero_grad()
                outputs = self(inputs)
                loss = criterion(outputs, labels)
                loss.backward()
                optimizer.step()

                running_loss += loss.item()
            
            print(f"Epoch [{epoch + 1}/{num_epochs}], Loss: {running_loss / len(train_loader):.4f}")
        
        self.eval()

class CustomImageDataset(Dataset):
    def __init__(self, dataframe, transform=None):
        self.dataframe = dataframe
        self.transform = transform

    def get_md5_of_url(self, url):
        md5_hash = hashlib.md5(url.encode()).hexdigest()
        return md5_hash
    
    def __len__(self):
        return len(self.dataframe)

    def __getitem__(self, idx):
        img_url = self.dataframe.iloc[idx]['ImageUrl']
        label = self.dataframe.iloc[idx]['GroundTruthLabel']

        md5_hash_file_name = self.get_md5_of_url(img_url)
        image_path = os.path.join("../images", f"{md5_hash_file_name}.png")

        try:
            if os.path.exists(image_path):
                image = Image.open(image_path).convert('RGB')
            else:
                response = requests.get(img_url)
                if response.status_code != 200:
                    raise ValueError(f"Image URL {img_url} is invalid. Skipping...")
                image = Image.open(BytesIO(response.content)).convert('RGB')
                image.save(image_path, "PNG")

            if self.transform:
                image = self.transform(image)
            return image, label

        except Exception as e:
            print(e)
            return None  # Return None if there's an issue with loading or processing the image

def custom_collate_fn(batch):
    # Filter out None values
    batch = [item for item in batch if item is not None]
    if len(batch) == 0:
        return None  # Return None if all items are invalid
    return torch.utils.data.dataloader.default_collate(batch)

def load_image_data(filepath):
    df = pd.read_csv(filepath, sep='\t')
    print(df.columns)
    good_images = df[df['GroundTruthLabel'] == 1]
    good_percentage = len(good_images) / len(df) * 100
    print(f"Percentage of labeled images: {good_percentage:.2f}%")
    return df

def train(num_epochs=5):
    df = load_image_data('../uhrs_image_evaluation_ds3.tsv')
    train_df, test_df = train_test_split(df, test_size=0.2, random_state=42)

    print(f"Training data: {len(train_df)} images")
    print(f"Testing data: {len(test_df)} images")

    class_counts = train_df['GroundTruthLabel'].value_counts().sort_index()
    print(f"Class counts: {class_counts}")
    class_weights = torch.tensor([1.0 / class_counts[0], 1.0 / class_counts[1]], dtype=torch.float32).to("cuda")
    print(f"Class weights: {class_weights}")
    
    
    classifier = ImageClassifier(num_classes=2)
    train_dataset = CustomImageDataset(train_df, transform=classifier.preprocess)
    train_loader = DataLoader(train_dataset, batch_size=32, shuffle=True, collate_fn=custom_collate_fn)
    classifier.train_model(train_loader, num_epochs=num_epochs, class_weights=class_weights)
    
    # Save the model
    torch.save(classifier.state_dict(), "image_classifier.pth")


def inference(image_url: str):
    # Load the model
    classifier = ImageClassifier(num_classes=2)
    classifier.load_state_dict(torch.load("image_classifier.pth"))
    classifier.eval()
    
    # Inference on a single image
    image = classifier.load_image_from_url(image_url)
    output = classifier(image)
    output = torch.softmax(output, dim=1)
    print(output)


def inference_hf(data_lines):
    model = AutoModelForImageClassification.from_pretrained("/home/wuc/nsfw_image_detection")
    processor = ViTImageProcessor.from_pretrained('/home/wuc/nsfw_image_detection')    
    model.eval()
    model.cuda()
    
    resutls = []
    for data in tqdm.tqdm(data_lines, total=len(data_lines)):
        image_url = data['imageUrl']
        # print(f"Processing image: {image_url}")
        with torch.no_grad():
            response = requests.get(image_url)
            if response.status_code != 200:
                print(f"Failed to access the image: {data_dict['imageUrl']}")
                continue
            img = Image.open(BytesIO(response.content)).convert('RGB')
            inputs = processor(images=img, return_tensors="pt")
            inputs.to("cuda")
            outputs = model(**inputs)
            logits = outputs.logits
        
        predicted_label = model.config.id2label[logits.argmax(-1).item()]
        ground_truth_label = data['GroundTruthLabel']
        gpt4o_label = data['GPT4oLabel']
        
        resutls.append({
            'imageUrl': image_url,
            'predicted_label': predicted_label,
            'ground_truth_label': ground_truth_label,
            'gpt4o_label': gpt4o_label
        })
    
    with open("hf_results.csv", "w") as f:
        f.write("imageUrl,predicted_label,ground_truth_label,gpt4o_label\n")
        for result in resutls:
            f.write(f"{result['imageUrl']},{result['predicted_label']},{result['ground_truth_label']},{result['gpt4o_label']}\n")


if __name__ == "__main__":
    # train(num_epochs=5)    
    image_url = "https://th.bing.com/th?id=OIP-C.-stzh95TNmX5w_tuipB6-wHaKb&pid=Wdp"
    # image_url = "https://img-s-msn-com.akamaized.net/tenant/amp/entityid/BB1pDFyu.img"
    # image_url = "https://th.bing.com/th?id=ORMS.2f149bb16434bf70d3a5c85b5329d893&pid=Wdp"
    # image_url = "https://th.bing.com/th?id=ORMS.301339b2b3fda5bcf61795b66ea4cf14&pid=Wdp"
    # image_url = "https://th.bing.com/th/id/OIP.FNbX0bXh8l3JYww-QthwIwHaLz?w=136&h=218&c=7&r=0&o=5&pid=1.7"
    # image_url = "https://th.bing.com/th/id/OIP.tyvhBPVwoeWRuq_yhuy3mwHaHo?w=200&h=206&c=7&r=0&o=5&pid=1.7"
    # inference(image_url)
    file_path = "ds3.csv"
    df_ds3 = pd.read_csv(file_path)
    print(df_ds3.columns)
    
    num_rows = df_ds3.shape[0]
    print("Number of rows:", num_rows)
    data_lines = []
    for i in tqdm.trange(num_rows):
        data_dict = {}
        data_dict['imageUrl'] = df_ds3.iloc[i]['BingUrl']
        data_dict['GPT4oLabel'] = df_ds3.iloc[i]['GPT4oLabel']
        data_dict['GroundTruthLabel'] = df_ds3.iloc[i]['GroundTruthLabel']
        data_lines.append(data_dict)
    
    from PIL import Image 
    import requests 
    from transformers import AutoModelForCausalLM 
    from transformers import AutoProcessor 

    model_id = "microsoft/Phi-3.5-vision-instruct" 

    # Note: set _attn_implementation='eager' if you don't have flash_attn installed
    model = AutoModelForCausalLM.from_pretrained(
        model_id, 
        device_map="cuda", 
        trust_remote_code=True, 
        torch_dtype=torch.float16, 
        _attn_implementation='eager'    
    )

    # for best performance, use num_crops=4 for multi-frame, num_crops=16 for single-frame.
    processor = AutoProcessor.from_pretrained(model_id, 
        trust_remote_code=True, 
        num_crops=2
    ) 
    
    # Read prompt content from txt file.
    with open("prompt.txt", "r") as f:
        prompt_conetent = f.read().strip()
    
    responses = []
    
    for data in tqdm.tqdm(data_lines, total=len(data_lines)):
        image_url = data['imageUrl']
        response_dict = {}
        images = []
        
        response = requests.get(image_url)
        if response.status_code != 200:
            print(f"Failed to access the image: {data_dict['imageUrl']}")
            continue
        img = Image.open(BytesIO(response.content)).convert('RGB')
        images.append(img)        
        # images.append(Image.open(requests.get(image_url, stream=True).raw))
        messages = [
            {"role": "user", "content": "<|image_1|>\n"+prompt_conetent},
        ]

        prompt = processor.tokenizer.apply_chat_template(
                messages, 
                tokenize=False, 
                add_generation_prompt=True,
        )

        inputs = processor(prompt, images, return_tensors="pt").to("cuda:0") 

        generation_args = { 
            "max_new_tokens": 1000, 
            "temperature": 0.0, 
            "do_sample": False, 
        } 

        generate_ids = model.generate(**inputs, 
            eos_token_id=processor.tokenizer.eos_token_id, 
            **generation_args
            )

        # remove input tokens 
        generate_ids = generate_ids[:, inputs['input_ids'].shape[1]:]
        response = processor.batch_decode(generate_ids, 
        skip_special_tokens=True, 
        clean_up_tokenization_spaces=False)[0] 
        
        response_dict['imageUrl'] = image_url
        response_dict['phi35Label'] = response
        
        responses.append(response_dict)
    
    with open("phi3_5_results.csv", "w") as f:
        f.write("imageUrl,phi35Label\n")
        for response in responses:
            f.write(f"{response['imageUrl']},{response['phi35Label']}\n")    
