# 1. استخدام نسخة Python خفيفة ومناسبة
FROM python:3.9-slim

# 2. تثبيت مكتبات النظام الضرورية لـ OpenCV و YOLO
# تثبيت مكتبات النظام الضرورية لـ OpenCV و YOLO
RUN apt-get update && apt-get install -y \
    libgl1 \
    libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

# 3. تحديد مكان العمل داخل الحاوية
WORKDIR /app

# 4. نسخ ملف الـ requirements وتثبيت المكتبات
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# 5. نسخ كل ملفات المشروع (main.py, api.py, best.pt, إلخ)
COPY . .

# 6. السماح بالوصول للمجلد (ضروري لـ Hugging Face Spaces)
RUN chmod -R 777 /app

# 7. تشغيل الـ API باستخدام uvicorn
# Hugging Face بيستخدم بورت 7860 افتراضياً
CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "7860"]