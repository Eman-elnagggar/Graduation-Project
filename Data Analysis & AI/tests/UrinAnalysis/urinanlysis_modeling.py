# =============================================================================
# Urine Analysis - Random Forest Model for Pregnant Women Screening
# (Decision support tool - NOT a medical diagnosis)
# Ready for prediction on new data via dict input
# Features: auto flag calculation, auto scaling, confidence & reasons in output
# =============================================================================

import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import classification_report, confusion_matrix, accuracy_score

# ────────────────────────────────────────────────
# 1. Load and prepare data
# ────────────────────────────────────────────────

print("Loading dataset...")
try:
    df = pd.read_csv("final_pregnancy_urinalysis_ready.csv")
except FileNotFoundError:
    print("Error: file 'final_pregnancy_urinalysis_ready.csv' not found in current directory")
    exit(1)

print("Dataset shape:", df.shape)
print("Columns:", df.columns.tolist())
print("Class distribution:\n", df["DIAGNOSIS"].value_counts(normalize=True))

# Features & target
X = df.drop("DIAGNOSIS", axis=1)
y = df["DIAGNOSIS"]

# ────────────────────────────────────────────────
# 2. Feature engineering (using correct column names)
# ────────────────────────────────────────────────

X = X.copy()

X["UTI_suspect"] = (
    (X["WBC"] > 5) |
    (X["NITRITES"] > 0) |
    (X["LEUKOCYTE ESTERASE"] > 0) |
    (X["BACTERIA"] > 0) |
    (X["CLARITY/TURBIDITY"] > 0)
).astype(int)

X["Proteinuria_flag"] = (X["PROTEIN"] > 0).astype(int)
X["Glucose_flag"]     = (X["GLUCOSE"] > 0).astype(int)
X["Hematuria_flag"]   = ((X["RBC"] > 2) | (X["BLOOD (HEMOGLOBIN)"] > 0)).astype(int)

# ────────────────────────────────────────────────
# 3. Train / Test split + scaling
# ────────────────────────────────────────────────

X_train, X_test, y_train, y_test = train_test_split(
    X, y,
    test_size=0.20,
    random_state=42,
    stratify=y
)

scaler = StandardScaler()
X_train_scaled = scaler.fit_transform(X_train)
X_test_scaled  = scaler.transform(X_test)

print(f"Training samples: {len(X_train)}   |   Test samples: {len(X_test)}")

# ────────────────────────────────────────────────
# 4. Train Random Forest
# ────────────────────────────────────────────────

rf = RandomForestClassifier(
    n_estimators=300,
    max_depth=10,
    min_samples_split=5,
    min_samples_leaf=3,
    class_weight="balanced_subsample",
    random_state=42,
    n_jobs=-1
)

print("Training Random Forest...")
rf.fit(X_train_scaled, y_train)

# ────────────────────────────────────────────────
# 5. Evaluate on test set
# ────────────────────────────────────────────────

y_pred = rf.predict(X_test_scaled)
y_prob = rf.predict_proba(X_test_scaled)[:, 1]

print("\nAccuracy (test):", round(accuracy_score(y_test, y_pred), 4))
print("\nClassification Report:\n")
print(classification_report(y_test, y_pred))
print("\nConfusion Matrix:\n", confusion_matrix(y_test, y_pred))

# ────────────────────────────────────────────────
# 6. Prediction function (ready for new dict input)
# ────────────────────────────────────────────────

def predict_urine_analysis(new_sample_dict, threshold=0.35):
    """
    Input: dict with column names matching the training data (e.g., {'WBC': 12.0, 'NITRITES': 2.0, ...})
    - Flags calculated automatically
    - Scaling done automatically
    - Output with confidence & reasons
    """
    # Create single-row DataFrame
    row = pd.DataFrame([new_sample_dict])

    # Auto calculate flags using scalar values (.iloc[0])
    row["UTI_suspect"] = int(
        row.get("WBC", 0).iloc[0] > 5 or
        row.get("NITRITES", 0).iloc[0] > 0 or
        row.get("LEUKOCYTE ESTERASE", 0).iloc[0] > 0 or
        row.get("BACTERIA", 0).iloc[0] > 0 or
        row.get("CLARITY/TURBIDITY", 0).iloc[0] > 0
    )
    row["Proteinuria_flag"] = int(row.get("PROTEIN", 0).iloc[0] > 0)
    row["Glucose_flag"]     = int(row.get("GLUCOSE", 0).iloc[0] > 0)
    row["Hematuria_flag"]   = int(
        (row.get("RBC", 0).iloc[0] > 2) or
        (row.get("BLOOD (HEMOGLOBIN)", 0).iloc[0] > 0)
    )

    # Align columns with training data
    row = row.reindex(columns=X.columns, fill_value=0)

   
    row_scaled = scaler.transform(row)

    # Predict
    prob = rf.predict_proba(row_scaled)[0, 1]
    is_abnormal = prob >= threshold

    # ─── Build reasons & risk ───
    reasons = []
    risk_type = "Unknown"

    values = row.iloc[0]  

    if is_abnormal:
        if values["UTI_suspect"] == 1:
            risk_type = "Urinary Tract Infection"
            if values.get("WBC", 0) > 5:
                reasons.append("WBC > 5")
            if values.get("LEUKOCYTE ESTERASE", 0) > 0:
                reasons.append("Leukocyte Esterase Positive")
            if values.get("NITRITES", 0) > 0:
                reasons.append("Nitrites Positive")
            if values.get("BACTERIA", 0) > 0:
                reasons.append("Bacteria Present")
            if values.get("CLARITY/TURBIDITY", 0) > 0:
                reasons.append("Turbid urine")

        elif values["Proteinuria_flag"] == 1:
            risk_type = "Preeclampsia Risk"
            reasons.append("Protein Positive")

        elif values["Glucose_flag"] == 1:
            risk_type = "Gestational Diabetes Risk"
            reasons.append("Glucose Positive")

        else:
            risk_type = "Other abnormality"
            reasons.append("General abnormal finding")

        confidence = round(prob * 100)
    else:
        risk_type = None
        reasons = ["No significant abnormal findings"]
        confidence = round((1 - prob) * 100)

    # ─── Print in requested format ───
    print("\n" + "="*50)
    print("Result:", "abnormal" if is_abnormal else "Normal")
    if risk_type:
        print("Risk Type:", risk_type)
    print("Reasons:")
    for r in reasons:
        print(f"- {r}")
    print(f"Confidence: {confidence}%")
    print("="*50 + "\n")


# ────────────────────────────────────────────────
# Example usage
# ────────────────────────────────────────────────

print("\nRunning example predictions...\n")

example_abnormal = {
    "WBC": 12.0,
    "LEUKOCYTE ESTERASE": 4.0,
    "NITRITES": 2.0,
    "PROTEIN": 0.0,
    "GLUCOSE": 0.0,
    "BACTERIA": 1.0,
    "CLARITY/TURBIDITY": 2.0,
    "RBC": 1.0,
    "BLOOD (HEMOGLOBIN)": 0.0,
    # other columns will be filled with 0
}

predict_urine_analysis(example_abnormal, threshold=0.30)

example_normal = {
    "WBC": 3.0,
    "LEUKOCYTE ESTERASE": 0.0,
    "NITRITES": 0.0,
    "PROTEIN": 0.0,
    "GLUCOSE": 0.0,
    "BACTERIA": 0.0,
    "CLARITY/TURBIDITY": 0.0,
    "RBC": 1.0,
    "BLOOD (HEMOGLOBIN)": 0.0,
}

predict_urine_analysis(example_normal)