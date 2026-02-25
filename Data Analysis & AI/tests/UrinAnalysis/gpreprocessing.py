import pandas as pd
import numpy as np
import re
from sklearn.preprocessing import LabelEncoder, StandardScaler
import joblib

# 1. تحميل الداتا الأصلية
df = pd.read_csv('updated_urinalysis_pregnancy.csv')

# 2. تنظيف أسماء الأعمدة (تحويلها لكبيرة لضمان عدم حدوث خطأ في المناداة)
df.columns = [c.strip().upper() for c in df.columns]

# 3. خطوة الحسم: حذف الرجال تماماً (Male) والإبقاء على النساء (Female)
# استخدمنا دالة لضمان حذف أي صف لا يحتوي على كلمة FEMALE بدقة
if 'GENDER' in df.columns:
    # تحويل العمود لنصوص كبيرة ومسح المسافات
    df['GENDER'] = df['GENDER'].astype(str).str.strip().str.upper()
    # الفلترة: سنبقي فقط على الصفوف التي تساوي 'FEMALE'
    df = df[df['GENDER'] == 'FEMALE']
    # حذف العمود لأنه أصبح غير ضروري (كل الداتا الآن نساء)
    df = df.drop(columns=['GENDER'])

# 4. دالة معالجة أخطاء إكسيل (تحويل 1-Mar و 5-Aug إلى أرقام طبية منطقية)
def fix_medical_values(val):
    val = str(val).upper().strip()
    if val in ['NEG', 'NEGATIVE', 'NONE', 'NORMAL', 'NONE SEEN', '0', '0.0']: return 0.0
    if 'LOADED' in val: return 50.0
    
    # خريطة لأشهر إكسيل
    month_map = {'JAN': 1, 'FEB': 2, 'MAR': 3, 'APR': 4, 'MAY': 5, 'JUN': 6, 
                 'JUL': 7, 'AUG': 8, 'SEP': 9, 'OCT': 10, 'NOV': 11, 'DEC': 12}
    
    for month_str, month_val in month_map.items():
        if month_str in val:
            nums = re.findall(r'\d+', val)
            if nums:
                return (int(nums[0]) + month_val) / 2
    
    # استخراج الأرقام إذا كان نطاق طبيعي مثل 2-5
    nums = re.findall(r'\d+', val)
    if len(nums) >= 2:
        return (int(nums[0]) + int(nums[1])) / 2
    elif len(nums) == 1:
        return float(nums[0])
    return 0.0

# تطبيق الدالة على WBC و RBC
df['WBC'] = df['WBC'].apply(fix_medical_values)
df['RBC'] = df['RBC'].apply(fix_medical_values)

# 5. التشفير الترتيبي (Ordinal Mapping) للنتائج الكيميائية (مهم جداً للحمل)
dipstick_mapping = {
    'NEGATIVE': 0, 'NEG': 0, 'NORMAL': 0, 'NONE': 0,
    'TRACE': 1,
    '1+': 2, 'POSITIVE': 2,
    '2+': 3,
    '3+': 4
}

chemical_cols = ['GLUCOSE', 'PROTEIN', 'KETONES', 'BLOOD (HEMOGLOBIN)', 
                 'NITRITES', 'LEUKOCYTE ESTERASE', 'BILIRUBIN', 'UROBILINOGEN']

for col in chemical_cols:
    if col in df.columns:
        df[col] = df[col].astype(str).str.strip().str.upper().map(dipstick_mapping).fillna(0)

# 6. التشفير النصي (Label Encoding) للأعمدة الوصفية الأخرى
categorical_cols = ['COLOR', 'CLARITY/TURBIDITY', 'EPITHELIAL CELLS', 'CRYSTALS', 'BACTERIA', 'CASTS']
encoders = {}
for col in categorical_cols:
    if col in df.columns:
        le = LabelEncoder()
        df[col] = le.fit_transform(df[col].astype(str))
        encoders[col] = le

# 7. تشفير عمود التشخيص (Target)
if 'DIAGNOSIS' in df.columns:
    target_le = LabelEncoder()
    df['DIAGNOSIS'] = target_le.fit_transform(df['DIAGNOSIS'].astype(str))
    joblib.dump(target_le, 'pregnancy_final_target_encoder.pkl')

# 8. تسوية القيم الرقمية (StandardScaler)
scaler = StandardScaler()
numeric_features = ['PH', 'SPECIFIC GRAVITY', 'WBC', 'RBC']
df[numeric_features] = scaler.fit_transform(df[numeric_features])

# 9. حفظ الملف النهائي النظيف تماماً
df.to_csv('final_pregnancy_urinalysis_ready.csv', index=False)

# 10. حفظ أدوات المعالجة لاستخدامها في التطبيق
joblib.dump(scaler, 'final_pregnancy_scaler.pkl')
joblib.dump(encoders, 'final_pregnancy_encoders.pkl')


print(f"{len(df)} ")
print(f"final_pregnancy_urinalysis_ready.csv")