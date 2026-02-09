import pandas as pd
import numpy as np
import random
import re

# Month to number mapping for parsing ranges like '1-Mar' -> 1-3
month_map = {
    'JAN': 1, 'FEB': 2, 'MAR': 3, 'APR': 4, 'MAY': 5, 'JUN': 6,
    'JUL': 7, 'AUG': 8, 'SEP': 9, 'OCT': 10, 'NOV': 11, 'DEC': 12
}

def parse_cell_count(s):
    """
    Parse cell count strings like '0-2', '1-Mar', 'LOADED', '>100', etc. to a numeric value.
    Handles common data entry errors like trailing months (e.g., '0-2 FEB') by cleaning them.
    """
    if pd.isna(s):
        return 0
    
    s = str(s).strip().upper()
    
    # Remove common units / trailing text (e.g., /HPF, /FIELD)
    s = re.sub(r'\s*(/\s*HPF|HPF|/HPE|/FIELD|\s*RBCS?|\s*WBCS?|\s*CELLS?).*$', '', s, flags=re.IGNORECASE).strip()
    
    # Qualitative mappings
    if s in ['NONE SEEN', 'RARE', 'NOT SEEN', 'NIL', 'NEGATIVE', 'NEG', '0']: 
        return 0
    if s in ['OCCASIONAL', 'FEW']: 
        return 3
    if s == 'MODERATE': 
        return 10
    if s in ['PLENTY', 'LOADED', 'TNTC']: 
        return 100
    
    # Greater than: >100 -> 150
    if s.startswith('>'):
        try:
            return int(re.sub(r'\D', '', s[1:])) + 50
        except ValueError:
            return 0
    
    # Range: '0-2', '1-Mar', '6-8 FEB' (clean to '6-8')
    if '-' in s:
        parts = s.split('-')
        if len(parts) != 2:
            return 0
        low_str = parts[0].strip().upper()
        high_str = parts[1].strip().upper()
        
        # Remove trailing non-digits from high_str (e.g., '2 FEB' -> '2')
        high_str = re.sub(r'[^0-9]+$', '', high_str).strip()
        
        try:
            low = month_map.get(low_str, int(low_str))
            high = month_map.get(high_str, int(high_str))
            return (low + high) / 2
        except ValueError:
            return 0
    
    # Single number
    try:
        return int(s)
    except ValueError:
        return 0

# Load the CSV file
df = pd.read_csv('new2.csv')

# For pregnancy-focused model: Optionally filter to female patients (assuming pregnancy context)
# Uncomment if you want to process only females:
# df = df[df['Gender'].str.strip().str.upper() == 'FEMALE']

# Seed for reproducibility
np.random.seed(42)
random.seed(42)

# Generate Ketones: Adjusted for pregnancy (more likely mild ketonuria even if glucose negative)
def generate_ketones(glucose):
    glucose = str(glucose).strip().upper()
    if glucose in ['TRACE', '1+', '2+', '3+', '4+']:
        # Higher chance of ketones when glycosuria present (common in pregnancy)
        return np.random.choice(
            ['NEGATIVE', 'TRACE', '1+', '2+', '3+'],
            p=[0.50, 0.20, 0.15, 0.10, 0.05]
        )
    else:
        # Even when glucose negative — mild ketonuria can occur in pregnancy
        return np.random.choice(
            ['NEGATIVE', 'TRACE'],
            p=[0.92, 0.08]
        )

df['Ketones'] = df['Glucose'].apply(generate_ketones)

# Generate Blood (Hemoglobin): Adjusted for pregnancy (microscopic hematuria more common)
def generate_blood(rbc):
    num_rbc = parse_cell_count(rbc)
    if num_rbc <= 3:
        return 'NEGATIVE'
    elif num_rbc <= 6:
        return 'TRACE'
    elif num_rbc < 12:
        return '1+'
    elif num_rbc < 25:
        return '2+'
    else:
        return '3+'

df['Blood (Hemoglobin)'] = df['RBC'].apply(generate_blood)

# Generate Nitrites: Adjusted for pregnancy (lower threshold due to higher UTI risk)
def generate_nitrites(diagnosis, bacteria):
    diagnosis = str(diagnosis).strip().upper()
    bac_level = parse_cell_count(bacteria)
    if diagnosis == 'POSITIVE' or bac_level >= 5:
        return 'POSITIVE'
    else:
        return 'NEGATIVE'

df['Nitrites'] = df.apply(lambda row: generate_nitrites(row['Diagnosis'], row['Bacteria']), axis=1)

# Generate Leukocyte Esterase: Adjusted for pregnancy (higher suspicion for even low WBC)
def generate_leukocyte_esterase(wbc):
    num_wbc = parse_cell_count(wbc)
    if num_wbc < 5:
        return 'NEGATIVE'
    elif num_wbc < 10:
        return 'TRACE'
    elif num_wbc < 20:
        return '1+'
    elif num_wbc < 50:
        return '2+'
    else:
        return '3+'

df['Leukocyte Esterase'] = df['WBC'].apply(generate_leukocyte_esterase)

# Generate Bilirubin: Slightly increased chance in pregnancy (keep mild adjustment)
def generate_bilirubin(color):
    color = str(color).strip().upper()
    if color in ['DARK YELLOW', 'BROWN', 'AMBER']:
        return np.random.choice(['NEGATIVE', '1+', '2+'], p=[0.75, 0.18, 0.07])
    else:
        return np.random.choice(['NEGATIVE', '1+'], p=[0.97, 0.03])

df['Bilirubin'] = df['Color'].apply(generate_bilirubin)

# Generate Urobilinogen: Mostly NORMAL, sometimes increased if Bilirubin positive
def generate_urobilinogen(bilirubin):
    bilirubin = str(bilirubin).strip().upper()
    if bilirubin != 'NEGATIVE':
        return np.random.choice(['NORMAL', '2', '4', '8'], p=[0.5, 0.2, 0.2, 0.1])  # Units EU/dL or similar
    else:
        return 'NORMAL'

df['Urobilinogen'] = df['Bilirubin'].apply(generate_urobilinogen)

# Generate Casts: Adjusted for pregnancy (hyaline casts more common with trace protein)
def generate_casts(protein, wbc, rbc):
    protein = str(protein).strip().upper()
    num_wbc = parse_cell_count(wbc)
    num_rbc = parse_cell_count(rbc)
    if protein in ['TRACE', '1+'] or num_wbc > 8 or num_rbc > 3:
        return np.random.choice(
            ['NONE SEEN', 'HYALINE CASTS RARE', 'HYALINE CASTS FEW', 'GRANULAR CASTS RARE'],
            p=[0.35, 0.30, 0.25, 0.10]
        )
    else:
        return 'NONE SEEN'

df['Casts'] = df.apply(lambda row: generate_casts(row['Protein'], row['WBC'], row['RBC']), axis=1)

# Save the updated DataFrame to a new CSV
df.to_csv('updated_urinalysis_pregnancy.csv', index=False)

# Print the first few rows to verify
print(df.head())