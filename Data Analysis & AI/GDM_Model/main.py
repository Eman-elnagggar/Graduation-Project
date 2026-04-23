from fastapi import FastAPI
import joblib
import pandas as pd

app = FastAPI()


model = joblib.load("gestational_diabetes_logistic_pipeline.pkl")


def calculate_bmi(weight_kg, height_cm):
    height_m = height_cm / 100
    return weight_kg / (height_m ** 2)


def bmi_to_encoded(bmi):
    if bmi < 18.5:
        return 0
    elif bmi <= 24.9:
        return 1
    elif bmi <= 29.9:
        return 2
    else:
        return 3


def calculate_map(systolic, diastolic):
    return (systolic + 2 * diastolic) / 3

@app.get("/")
def home():
    return {"message": "Gestational Diabetes API is running"}

@app.post("/predict")
def predict(data: dict):
    try:
       
        weight = data["weight_kg"]
        height = data["height_cm"]
        bmi_value = calculate_bmi(weight, height)
        bmi_encoded = bmi_to_encoded(bmi_value)
        
        systolic = data["systolic_bp"]
        diastolic = data["diastolic_bp"]
        map_value = calculate_map(systolic, diastolic)

       
        input_data = {
            "age_years": data["age_years"],
            "fasting_glucose_mgdl": data["fasting_glucose_mgdl"],
            "proteinuria": data["proteinuria"],
            "bmi_category_encoded": bmi_encoded,
            "MAP": map_value
        }
        
        df = pd.DataFrame([input_data])

        
        prediction = int(model.predict(df)[0])
        probability = float(model.predict_proba(df)[0][1])

        
        if probability < 0.5:
            risk = "Low Risk"
        elif probability < 0.8:
            risk = "Medium Risk"
        else:
            risk = "High Risk"

        
        return {
            "status": "success",
            "prediction": "Diabetic" if prediction == 1 else "Non-Diabetic",
            "risk_score": f"{round(probability * 100, 2)}%",
            "risk_level": risk,
            "recommendation": "Please consult a doctor for a formal diagnosis."
        }

    except Exception as e:
        return {"status": "error", "message": str(e)}