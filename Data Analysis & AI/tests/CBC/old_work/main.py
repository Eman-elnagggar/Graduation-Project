# =========================================================
# CBC Diagnostic System (HYBRID VERSION)
# Rule-Based + ML + Medical Constraints + Thresholds
# =========================================================

import numpy as np
import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.multioutput import MultiOutputClassifier
from sklearn.preprocessing import MultiLabelBinarizer
from sklearn.metrics import accuracy_score, hamming_loss, f1_score


df = pd.read_csv('./CBC/cbc_final_data.csv')

features = [
    'Gender', 'Age', 'Hb', 'RBC', 'WBC', 'PLATELETS', 'HCT', 'MCV', 'MCH', 'MCHC',
    'RDW', 'PDW', 'MPV', 'PCT', 'Lymphocytes', 'Monocytes', 'Neutrophils',
    'Eosinophils', 'Basophils'
]

X = df[features].values

labels = [
    'Leukopenia', 'Leukocytosis', 'Anemia', 'Elevated Hemoglobin',
    'Thrombocytopenia', 'Thrombocytosis', 'Microcytosis', 'Macrocytosis',
    'Elevated RDW', 'Neutropenia', 'Neutrophilia', 'Lymphopenia', 'Lymphocytosis',
    'Monocytopenia', 'Monocytosis', 'Eosinophilia', 'Basophilia',
    'Probable Iron Deficiency Anemia (IDA)'
]


def label_row(row):
    diagnoses = []

    HGB = row['Hb']
    PLT = row['PLATELETS']

    if row['WBC'] < 4.0: diagnoses.append('Leukopenia')
    if row['WBC'] > 10.0: diagnoses.append('Leukocytosis')
    if HGB < 12.0: diagnoses.append('Anemia')
    if HGB > 15.0: diagnoses.append('Elevated Hemoglobin')
    if PLT < 150: diagnoses.append('Thrombocytopenia')
    if PLT > 400: diagnoses.append('Thrombocytosis')
    if row['MCV'] < 80: diagnoses.append('Microcytosis')
    if row['MCV'] > 100: diagnoses.append('Macrocytosis')
    if row['RDW'] > 15: diagnoses.append('Elevated RDW')
    if row['Neutrophils'] < 2.5: diagnoses.append('Neutropenia')
    if row['Neutrophils'] > 7.0: diagnoses.append('Neutrophilia')
    if row['Lymphocytes'] < 1.0: diagnoses.append('Lymphopenia')
    if row['Lymphocytes'] > 4.8: diagnoses.append('Lymphocytosis')
    if row['Monocytes'] < 0.2: diagnoses.append('Monocytopenia')
    if row['Monocytes'] > 0.8: diagnoses.append('Monocytosis')
    if row['Eosinophils'] > 0.5: diagnoses.append('Eosinophilia')
    if row['Basophils'] > 0.2: diagnoses.append('Basophilia')

    if row['MCV'] < 80 and row['RDW'] > 15:
        diagnoses.append('Probable Iron Deficiency Anemia (IDA)')
    if len(diagnoses)==0:
        diagnoses.append('Normal')
    return diagnoses

y_list = []
for _, r in df.iterrows():
    y_list.append(label_row(r))

mlb = MultiLabelBinarizer(classes=labels)
y = mlb.fit_transform(y_list)

print(f"Dataset: {X.shape[0]} samples, {X.shape[1]} features")
print(f"Multi-label diagnoses: {len(labels)}\n")


X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, random_state=42
)


rf = RandomForestClassifier(
    n_estimators=500,
    random_state=42,
    n_jobs=-1
)

model = MultiOutputClassifier(rf)
model.fit(X_train, y_train)


y_pred = model.predict(X_test)

print("=== Model Performance ===")
print(f"Subset Accuracy: {accuracy_score(y_test, y_pred):.4f}")
print(f"Hamming Loss: {hamming_loss(y_test, y_pred):.4f}")
print(f"Micro F1: {f1_score(y_test, y_pred, average='micro'):.4f}")
print(f"Macro F1: {f1_score(y_test, y_pred, average='macro'):.4f}\n")


# Feature Importance

# print("=== Feature Contributions per Diagnosis ===")

# for i, est in enumerate(model.estimators_):
#     imp = est.feature_importances_
#     top3 = np.argsort(imp)[::-1][:3]
#     info = ", ".join([f"{features[j]} ({imp[j]:.3f})" for j in top3])
#     print(f"{labels[i]} -> {info}")


thresholds = {
    'Anemia': 0.4,
    'Elevated RDW': 0.4,
    'Probable Iron Deficiency Anemia (IDA)': 0.35
}


def apply_medical_constraints(results):
    conflicts = [
        ('Leukopenia', 'Leukocytosis'),
        ('Anemia', 'Elevated Hemoglobin'),
        ('Microcytosis', 'Macrocytosis')
    ]

    final = results.copy()

    for a, b in conflicts:
        if a in final and b in final:
            if final[a] >= final[b]:
                final.pop(b)
            else:
                final.pop(a)

    return final


# Hybrid Prediction Example

print("\n=== Sample Patient HYBRID Prediction ===")

sample_X = X_test[:1]
sample_row = pd.Series(sample_X[0], index=features)

# Rule-based
rule_based = label_row(sample_row)

# ML probabilities
ml_results = {}

for i, est in enumerate(model.estimators_):
    prob = est.predict_proba(sample_X)[0][1]
    label = labels[i]
    th = thresholds.get(label, 0.5)

    if prob >= th:
        ml_results[label] = round(prob, 3)

# Apply medical constraints هنا هنعمل
ml_results = apply_medical_constraints(ml_results)

print("Rule-Based Diagnosis:")
for d in rule_based:
    print(f"- {d}")

print("\nML Suggestions (with confidence):")
for d, c in ml_results.items():
    print(f"- {d} (confidence: {c})")



