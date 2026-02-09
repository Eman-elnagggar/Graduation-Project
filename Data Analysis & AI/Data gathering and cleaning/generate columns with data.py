import pandas as pd
import numpy as np

# 1. تحميل الداتاسيت (تعديل لقرائتها كـ CSV بناءً على الملف المرفق)
df = pd.read_excel('cbc_Dataset.xlsx')

# --- الخطوة الأهم: توحيد الوحدات ---
# نحول WBC من (12300) إلى (12.3) لكي تتوافق مع الـ Rules الطبية
df['WBC'] = df['WBC'] / 1000
# نحول PLATELETS من (404000) إلى (404)
df['PLATELETS'] = df['PLATELETS'] / 1000

# 2. تحويل الأعمدة الموجودة (LYMP, MONO) إلى Absolute بنفس الوحدة الجديدة (10³/µL)
df['ABS_LYMP'] = (df['LYMP'] * df['WBC']) / 100
df['ABS_MONO'] = (df['MONO'] * df['WBC']) / 100

# 3. دالة توليد الأعمدة الناقصة بناءً على التشخيص
def generate_abs_differential(row):
    wbc = row['WBC']
    diagnosis = str(row['Diagnosis']).lower()
    
    current_pct_sum = row['LYMP'] + row['MONO']
    remaining_pct = 100 - current_pct_sum
    if remaining_pct <= 0: remaining_pct = 10 
    
    # المنطق الطبي للنسب
    if 'viral' in diagnosis:
        neut_ratio = np.random.uniform(0.50, 0.65) # في الفيروسات يقل النيوتروفيل ويزيد الليمف
        eos_ratio = np.random.uniform(0.10, 0.20)
    elif 'infection' in diagnosis or wbc > 11: 
        neut_ratio = np.random.uniform(0.80, 0.90) # في البكتيريا يزيد النيوتروفيل
        eos_ratio = np.random.uniform(0.01, 0.05)
    else: 
        neut_ratio = np.random.uniform(0.65, 0.75)
        eos_ratio = np.random.uniform(0.05, 0.15)
    
    neut_pct = remaining_pct * neut_ratio
    remaining_after_neut = remaining_pct - neut_pct
    eos_pct = remaining_after_neut * 0.8
    baso_pct = remaining_after_neut - eos_pct
    
    # الناتج هنا سيكون بوحدة 10³/µL (مثلاً 7.5 بدلاً من 7500)
    abs_neut = (neut_pct * wbc) / 100
    abs_eos = (eos_pct * wbc) / 100
    abs_baso = (baso_pct * wbc) / 100
    
    return pd.Series([round(abs_neut, 2), round(abs_eos, 2), round(abs_baso, 2)])

# 4. تطبيق الدالة
df[['ABS_NEUT', 'ABS_EOS', 'ABS_BASO']] = df.apply(generate_abs_differential, axis=1)

# حفظ الملف الجديد (بصيغة CSV لسهولة التعامل)
df.to_excel('cbc_absolute_counts_ready.xlsx', index=False)

print("تم تعديل الوحدات وتوليد البيانات!")
print("وحدة WBC و Neutrophils الآن هي (10³/µL) لتناسب القواعد الطبية.")