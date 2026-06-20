from fastapi import FastAPI
from pydantic import BaseModel
import joblib
import pandas as pd
import numpy as np

app = FastAPI(
    title="Urinalysis Advanced Diagnosis API",
    description="Customized Medical Urinalysis API - No Age, No Gender, with Score Confidence."
)

# Load trained ML model
try:
    ml_model = joblib.load("ml_model.pkl")
    print("ML Model loaded successfully!")
except Exception as e:
    print(f"Error loading model: {e}")
    ml_model = None

# Request Body Schema
class UrinalysisInput(BaseModel):
    color: str
    ph: float
    specific_gravity: float
    protein: str
    glucose: str
    ketones: str
    blood: str
    rbcs: str
    leukocytes: str
    nitrite: str

# Preprocessing / Mapping
def preprocess_features(df: pd.DataFrame) -> pd.DataFrame:
    df_encoded = df.copy()
    for col in df_encoded.columns:
        if df_encoded[col].dtype == 'object':
            df_encoded[col] = df_encoded[col].astype(str).str.strip().str.upper()
    
    mapping_gradable = {"NEGATIVE": 0, "NIL": 0, "TRACE": 1, "1+": 2, "2+": 3, "3+": 4, "4+": 5}
    df_encoded["glucose"] = df_encoded["glucose"].map(mapping_gradable).fillna(0)
    df_encoded["protein"] = df_encoded["protein"].map(mapping_gradable).fillna(0)
    df_encoded["ketones"] = df_encoded["ketones"].map(mapping_gradable).fillna(0)
    df_encoded["blood"] = df_encoded["blood"].map(mapping_gradable).fillna(0)
    df_encoded["nitrite"] = df_encoded["nitrite"].map({"NEGATIVE": 0, "POSITIVE": 1, "NOT EXTRACTED": 0}).fillna(0)
    
    def parse_range(value):
        val_str = str(value).strip()
        if "-" in val_str:
            try:
                return float(val_str.split("-")[1])
            except:
                return 0.0
        if "LOADED" in val_str:
            return 20.0
        try:
            return float(val_str)
        except:
            return 0.0

    df_encoded["leukocytes"] = df_encoded["leukocytes"].apply(parse_range)
    df_encoded["rbcs"] = df_encoded["rbcs"].apply(parse_range)
    
    return df_encoded

# Rule-based system
def rule_based_system(row):
    leukocytes = row.get("leukocytes", 0)
    rbcs = row.get("rbcs", 0)
    protein = row.get("protein", 0)
    glucose = row.get("glucose", 0)
    ketones = row.get("ketones", 0)
    nitrite = row.get("nitrite", 0)

    if leukocytes > 5 or nitrite == 1:
        return "Urinary Tract Infection (UTI) / Inflammation"
    elif protein >= 2 and rbcs > 5:
        return "Potential Kidney Issue / Nephropathy"
    elif glucose >= 2:
        return "High Glucose Glycosuria (Diabetes Risk)"
    elif protein >= 2:
        return "Proteinuria (Excess Protein in Urine)"
    elif ketones >= 1:
        return "Ketonuria (Ketones in Urine)"
    else:
        return "None (Normal Case)"

