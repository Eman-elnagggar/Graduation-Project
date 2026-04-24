# استخدام نسخة Python مناسبة
FROM python:3.9

# تحديد مجلد العمل داخل الحاوية
WORKDIR /code

# نسخ ملف المتطلبات أولاً للاستفادة من الـ Cache
COPY ./requirements.txt /code/requirements.txt

# تثبيت المكتبات
RUN pip install --no-cache-dir --upgrade -r /code/requirements.txt

# نسخ كل ملفات المشروع إلى الحاوية
COPY . .

# Hugging Face يتطلب العمل على منفذ 7860 بشكل افتراضي
CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "7860"]