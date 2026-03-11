import pandas as pd
import matplotlib.pyplot as plt
import os

# ============================================
# ⚙️ إعدادات التصميم الاحترافي
# ============================================

# الألوان المهنية المناسبة للقطاع الطبي
COLORS = {
    'primary': '#2563eb',      # أزرق رئيسي
    'success': '#10b981',      # أخضر للطبيعي
    'warning': '#f59e0b',      # برتقالي للتحذير
    'danger': '#ef4444',       # أحمر للخطر
    'bg': '#f8fafc',           # خلفية فاتحة
    'grid': '#e2e8f0',         # شبكة خفيفة
    'text': '#1e293b',         # نص داكن
    'text_light': '#64748b'    # نص ثانوي
}

# القيم المرجعية من ملف Tests.docx
CONFIG = {
    'Hemoglobin (HB in Complete Blood Count)': {'name': 'Hemoglobin', 'low': 13.0, 'high': 17.0, 'unit': 'g/dL'},
    'WBC': {'name': 'WBC', 'low': 4000, 'high': 11000, 'unit': '/μL'},
    'Glucose (HbA1c)': {'name': 'Glucose (HbA1c)', 'low': 0, 'high': 5.7, 'unit': '%'},
    'Albumin (Protein in Urine)': {'name': 'Protein (Albumin)', 'low': 0, 'high': 0, 'unit': 'mg/dL'},
    'Glucose (Sugar in Urine)': {'name': 'Glucose (Sugar)', 'low': 0, 'high': 0, 'unit': 'mg/dL'},
    'TSH': {'name': 'TSH', 'low': 0.4, 'high': 4.0, 'unit': 'mIU/L'},
    'Iron (Ferritin)': {'name': 'Iron (Ferritin)', 'low': 22, 'high': 322, 'unit': 'ng/mL'}
}

# إعدادات الخطوط والمظهر العام
plt.rcParams.update({
    'font.family': 'sans-serif',
    'font.size': 10,
    'axes.titlesize': 13,
    'axes.labelsize': 10,
    'axes.facecolor': COLORS['bg'],
    'figure.facecolor': 'white',
    'figure.dpi': 150,
    'savefig.dpi': 300,
    'savefig.bbox': 'tight',
    'savefig.pad_inches': 0.1
})

def load_patient_data(file_path):
    """
    تحميل بيانات المريض من ملف الإكسل (صف واحد فقط)
    """
    if not os.path.exists(file_path):
        print(f"❌ File not found: {file_path}")
        return None
    return pd.read_excel(file_path)

def get_status_info(value, low, high):
    """
    تحديد حالة القيمة وإرجاع الألوان والنص المناسب
    """
    if value > high:
        return 'HIGH ⚠️', COLORS['danger'], 'critical'
    elif value < low:
        return 'LOW ⚠️', COLORS['danger'], 'critical'
    else:
        return 'Normal', COLORS['success'], 'normal'

