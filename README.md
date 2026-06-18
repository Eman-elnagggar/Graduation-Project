<img width="1280" height="714" alt="image" src="https://github.com/user-attachments/assets/d4231b35-fb70-4d03-bd51-a5a42a6007e8" />

# Nabd (نبض) - AI-Powered Healthcare Platform

##  Overview
**Nabd** is an AI-powered healthcare platform designed to support maternal and fetal health through intelligent risk assessment, medical report analysis, and continuous clinical monitoring. 

The platform integrates Machine Learning, Computer Vision, OCR, and Conversational AI technologies to assist healthcare professionals in the early detection of pregnancy-related complications and improve clinical decision-making.

---

###  Key AI Components
* **Maternal Health Risk Prediction**
* **Gestational Diabetes (GDM) Prediction**
* **Medical Report OCR Analysis**
* **Medication Safety Analysis**
* **Fetal Cardiothoracic Ratio (CTR) Analysis** from ultrasound images
* **AI Medical Chatbot** for patient support

### Platform Capabilities
Beyond AI capabilities, **Nabd** provides a complete healthcare management system that connects patients, doctors, and medical secretaries through:
* Appointment management
* Patient follow-up
* Medical records
* Communication tools

---
> ⚠️ **Clinical Decision Support:** Nabd serves as a clinical decision-support platform that enhances healthcare delivery while ensuring that all final medical decisions remain under physician supervision.
> ### 🎥 Our Demo Video
Check out our demo video to see **Nabd** in action and explore its full features:
👉 [Watch the Demo Video Here](رابط_الفيديو_هنا)
>
## 📁 Repository Structure

```text
Graduation-Project/
├── Graduation Project/              # Main application development folder
│   ├── backend/                     # Backend API & Server-side logic (Node.js/Django/FastAPI)
│   │   ├── controllers/
│   │   ├── models/
│   │   ├── routes/
│   │   └── server.js
│   │
│   └── frontend/                    # Frontend user interface (React/Flutter/Angular)
│       ├── public/
│       ├── src/
│       │   ├── components/
│       │   └── pages/
│       └── package.json
│
├── Data Analysis & AI/              # Data science and AI modeling workspace
│   ├── datasets/                    # Prepared and processed datasets
│   ├── notebooks/                   # Jupyter notebooks (EDA, Model Training, Testing)
│   │   ├── EDA.ipynb
│   │   ├── maternal_risk_prediction.ipynb
│   │   └── gdm_prediction.ipynb
│   └── models/                      # Saved and deployed AI models (.h5, .tflite, .pkl)
│
├── Documentation/                   # Project documentation, SRS, and diagrams
├── .vs/                             # Visual Studio environment configurations
├── Graduation Project.zip           # Compressed backup of the project files
└── README.md                        # Project main documentation file
```text
---
##  Model Architecture

Nabd integrates multiple AI models to support maternal and fetal healthcare:

### Maternal Health Risk Prediction
Predicts pregnancy risk levels (**Low, Moderate, High Risk**) using maternal clinical data and machine learning.

### Gestational Diabetes (GDM) Prediction
Assesses the likelihood of gestational diabetes to support early detection and intervention.

### Medical Report OCR
Extracts and analyzes information from laboratory reports automatically using OCR and AI.

###  Medication Safety Analysis
Evaluates medication ingredients and determines their suitability during pregnancy.

###  Fetal Cardiothoracic Ratio (CTR) Analysis
Uses Computer Vision to analyze ultrasound images and calculate the fetal cardiothoracic ratio (CTR) for cardiac assessment.

###  AI Medical Chatbot
Provides instant responses to pregnancy-related questions and general medical guidance.

### Medical Report Generator

Nabd automatically generates a comprehensive digital medical report that combines laboratory test results, OCR analysis, maternal risk prediction, and GDM prediction into a single structured summary. 

The report is securely stored and accessible to both doctors and patients, enabling efficient follow-up, improved communication, and easy sharing of medical records.

###AI Report Agent

An AI-powered agent analyzes the patient's medical data and generated reports to provide concise summaries and personalized recommendations. 

The agent assists healthcare professionals by highlighting important findings and suggesting potential next steps, helping streamline clinical decision-making while keeping physicians in full control of medical decisions.
