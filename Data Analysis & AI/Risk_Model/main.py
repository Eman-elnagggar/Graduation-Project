from fastapi import FastAPI
from fastapi.responses import RedirectResponse
import joblib
import pandas as pd
import numpy as np

app = FastAPI()

@app.get("/")
async def root():
    return RedirectResponse(url="/docs")

# =========================
# Load Model
# =========================
model = joblib.load("model.pkl")

FEATURES = list(model.feature_names_in_)
print("Model Features:", FEATURES)

# =========================
# Risk Mapping
# =========================
risk_map = {
    0: "Low Risk",
    1: "Moderate Risk",
    2: "High Risk"
}

# =========================
# API Endpoint
# =========================
@app.post("/predict")
def predict(data: dict):
    try:
        df_fe = pd.DataFrame([data])

        # =========================
        # Feature Engineering
        # =========================

        # MAP
        df_fe['MAP'] = (
            df_fe['systolic_bp_mmhg'] + 2 * df_fe['diastolic_bp_mmhg']
        ) / 3

        # glucose_gestation
        df_fe['glucose_gestation'] = (
            df_fe['fasting_glucose_mgdl'] * df_fe['gestational_age_weeks']
        )

        # is_high_risk_age
        df_fe['is_high_risk_age'] = df_fe['age_years'].apply(
            lambda x: 1 if x < 18 or x > 35 else 0
        )

        # risk_index
        df_fe['risk_index'] = (
            df_fe['MAP'] * df_fe['fasting_glucose_mgdl']
        )

        # =========================
        # BMI (NEW)
        # =========================
        df_fe['height_m'] = df_fe['height_cm'].replace(0, np.nan) / 100
        df_fe['bmi'] = df_fe['weight_kg'] / (df_fe['height_m'] ** 2)

        # =========================
        # Match training features
        # =========================
        df_fe = df_fe.reindex(columns=FEATURES)

        # =========================
        # Prediction
        # =========================
        proba = model.predict_proba(df_fe)[0]
        classes = model.classes_

        idx = proba.argmax()

        prediction = int(classes[idx])
        confidence = float(proba[idx])

        # =========================
        # Response
        # =========================
        return {
            "prediction": prediction,
            "risk_level": risk_map.get(prediction, "Unknown"),
            "confidence": confidence
        }

    except Exception as e:
        return {"error": str(e)}