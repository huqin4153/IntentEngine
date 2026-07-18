# -*- coding: utf-8 -*-
import os
os.environ["HF_HUB_ENABLE_HF_TRANSFER"] = "0"

import warnings
warnings.filterwarnings("ignore")

import torch
from transformers import AutoModel, AutoTokenizer
import onnx

MODEL_NAME = "BAAI/bge-small-zh-v1.5"
OUT_DIR = r"e:\VSCode\Monitor\IntentEngine\Resources"
ONNX_PATH = os.path.join(OUT_DIR, "bge-small-zh-v1.5.onnx")

# Clean old files
for f in [ONNX_PATH, ONNX_PATH + ".data"]:
    if os.path.exists(f): os.remove(f)

print("Loading model...")
tokenizer = AutoTokenizer.from_pretrained(MODEL_NAME)
model = AutoModel.from_pretrained(MODEL_NAME)
model.eval()

dummy = tokenizer(["测试"], padding=True, truncation=True, max_length=128, return_tensors="pt")

# Use the ONNX exporter with dynamic_axes=False to force simpler graph
# Export with opset 16 which is the latest ORT 1.14 supports
torch.onnx.export(
    model.eval(),
    (dummy["input_ids"], dummy["attention_mask"], dummy["token_type_ids"]),
    ONNX_PATH,
    input_names=["input_ids", "attention_mask", "token_type_ids"],
    output_names=["last_hidden_state"],
    dynamic_axes={
        "input_ids": {0: "batch_size", 1: "seq_length"},
        "attention_mask": {0: "batch_size", 1: "seq_length"},
        "token_type_ids": {0: "batch_size", 1: "seq_length"},
    },
    opset_version=16,
)

# Merge external data
ext = ONNX_PATH + ".data"
if os.path.exists(ext):
    print("External data exists, merging...")
    model_proto = onnx.load(ONNX_PATH, load_external_data=True)
    onnx.save_model(model_proto, ONNX_PATH, save_as_external_data=False)
    os.remove(ext)

# Verify with ORT
import onnxruntime
sess = onnxruntime.InferenceSession(ONNX_PATH)
print(f"IR version: {sess.get_modelmeta().version}")
print(f"Model size: {os.path.getsize(ONNX_PATH)/1024/1024:.1f}MB")
print("SUCCESS")
