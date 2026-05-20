from fastapi import FastAPI, File, UploadFile, Form
# from enum import Enum
# from pydantic import BaseModel
import main
# import shutil
# import os
import base64
import cv2

app = FastAPI()
@app.post("/fetal")
async def fetal_abnormalities(image: UploadFile = File(...)):

    image_bytes = await image.read()
    result_data = main.call_yolo_model(image_bytes)
    # if result_data['image']!=None:
    _, buffer = cv2.imencode('.jpg', result_data['image'])
    img_as_text = base64.b64encode(buffer).decode('utf-8')
    return {
    "error":result_data['error'],
    "image": img_as_text,
    "risk": result_data['risk'],
    "thickness_mm": result_data['thickness_mm']
    }
    # return result_data

