import pandas as pd
import numpy as np

# Load the dataset
data = pd.read_csv('updated_urinalysis_pregnancy.csv')

# Filter for pregnant women (assuming female gender indicates pregnancy context)
data = data[data['Gender'] == 'FEMALE']

# Month abbreviation to number mapping for range parsing
month_map = {
    'Jan': 1, 'Feb': 2, 'Mar': 3, 'Apr': 4, 'May': 5, 'Jun': 6,
    'Jul': 7, 'Aug': 8, 'Sep': 9, 'Oct': 10, 'Nov': 11, 'Dec': 12
}

# Function to parse cell count ranges (e.g., '0-2', '1-Mar' -> 1-3, 'LOADED' -> 100)
def parse_cell_count(s):
    if pd.isna(s):
        return np.nan
    s = str(s).strip().upper()
    if s == 'LOADED':
        return 100.0
    if s.startswith('>'):
        try:
            return float(s[1:]) + 1
        except:
            return np.nan
    if '-' in s:
        parts = s.split('-')
        if len(parts) != 2:
            return np.nan
        low, high = parts
        low = month_map.get(low, low)
        high = month_map.get(high, high)
        try:
            low = float(low)
            high = float(high)
            return (low + high) / 2
        except:
            return np.nan
    else:
        val = month_map.get(s, s)
        try:
            return float(val)
        except:
            return np.nan

# Apply parsing to WBC and RBC
data['WBC'] = data['WBC'].apply(parse_cell_count)
data['RBC'] = data['RBC'].apply(parse_cell_count)

# Function to map chemical test results to binary (0 for negative/trace, 1 for positive)
def map_to_binary(s):
    if pd.isna(s):
        return np.nan
    s = str(s).strip().upper()
    if s in ['NEGATIVE', 'NEG', 'TRACE']:
        return 0
    elif s in ['POSITIVE', '1+', '2+', '3+', '4+']:
        return 1
    else:
        return np.nan

# Binary columns
binary_cols = ['Glucose', 'Protein', 'Ketones', 'Blood (Hemoglobin)', 'Nitrites', 'Leukocyte Esterase', 'Bilirubin']
for col in binary_cols:
    data[col] = data[col].apply(map_to_binary)

# Function to parse Urobilinogen (NORMAL -> 0.6 avg, numerical or + levels)
def parse_urobilinogen(s):
    if pd.isna(s):
        return np.nan
    s = str(s).strip().upper()
    if s == 'NORMAL':
        return 0.6
    if s == 'TRACE':
        return 0.2
    if s.endswith('+'):
        try:
            return float(s[:-1])
        except:
            return np.nan
    try:
        return float(s)
    except:
        return np.nan

data['Urobilinogen'] = data['Urobilinogen'].apply(parse_urobilinogen)

# Function to map qualitative microscopic results to numerical scale
def map_qualitative(s):
    if pd.isna(s):
        return np.nan
    s = str(s).strip().upper()
    if s in ['NONE SEEN', 'NONE', 'NEGATIVE', 'NEG']:
        return 0
    if s in ['RARE', 'OCCASIONAL']:
        return 1
    if s == 'FEW':
        return 2
    if s == 'MODERATE':
        return 3
    if s == 'PLENTY':
        return 4
    if s == 'LOADED':
        return 5
    else:
        return np.nan

# Qualitative columns
qual_cols = ['Epithelial Cells', 'crystals', 'Bacteria']
for col in qual_cols:
    data[col] = data[col].apply(map_qualitative)

# For Bacteria, convert to binary as per model (0 if none, 1 if present)
data['Bacteria'] = (data['Bacteria'] > 0).astype(int)

# Function to map Casts
def map_casts(s):
    if pd.isna(s):
        return np.nan
    s = str(s).strip().upper()
    if s == 'NONE SEEN':
        return 0
    if 'RARE' in s:
        return 1
    if 'FEW' in s:
        return 2
    else:
        return 0  # Default to 0 for other types

data['Casts'] = data['Casts'].apply(map_casts)

# Function to map Clarity/Turbidity
def map_clarity(s):
    if pd.isna(s):
        return np.nan
    s = str(s).strip().upper()
    if s == 'CLEAR':
        return 0
    if s == 'SLIGHTLY HAZY':
        return 1
    if s == 'HAZY':
        return 2
    if s in ['TURBID', 'CLOUDY']:
        return 3
    else:
        return np.nan

data['Clarity/Turbidity'] = data['Clarity/Turbidity'].apply(map_clarity)

# Rename columns to match model expectations
data = data.rename(columns={'Specific Gravity': 'SG', 'Blood (Hemoglobin)': 'Blood'})
# Feature Engineering (as per model)
data['WBC_flag'] = (data['WBC'] > 5).astype(int)
data['Protein_flag'] = data['Protein']
data['Diabetes_flag'] = data['Glucose']
data['UTI_flag'] = ((data['WBC'] > 5) | (data['Leukocyte Esterase'] > 0) | (data['Nitrites'] > 0) | (data['Bacteria'] > 0)).astype(int)

# Create multi-class labels based on rules (prioritizing risks)
def create_label(row):
    if row['Glucose'] > 0:
        return 3  # Gestational Diabetes Risk
    elif row['Protein'] > 0:
        return 2  # Preeclampsia Risk
    elif row['UTI_flag'] == 1:
        return 1  # UTI
    else:
        return 0  # Normal

data['Label'] = data.apply(create_label, axis=1)

# Select relevant features for the model
features = ['pH', 'SG', 'Protein', 'Glucose', 'Ketones', 'Blood', 'Nitrites', 'Leukocyte Esterase', 'RBC', 'WBC', 'Bacteria', 'WBC_flag', 'Protein_flag', 'Diabetes_flag', 'UTI_flag']

# Handle missing values: drop rows with NaN in features or label
data = data.dropna(subset=features + ['Label'])

# Save preprocessed data to CSV (ready for model training)
data[features + ['Label']].to_csv('preprocessed_urinalysis_pregnancy.csv', index=False)

print("Preprocessing complete. Preprocessed file saved as 'preprocessed_urinalysis_pregnancy.csv'")