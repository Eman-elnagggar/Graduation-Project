FROM python:3.10-slim

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

WORKDIR /app

RUN apt-get update && apt-get install -y \
    build-essential \
    libglib2.0-0 \
    libgl1 \
    git \
    && rm -rf /var/lib/apt/lists/*


COPY requirements.txt .


RUN pip install --upgrade pip
RUN pip install --no-cache-dir -r requirements.txt


COPY . .


EXPOSE 7860

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "7860"]