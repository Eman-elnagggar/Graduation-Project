# CBC Diagnostic System using Real Dataset (cbc_final_data.csv)
# Multi-Label Random Forest Model + Per-Diagnosis Top Feature Importances
# Outputs in the exact format you requested

import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.multioutput import MultiOutputClassifier
from sklearn.preprocessing import MultiLabelBinarizer
from sklearn.metrics import accuracy_score, hamming_loss, f1_score, classification_report

# Load the dataset
df = pd.read_csv('cbc_final_data.csv')

# Define features (all numerical + Gender & Age)
features = ['Gender', 'Age', 'Hb', 'RBC', 'WBC', 'PLATELETS', 'HCT', 'MCV', 'MCH', 'MCHC',
            'RDW', 'PDW', 'MPV', 'PCT', 'Lymphocytes', 'Monocytes', 'Neutrophils',
            'Eosinophils', 'Basophils']

X = df[features].values

# Define the multi-label diagnoses we want to predict (rule-based style)
labels = [
    'Leukopenia', 'Leukocytosis', 'Anemia', 'Elevated Hemoglobin',
    'Thrombocytopenia', 'Thrombocytosis', 'Microcytosis', 'Macrocytosis',
    'Elevated RDW', 'Neutropenia', 'Neutrophilia', 'Lymphopenia', 'Lymphocytosis',
    'Monocytopenia', 'Monocytosis', 'Eosinophilia', 'Basophilia',
    'Probable Iron Deficiency Anemia (IDA)'
]

# Simple rule-based labeling function (same as before, adapted for DataFrame rows)
def label_row(row_dict):
    diagnoses = []
    # Rename Hb -> HGB for rule compatibility
    row_dict = row_dict.copy()
    row_dict['HGB'] = row_dict.pop('Hb')
    row_dict['PLT'] = row_dict['PLATELETS']

    # Apply rules
    if row_dict['WBC'] < 4.0: diagnoses.append('Leukopenia')
    if row_dict['WBC'] > 10.0: diagnoses.append('Leukocytosis')
    if row_dict['HGB'] < 12.0: diagnoses.append('Anemia')
    if row_dict['HGB'] > 15.0: diagnoses.append('Elevated Hemoglobin')
    if row_dict['PLT'] < 150: diagnoses.append('Thrombocytopenia')
    if row_dict['PLT'] > 400: diagnoses.append('Thrombocytosis')
    if row_dict['MCV'] < 80: diagnoses.append('Microcytosis')
    if row_dict['MCV'] > 100: diagnoses.append('Macrocytosis')
    if row_dict['RDW'] > 15.0: diagnoses.append('Elevated RDW')
    if row_dict['Neutrophils'] < 2.5: diagnoses.append('Neutropenia')
    if row_dict['Neutrophils'] > 7.0: diagnoses.append('Neutrophilia')
    if row_dict['Lymphocytes'] < 1.0: diagnoses.append('Lymphopenia')
    if row_dict['Lymphocytes'] > 4.8: diagnoses.append('Lymphocytosis')
    if row_dict['Monocytes'] < 0.2: diagnoses.append('Monocytopenia')
    if row_dict['Monocytes'] > 0.8: diagnoses.append('Monocytosis')
    if row_dict['Eosinophils'] > 0.5: diagnoses.append('Eosinophilia')
    if row_dict['Basophils'] > 0.2: diagnoses.append('Basophilia')
    if row_dict['MCV'] < 80.0 and row_dict['RDW'] > 15.0:
        diagnoses.append('Probable Iron Deficiency Anemia (IDA)')
    return diagnoses

# Generate multi-label y from rules
y_list = []
for _, row in df.iterrows():
    row_dict = row[features].to_dict()
    y_list.append(label_row(row_dict))

# Binarize labels
mlb = MultiLabelBinarizer(classes=labels)
y = mlb.fit_transform(y_list)

print(f"Dataset: {X.shape[0]} samples, {X.shape[1]} features")
print(f"Multi-label targets: {len(labels)} possible diagnoses\n")

# Train-test split
X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

# Multi-label Random Forest model
base_rf = RandomForestClassifier(n_estimators=500, random_state=42, n_jobs=-1)
model = MultiOutputClassifier(base_rf)

print("Training multi-label Random Forest model...")
model.fit(X_train, y_train)
print("Training completed.\n")

# Evaluation
y_pred = model.predict(X_test)
print("=== Model Performance (Test Set) ===")
print(f"Subset Accuracy: {accuracy_score(y_test, y_pred):.4f}")
print(f"Hamming Loss: {hamming_loss(y_test, y_pred):.4f}")
print(f"Micro F1: {f1_score(y_test, y_pred, average='micro'):.4f}")
print(f"Macro F1: {f1_score(y_test, y_pred, average='macro', zero_division=0):.4f}\n")
# === FEATURE IMPORTANCE PER DIAGNOSIS (as requested) ===
print("=== Feature Contributions per Diagnosis ===")
importances = np.zeros((len(labels), len(features)))
for i, estimator in enumerate(model.estimators_):
    importances[i] = estimator.feature_importances_

# For each diagnosis, show top 3 features with their importance
for label_idx, label in enumerate(labels):
    top3_idx = np.argsort(importances[label_idx])[::-1][:3]
    contributions = []
    for idx in top3_idx:
        contributions.append(f"{features[idx]} ({importances[label_idx][idx]:.3f})")
    print(f"{label} -> {', '.join(contributions)}")

# === PREDICTION EXAMPLE ===
print("\n=== Sample Patient Prediction ===")
sample_idx = 0
sample_X = X_test[sample_idx:sample_idx+1]
sample_pred = model.predict(sample_X)[0]

predicted_diagnoses = [labels[i] for i in range(len(labels)) if sample_pred[i] == 1]

print("Predicted diagnoses for sample patient:", predicted_diagnoses)