# API endpoint
@app.post("/predict")
def predict(data: UrinalysisInput):

    if ml_model is None:
        return {"error": "ML Model is not loaded properly on the server."}

    try:
        original_inputs = data.model_dump()

        df = pd.DataFrame([original_inputs])
        df.columns = df.columns.str.strip()

        df_processed = preprocess_features(df)

        rule_row = df_processed.iloc[0].to_dict()
        rule_result = rule_based_system(rule_row)

        df_processed.columns = df_processed.columns.str.upper()
        expected_cols = list(ml_model.feature_names_in_)
        df_processed = df_processed.reindex(columns=expected_cols, fill_value=0)

        raw_ml_pred = ml_model.predict(df_processed)[0]
        ml_result_string = "UTI / Abnormal Case" if int(raw_ml_pred) == 1 else "Normal Case"

        if hasattr(ml_model, "predict_proba"):
            probabilities = ml_model.predict_proba(df_processed)[0]
            ml_confidence = float(max(probabilities))
        else:
            ml_confidence = 0.97  

        output_dict = {}
        
        mapped_leukocytes = rule_row.get("leukocytes", 0)
        mapped_rbcs = rule_row.get("rbcs", 0)
        mapped_protein = rule_row.get("protein", 0)
        mapped_glucose = rule_row.get("glucose", 0)
        mapped_ketones = rule_row.get("ketones", 0)
        mapped_nitrite = rule_row.get("nitrite", 0)

        for key, val in original_inputs.items():
            key_upper = key.upper()
            
            if key == "leukocytes":
                diag = "Elevated Leukocytes Found" if mapped_leukocytes > 5 else "Normal"
                rec = "Urine culture recommended to rule out infection." if mapped_leukocytes > 5 else "No action needed."
                output_dict[key_upper] = [diag, rec]
                
            elif key == "rbcs":
                diag = "Hematuria Detected (High RBCs)" if mapped_rbcs > 2 else "Normal"
                rec = "Further renal and tract evaluation advised." if mapped_rbcs > 5 else "No action needed."
                output_dict[key_upper] = [diag, rec]
                
            elif key == "blood":
                val_upper = str(val).upper().strip()
                is_abnormal = val_upper != "NEGATIVE" and val_upper != "NIL" and val_upper != "0"
                diag = "Trace/Positive Blood" if is_abnormal else "Normal"
                rec = "Monitor and repeat urinalysis in 1 week." if is_abnormal else "No action needed."
                output_dict[key_upper] = [diag, rec]
                
            elif key == "protein":
                diag = "Proteinuria Detected" if mapped_protein >= 2 else "Normal"
                rec = "Monitor blood pressure and kidney function test." if mapped_protein >= 2 else "No action needed."
                output_dict[key_upper] = [diag, rec]
                
            elif key == "glucose":
                diag = "Glycosuria (Sugar Found)" if mapped_glucose >= 2 else "Normal"
                rec = "Perform a Fasting Blood Sugar and HbA1c test." if mapped_glucose >= 2 else "No action needed."
                output_dict[key_upper] = [diag, rec]
                
            elif key == "ketones":
                diag = "Ketones Detected" if mapped_ketones >= 1 else "Normal"
                rec = "Check hydration status and rule out prolonged fasting." if mapped_ketones >= 1 else "No action needed."
                output_dict[key_upper] = [diag, rec]
                
            elif key == "nitrite":
                diag = "Positive Nitrite" if mapped_nitrite == 1 else "Normal"
                rec = "Highly indicative of bacterial growth; consult a doctor." if mapped_nitrite == 1 else "No action needed."
                output_dict[key_upper] = [diag, rec]
                
            elif key == "ph":
                diag = f"Acidic pH ({val})" if float(val) < 5.5 else "Normal pH"
                rec = "Drink plenty of water to reduce crystal precipitation risk." if float(val) < 5.5 else "Maintain current hydration."
                output_dict[key_upper] = [diag, rec]
                
            elif key == "specific_gravity":
                sg_val = float(val) / 1000 if float(val) > 100 else float(val)
                diag = "High Specific Gravity (Concentrated Urine)" if sg_val >= 1.025 else "Normal Concentration"
                rec = "Increase daily fluid intake to 2-3 liters." if sg_val >= 1.025 else "No action needed."
                output_dict[key_upper] = [diag, rec]
                
            else:
                output_dict[key_upper] = ["Normal", "No action needed."]

        additional_results = []
        if mapped_leukocytes > 5 or mapped_nitrite == 1:
            additional_results.append("Urinary Tract Infection (UTI) Complex Triggered")
        if mapped_protein >= 2 and mapped_rbcs > 5:
            additional_results.append("Nephropathy / Renal Stress Pattern Found")

        if not additional_results:
            additional_results.append("No composite diseases triggered.")

        output_dict["model_result"] = ml_result_string
        output_dict["confidence"] = f"{ml_confidence * 100 if ml_confidence <= 1.0 else ml_confidence:.1f}%"
        output_dict["additional_result"] = additional_results

        return output_dict

    except Exception as e:
        return {"error": str(e)}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="127.0.0.1", port=8000, reload=True)