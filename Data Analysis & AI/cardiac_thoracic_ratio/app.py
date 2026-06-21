from fastapi import FastAPI, File, UploadFile
import main
import base64
import cv2
import numpy as np

app = FastAPI()

@app.post("/fetal")
async def fetal_abnormalities(image: UploadFile = File(...)):
    image_bytes = await image.read()
    result_data = main.call_yolo_model(image_bytes)
    if not result_data:
        return {
            "error": "Critical: Model pipeline returned no data",
            "image": None,
            "risk": "Unknown",
            "ctr_percentage": 0.0
        }
    
    img_as_text = None
    model_image = result_data.get('image')
    if model_image is not None and isinstance(model_image, np.ndarray):
        _, buffer = cv2.imencode('.jpg', model_image)
        img_as_text = base64.b64encode(buffer).decode('utf-8')
        
    return {
        "error": result_data.get('error'),
        "image": img_as_text,
        "risk": result_data.get('risk', "Unknown"),
        "ctr_percentage": result_data.get('ctr_percentage', 0.0)
    }