def draw_professional_chart(config, value, patient_id='Patient_001'):
    """
    رسم مخطط احترافي منفرد لكل اختبار
    """
    status, color, severity = get_status_info(value, config['low'], config['high'])
    
    # إنشاء الشكل مع هوامش أنيقة
    fig, ax = plt.subplots(figsize=(10, 5))
    fig.patch.set_facecolor('white')
    
    # رسم العمود الرئيسي مع زوايا دائرة
    bar = ax.bar([config['name']], [value], 
                 color=color, 
                 edgecolor='white', 
                 linewidth=2,
                 width=0.5,
                 alpha=0.95,
                 zorder=3)
    
    # تظليل المنطقة الطبيعية بتدرج لوني ناعم
    ax.axhspan(config['low'], config['high'], 
               alpha=0.12, 
               color=COLORS['success'], 
               label='Normal Range',
               zorder=1)
    
    # خطوط الحدود المرجعية
    ax.axhline(config['low'], color=COLORS['text_light'], linestyle=':', linewidth=1.5, 
               label=f'Lower Limit: {config["low"]}', zorder=2)
    ax.axhline(config['high'], color=COLORS['text_light'], linestyle=':', linewidth=1.5,
               label=f'Upper Limit: {config["high"]}', zorder=2)
    
    # إضافة قيمة رقمية فوق العمود
    ax.text(0, value + (config['high'] * 0.08 if config['high'] > 0 else value * 0.1), 
            f'{value} {config["unit"]}', 
            ha='center', va='bottom', 
            fontsize=14, fontweight='bold', 
            color=COLORS['text'],
            bbox=dict(boxstyle='round,pad=0.4', facecolor='white', edgecolor=color, linewidth=2),
            zorder=4)
    
    # إعداد المحور العمودي (يبدأ من الصفر دائماً)
    y_max = max(value * 1.4, config['high'] * 1.3) if config['high'] > 0 else value * 1.4
    ax.set_ylim(0, y_max)
    
    # تنسيق المحاور والعناوين
    ax.set_ylabel(f'Value ({config["unit"]})', fontsize=10, color=COLORS['text_light'], fontweight='500')
    ax.set_xlabel('')
    ax.set_xticks([])  # إخفاء تسمية المحور السيني للتنظيف
    
    # العنوان الرئيسي مع شارة الحالة
    title_color = color if severity == 'critical' else COLORS['success']
    ax.set_title(f'{config["name"]}\n{status}', 
                 fontsize=14, fontweight='bold', 
                 color=title_color, pad=20)
    
    # شبكة خلفية أنيقة
    ax.grid(axis='y', linestyle='--', linewidth=0.5, alpha=0.4, color=COLORS['grid'], zorder=0)
    ax.set_axisbelow(True)
    
    # شريط جانبي ملون للإشارة للحالة
    ax.axvline(-0.3, ymin=0, ymax=1, color=color, linewidth=4, zorder=5)
    
    # شارة معلومات المريض في الزاوية
    ax.text(0.98, 0.98, f'👤 {patient_id}', 
            transform=ax.transAxes, 
            fontsize=9, 
            ha='right', va='top',
            bbox=dict(boxstyle='round,pad=0.3', facecolor=COLORS['bg'], edgecolor=COLORS['grid'], linewidth=1),
            color=COLORS['text_light'])
    
    # أسطورة أنيقة
    ax.legend(loc='upper right', fontsize=8, frameon=True, 
              facecolor='white', edgecolor=COLORS['grid'], 
              fancybox=True, shadow=False)
    
    # إزالة الحدود العلوية واليمنى للنظافة
    ax.spines['top'].set_visible(False)
    ax.spines['right'].set_visible(False)
    ax.spines['left'].set_color(COLORS['grid'])
    ax.spines['bottom'].set_color(COLORS['grid'])
    
    # حفظ الملف بجودة عالية
    filename = f"{config['name'].replace(' ', '_')}_chart.png"
    plt.savefig(filename, bbox_inches='tight', facecolor='white')
    
    # عرض الرسم
    plt.show()
    plt.close()
    
    return filename, status, severity

def print_summary_report(results):
    """
    طباعة تقرير ملخص احترافي للفريق الخلفي
    """
    print("\n" + "═"*70)
    print(" 📋 PATIENT LAB ANALYSIS REPORT")
    print(" 🗂️ Reference: Tests.docx | Data: prototypeAnalysis.xlsx")
    print("═"*70 + "\n")
    
    normal_count = sum(1 for r in results if r['severity'] == 'normal')
    critical_count = len(results) - normal_count
    
    print(f"📊 Total Tests: {len(results)}")
    print(f"✅ Normal: {normal_count}")
    print(f"🚨 Requires Attention: {critical_count}\n")
    
    if critical_count > 0:
        print("⚠️  CRITICAL ALERTS:")
        print("-"*70)
        for r in results:
            if r['severity'] == 'critical':
                print(f"  🔴 {r['name']}: {r['value']} {r['unit']} [{r['status']}]")
                print(f"     Reference: {r['low']} - {r['high']} {r['unit']}\n")
    
    print("💾 Files Generated:")
    for r in results:
        icon = "✅" if r['severity'] == 'normal' else "🔴"
        print(f"  {icon} {r['filename']}")


if __name__ == "__main__":
    print("🔬 Professional Lab Analysis - Backend Ready\n")
    
    # تحميل البيانات
    df = load_patient_data('prototypeAnalysis.xlsx')
    if df is None:
        exit()
    
    results = []
    
    # معالجة كل اختبار
    for col, cfg in CONFIG.items():
        if col in df.columns:
            val = df[col].iloc[0]
            if val != -1:  # تجاهل البيانات غير المستخرجة
                print(f"  📊 Processing: {cfg['name']}...")
                filename, status, severity = draw_professional_chart(cfg, val)
                
                results.append({
                    'name': cfg['name'],
                    'value': val,
                    'unit': cfg['unit'],
                    'low': cfg['low'],
                    'high': cfg['high'],
                    'status': status,
                    'severity': severity,
                    'filename': filename
                })
    
    # طباعة التقرير النهائي
    print_summary_report(results)
