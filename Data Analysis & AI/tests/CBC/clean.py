import pandas as pd
import numpy as np
import joblib

# 1. Load the original absolute counts data
# Use the file BEFORE scaling to keep real medical values
df = pd.read_csv('cbc_final_data.csv')

# 2. Map your new names to ensure the rules work
# Ensure these names match what you renamed in the CSV
features = ['Hb', 'RBC', 'WBC', 'PLATELETS', 'HCT', 'MCV', 'MCH', 'MCHC', 'RDW', 
            'Lymphocytes', 'Monocytes', 'Neutrophils', 'Eosinophils', 'Basophils']

# 3. Basic Cleaning (No Scaling)
df = df.dropna(subset=features)
for col in features:
    df[col] = pd.to_numeric(df[col], errors='coerce')
df = df.dropna()

print(f"Data cleaned. Total samples: {len(df)}")
print("Note: Scaling skipped to keep medical units (g/dL, 10^3/uL, etc.) for rules.")