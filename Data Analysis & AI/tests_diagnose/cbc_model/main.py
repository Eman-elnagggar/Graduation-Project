from fastapi import FastAPI
from pydantic import BaseModel
import joblib
import pandas as pd
import numpy as np

# 1. Initialize FastAPI app
app = FastAPI(title="Professional CBC Diagnostic System")

# 2. Load the saved components
try:
    model = joblib.load('female_cbc_model.pkl')
    scaler = joblib.load('scaler.pkl')
    mlb = joblib.load('mlb.pkl')
    features = joblib.load('features_list.pkl')
    print("✅ System Ready: Models loaded.")
except Exception as e:
    print(f"❌ Error during loading: {e}")

# 3. Data Schema
class PatientData(BaseModel):
    Age: float; Hb: float; RBC: float; WBC: float; PLATELETS: float
    HCT: float; MCV: float; MCH: float; MCHC: float; RDW: float
    PDW: float; MPV: float; PCT: float; Lymphocytes: float
    Monocytes: float; Neutrophils: float; Eosinophils: float; Basophils: float

@app.get("/")
def home():
    return {"message": "AI Diagnostic System is Online."}

@app.post("/predict")
def predict(patient: PatientData):
    try:
        input_dict = patient.model_dump()
        input_df = pd.DataFrame([input_dict])[features]
        scaled_data = scaler.transform(input_df)
        probabilities = model.predict_proba(scaled_data)
        
        all_findings = []
        found_diseases_info = []

        # --- 3. Detailed Analysis with FULL Medical Overrides ---
        for i, label in enumerate(mlb.classes_):
            conf = probabilities[i][0][1] * 100 if len(probabilities[i][0]) > 1 else 0.0
            
            # 🛑 الفلاتر الصارمة: لو الرقم طبيعي، امسح الحالة دي من حساباتك خالص
            if label == 'Leukocytosis' and patient.WBC <= 11.0:
                continue
            if label == 'Leukopenia' and patient.WBC >= 4.0: # الفلتر اللي كان ناقص
                continue
            if label == 'Anemia' and patient.Hb >= 12.0:
                continue
            if label == 'Thrombocytopenia' and patient.PLATELETS >= 150.0:
                continue
            if label == 'Elevated Hemoglobin' and patient.Hb <= 16.0:
                continue
            if label == 'Microcytosis' and patient.MCV >= 80.0:
                continue
            if label == 'Macrocytosis' and patient.MCV <= 100.0:
                continue
            if label == 'High RDW' or label == 'Elevated RDW':
                if patient.RDW <= 14.5:
                    continue

            # إظهار الحالات التي تتخطى الـ 30% فقط بعد تجاوز الفلاتر
            if conf >= 30:
                # صياغة أسباب ذكية ومربوطة بالأرقام
                if label == 'Anemia':
                    reason = f"Hemoglobin ({patient.Hb}) is below normal range (<12 g/dL)."
                elif label == 'Leukocytosis':
                    reason = f"WBC count ({patient.WBC}) is elevated, suggesting immune response."
                elif label == 'Leukopenia':
                    reason = f"WBC count ({patient.WBC}) is lower than normal, suggesting low immunity."
                elif label == 'Probable Iron Deficiency Anemia (IDA)':
                    reason = f"Low Hb ({patient.Hb}) and MCV ({patient.MCV}) with high RDW suggest Iron Deficiency."
                else:
                    reason = "Blood indices show patterns consistent with this condition."

                finding = {"condition": label, "confidence": f"{conf:.1f}%", "reason": reason}
                all_findings.append(finding)
                
                if conf >= 70:
                    found_diseases_info.append(finding)

        # --- 4. Final Report Generation ---
        if not all_findings: # الحالة بقت طبيعية تماماً بعد الفلاتر
            conclusion = "Conclusion: Your blood profile is within normal ranges."
            reasons = "Reasoning: All core blood parameters (Hb, WBC, RBC, Platelets) are within standard clinical limits."
            steps = "Steps: Maintain a healthy lifestyle and continue with regular annual check-ups."
            urgency = "Low"
        elif not found_diseases_info: # لو فيه حالات بسيطة (بين 30% و 70%)
            conclusion = "Conclusion: Minor variations detected in your blood work."
            reasons = "Reasoning: " + " ".join([f["reason"] for f in all_findings])
            steps = "Steps: 1. Monitor your health. 2. Share these results with your physician during your next visit."
            urgency = "Low"
        else: # حالات مؤكدة (> 70%)
            urgency = "High"
            conclusion = f"Conclusion: The system detected patterns of {', '.join([d['condition'] for d in found_diseases_info])}."
            reasons = "Reasoning: " + " ".join([d['reason'] for d in found_diseases_info])
            steps = "Steps: 1. Consult your physician immediately for evaluation. 2. Further diagnostic tests may be required."

        report_text = f"{conclusion} {reasons} {steps}"

        return {
            "status": "success",
            "detailed_analysis": all_findings,
            "final_report": {
                "message": report_text,
                "urgency_level": urgency
            }
        }

    except Exception as e:
        return {"status": "error", "message": str(e)}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)