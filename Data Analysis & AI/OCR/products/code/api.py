import os
import uuid
import shutil
from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.responses import JSONResponse

from main import run_ocr

app = FastAPI(title="Product Ingredients Analyzer API")

UPLOAD_FOLDER = "temp_uploads"
os.makedirs(UPLOAD_FOLDER, exist_ok=True)

@app.post("/analyze-image")
async def analyze_image(image: UploadFile = File(...)):
    try:
        if not image.content_type.startswith("image/"):
            raise HTTPException(status_code=400, detail="File must be an image")

        file_ext = image.filename.split(".")[-1]
        unique_name = f"{uuid.uuid4()}.{file_ext}"
        file_path = os.path.join(UPLOAD_FOLDER, unique_name)

        with open(file_path, "wb") as buffer:
            shutil.copyfileobj(image.file, buffer)

        status, explained_text = run_ocr("image", file_path)

        os.remove(file_path)

        return JSONResponse(
            content={
                "status": status,
                "explained_text": explained_text
            }
        )

    except Exception as e:
        return JSONResponse(
            status_code=500,
            content={
                "success": False,
                "error": str(e)
            }
        )


@app.post("/analyze-text")
async def analyze_text(text: str = Form(...)):
    try:
        if not text.strip():
            raise HTTPException(status_code=400, detail="Text cannot be empty")

        status, explained_text = run_ocr("text", text)

        return JSONResponse(
            content={
                "status": status,
                "explained_text": explained_text
            }
        )

    except Exception as e:
        return JSONResponse(
            status_code=500,
            content={
                "success": False,
                "error": str(e)
            }
        )