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
```
---
# <span style="font-size: 32px;">🧠 Model Architecture</span>

Nabd integrates multiple AI models to support maternal and fetal healthcare:

### Medical Report OCR
Extracts and analyzes information from laboratory reports automatically using OCR and AI.

### Maternal Health Risk Prediction
Predicts pregnancy risk levels (**Low, Moderate, High Risk**) using maternal clinical data and machine learning.

### Gestational Diabetes (GDM) Prediction
Assesses the likelihood of gestational diabetes to support early detection and intervention.

###  Medication Safety Analysis
Evaluates medication ingredients and determines their suitability during pregnancy.

###  Fetal Cardiothoracic Ratio (CTR) Analysis
Uses Computer Vision to analyze ultrasound images and calculate the fetal cardiothoracic ratio (CTR) for cardiac assessment.

###  AI Medical Chatbot
Provides instant responses to pregnancy-related questions and general medical guidance.

### Medical Report Generator
Nabd automatically generates a structured digital medical report combining lab results, OCR analysis, maternal risk, and GDM predictions. Securely stored and accessible to both doctors and patients, it enhances clinical follow-up, communication, and medical record sharing.

### **AI Report Agent**
An AI-powered agent analyzes patient data and reports to provide concise summaries and personalized recommendations. It assists healthcare professionals by highlighting key findings and suggesting next steps, streamlining clinical decisions under full physician supervision.

---

# <span style="font-size: 32px;">⚙️ Training Pipeline</span>

The full AI research and development pipeline is documented and available under the `Data Analysis & AI/` directory:
* `GDM_Model/` – Training, tuning, and evaluation for Gestational Diabetes screening.
* `Risk_Model/` – Model architecture and training pipeline for Maternal Health Risk prediction.
* `cardiac_thoracic_ratio/` – Computer Vision pipeline for Fetal Cardiothoracic Ratio (CTR) analysis.
* `OCR/` & `tests_diagnose/` – Text extraction from medical reports and diagnostic classification models..

### 🚀 Key Pipeline Steps
1. **Data Cleaning & Preprocessing:** Handling missing clinical values, medical text normalization, and image filtering.
2. **Train/Valid/Test Splits:** Ensuring stratified splits to maintain balanced risk classes across datasets.
3. **Image Augmentation & Computer Vision:** Applying advanced transformations (rotation, scaling, contrast adjustment) for ultrasound CTR analysis.
4. **Transfer Learning & Fine-Tuning:** Leveraging pre-trained deep learning architectures for high-accuracy medical image classification.
5. **Model Evaluation:** Detailed assessment using Accuracy, Confusion Matrix, Precision, Recall, and F1-Score.
6. **Model Optimization & Deployment:** Exporting models and applying quantization (e.g., TFLite, Pickle) for fast inference via FastAPI.

 ---
---

# ⚙️ Technology Stack

### Backend
* ASP.NET Core MVC
* Entity Framework Core
* SQL Server

### AI Services
* Python
* FastAPI
* Scikit-Learn
* Pandas
* NumPy
* Joblib

### Frontend
* HTML
* CSS
* JavaScript
* Bootstrap

### Database
* Microsoft SQL Server
* 
# <span style="font-size: 32px;">How to Use </span>

**Nabd** is fully deployed and ready for live testing! You don't need to install or configure anything locally. You can access the platform and explore all its AI features directly via the link below:

👉 **[Launch Nabd Live Platform](http://nabd-sys.runasp.net/)**

---

# <span style="font-size: 32px;"> Future Work</span>

We plan to continuously improve and expand the **Nabd** ecosystem by integrating more advanced technologies and extending its clinical capabilities:

### ⌚ 1. Wearable & IoT Medical Devices Integration
* Connect the system with smartwatches and specialized health sensors to enable continuous, real-time monitoring of critical patient vitals (such as heart rate, blood pressure, and oxygen levels).
* Allow proactive data sync that alerts doctors automatically if any sudden clinical anomalies are detected between visits.

### 🗣️ 2. Multilingual Voice-Enabled AI Assistant
* Upgrade the **AI Medical Chatbot** to support seamless voice-to-text interactions in both Formal Arabic and local Egyptian dialects.
* This will significantly improve accessibility for pregnant users who prefer speaking over typing, or those with limited literacy, enabling inclusive communication.

### 📍 3. Location-Based Healthcare Recommendations
* Enable the system to intelligently suggest nearby hospitals, specialized clinics, and emergency services based on the patient’s live location.
* This is designed to save critical time, especially in high-risk situations requiring immediate medical attention.

### 🏥 4. Full Integration with Hospital EHR Systems
* Connect Nabd with Electronic Health Record (EHR) systems to enable secure and seamless data exchange between hospitals, clinics, and the platform.
* Ensure data portability using international healthcare standards to allow medical professionals to access the patient's multi-model AI history instantly.


