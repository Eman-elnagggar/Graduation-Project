from fastapi import FastAPI, File, UploadFile, Form
from enum import Enum
from pydantic import BaseModel
import main
import consistancy
import shutil
import os

app = FastAPI()
class TestType(str, Enum):
    CBC='CBC (Complete Blood Count)'
    Urine='Urinalysis'
    HbA1c='HbA1c (Sugar Test)'
    TSH='TSH (Thyroid)'
    Ferritin='Ferritin'
    Blood_Group='Blood Group'
    HBsAg='HBsAg (Hepatitis B)'
    HCV='HCV (Hepatitis C)'

@app.post("/analyze")
async def get_analysis(
    test_type: TestType = Form(...), 
    image: UploadFile = File(...)
):
    temp_path = f"temp_{image.filename}"
    with open(temp_path, "wb") as buffer:
        shutil.copyfileobj(image.file, buffer)
    
    try:
        print(f"Processing file: {temp_path} with test: {test_type.value}")
        
        result = main.run_ocr(temp_path, test_type.value)
    except Exception as e:
        print(f"Error happened: {e}") 
        return {"error": str(e)}
    finally:
        if os.path.exists(temp_path):
            os.remove(temp_path)
            
    return result

class ConfirmRequest(BaseModel):
    test_type: str
    edited_values: dict

@app.post("/confirm")
async def confirm_results(data: ConfirmRequest):
    try:
        final_data = consistancy.consistancy_data(data.test_type, data.edited_values)
        return final_data 
    except Exception as e:
        return {"error": str(e)}