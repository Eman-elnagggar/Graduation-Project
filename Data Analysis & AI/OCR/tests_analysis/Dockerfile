FROM python:3.9-slim

# 1. تسطيب مكتبات النظام الضرورية
RUN apt-get update && apt-get install -y \
    libgl1-mesa-glx \
    libglib2.0-0 \
    libgeos-dev \
    libpango-1.0-0 \
    libpangocairo-1.0-0 \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# 2. ننسخ ملف المتطلبات أولاً
COPY requirements.txt .

# 3. تسطيب المكتبات (مع زيادة الـ Timeout عشان doctr بتاخد وقت)
RUN pip install --no-cache-dir --default-timeout=1000 -r requirements.txt

# 4. ننسخ باقي ملفات الكود
COPY . .

# 5. تعديل الصلاحيات
RUN chmod -R 777 /app

EXPOSE 7860

# 6. تشغيل الـ App
CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "7860"]