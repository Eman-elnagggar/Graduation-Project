import pandas as pd
import matplotlib.pyplot as plt
import os
from matplotlib.patches import Rectangle

# ============================================
# ⚙️ إعدادات التصميم الاحترافي
# ============================================
COLORS = {
    'primary': '#2563eb',
    'success': '#10b981',
    'warning': '#f59e0b',
    'danger': '#ef4444',
    'bg': '#f8fafc',
    'grid': '#e2e8f0',
    'text': '#1e293b',
    'text_light': '#64748b'
}

CONFIG = {
    'Hemoglobin (HB in Complete Blood Count)': {'name': 'Hemoglobin', 'low': 13.0, 'high': 17.0, 'unit': 'g/dL'},
    'WBC': {'name': 'WBC', 'low': 4000, 'high': 11000, 'unit': '/μL'},
    'Glucose (HbA1c)': {'name': 'Glucose (HbA1c)', 'low': 0, 'high': 5.7, 'unit': '%'},
    'Albumin (Protein in Urine)': {'name': 'Protein (Albumin)', 'low': 0, 'high': 0, 'unit': 'mg/dL'},
    'Glucose (Sugar in Urine)': {'name': 'Glucose (Sugar)', 'low': 0, 'high': 0, 'unit': 'mg/dL'},
    'TSH': {'name': 'TSH', 'low': 0.4, 'high': 4.0, 'unit': 'mIU/L'},
    'Iron (Ferritin)': {'name': 'Iron (Ferritin)', 'low': 22, 'high': 322, 'unit': 'ng/mL'}
}

plt.rcParams.update({
    'font.family': 'sans-serif',
    'font.size': 10,
    'axes.titlesize': 16,
    'axes.labelsize': 11,
    'axes.facecolor': COLORS['bg'],
    'figure.facecolor': 'white',
    'figure.dpi': 150,
    'savefig.dpi': 300,
    'savefig.bbox': 'tight',
    'savefig.pad_inches': 0.1
})

def load_patient_data(file_path):
    if not os.path.exists(file_path):
        print(f"❌ File not found: {file_path}")
        return None
    return pd.read_excel(file_path)

def get_status_info(value, low, high):
    if high == 0 and value == 0:
        return 'Normal', COLORS['success'], 'normal'
    elif value > high:
        return 'HIGH ⚠️', COLORS['danger'], 'critical'
    elif value < low:
        return 'LOW ⚠️', COLORS['danger'], 'critical'
    else:
        return 'Normal', COLORS['success'], 'normal'

def draw_combined_professional_chart(config, values_dict, patient_id='Patient_001'):
    """
    رسم مخطط موحد احترافي لجميع الاختبارات
    """
    test_names = []
    test_values = []
    test_colors = []
    test_status = []
    test_units = []
    test_ranges = []
    
    normal_count = 0
    critical_count = 0
    
    for col, cfg in config.items():
        if col in values_dict:
            val = values_dict[col]
            if val != -1:
                status, color, severity = get_status_info(val, cfg['low'], cfg['high'])
                test_names.append(cfg['name'])
                test_values.append(val)
                test_colors.append(color)
                test_status.append(status)
                test_units.append(cfg['unit'])
                test_ranges.append((cfg['low'], cfg['high']))
                
                if severity == 'normal':
                    normal_count += 1
                else:
                    critical_count += 1
    
    # إنشاء الشكل الرئيسي
    fig, ax = plt.subplots(figsize=(14, 8))
    fig.patch.set_facecolor('white')
    
    # إنشاء الأعمدة الأفقية
    y_pos = range(len(test_names))
    bars = ax.barh(y_pos, test_values, color=test_colors, edgecolor='white',
                   linewidth=1.5, alpha=0.9, zorder=3, height=0.6)
    
    # تظليل النطاقات المرجعية لكل اختبار
    for i, (low, high) in enumerate(test_ranges):
        if high > 0:
            ax.axhspan(i - 0.4, i + 0.4, alpha=0.08, color=COLORS['success'], zorder=1)
            ax.plot([low, high], [i, i], color=COLORS['success'], linestyle='--', 
                   linewidth=2, alpha=0.6, zorder=2)
    
    # إضافة قيم رقمية على الأعمدة
    for i, (val, unit) in enumerate(zip(test_values, test_units)):
        ax.text(val + (max(test_values) * 0.02), i, f'{val} {unit}',
                va='center', fontsize=10, fontweight='bold',
                color=COLORS['text'], zorder=4)
    
    # إضافة حالة كل اختبار
    for i, status in enumerate(test_status):
        ax.text(-max(test_values) * 0.15, i, status,
                va='center', fontsize=9, fontweight='500',
                color=COLORS['text_light'], ha='right', zorder=4)
    
    # تنسيق المحاور
    ax.set_yticks(y_pos)
    ax.set_yticklabels(test_names, fontsize=10, fontweight='500', color=COLORS['text'])
    ax.set_xlabel('Value', fontsize=11, fontweight='600', color=COLORS['text_light'])
    ax.set_xlim(left=-max(test_values) * 0.2)
    
    # العنوان الرئيسي
    ax.set_title('🔬 COMPREHENSIVE LAB ANALYSIS REPORT',
                 fontsize=16, fontweight='bold', color=COLORS['text'], pad=20)
    
    # شبكة خلفية
    ax.grid(axis='x', linestyle='--', linewidth=0.5, alpha=0.4, color=COLORS['grid'], zorder=0)
    ax.set_axisbelow(True)
    
    # شريط ملون جانبي للحالة
    ax.axvline(0, ymin=0, ymax=1, color=COLORS['primary'], linewidth=3, zorder=5)
    
    # صندوق ملخص الإحصائيات
    summary_text = f'✅ Normal: {normal_count}  |  🚨 Critical: {critical_count}  |  📊 Total: {len(test_names)}'
    ax.text(0.5, 1.02, summary_text, transform=ax.transAxes,
            fontsize=11, fontweight='600', ha='center', va='bottom',
            bbox=dict(boxstyle='round,pad=0.5', facecolor=COLORS['bg'],
                     edgecolor=COLORS['grid'], linewidth=1),
            color=COLORS['text'])
    
    # معلومات المريض
    ax.text(0.98, 0.98, f'👤 {patient_id}', transform=ax.transAxes,
            fontsize=10, ha='right', va='top',
            bbox=dict(boxstyle='round,pad=0.4', facecolor=COLORS['bg'],
                     edgecolor=COLORS['grid'], linewidth=1),
            color=COLORS['text_light'])
    
    # أسطورة الألوان
    legend_elements = [
        Rectangle((0, 0), 1, 1, facecolor=COLORS['success'], alpha=0.9, label='Normal'),
        Rectangle((0, 0), 1, 1, facecolor=COLORS['danger'], alpha=0.9, label='Critical')
    ]
    ax.legend(handles=legend_elements, loc='lower right', fontsize=9,
              frameon=True, facecolor='white', edgecolor=COLORS['grid'],
              fancybox=True, shadow=False)
    
    # إزالة الحدود العلوية واليمنى
    ax.spines['top'].set_visible(False)
    ax.spines['right'].set_visible(False)
    ax.spines['left'].set_color(COLORS['grid'])
    ax.spines['bottom'].set_color(COLORS['grid'])
    
    # حفظ الملف
    filename = 'First of many.png'
    plt.savefig(filename, bbox_inches='tight', facecolor='white', dpi=300)
    
    plt.show()
    plt.close()
    
    return filename, normal_count, critical_count

def print_summary_report(results, filename):
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
    print(f"  📁 {filename}")

if __name__ == "__main__":
    print("🔬 Professional Lab Analysis - Backend Ready\n")
    
    df = load_patient_data('prototypeAnalysis.xlsx')
    if df is None:
        exit()
    
    values_dict = {}
    results = []
    
    for col, cfg in CONFIG.items():
        if col in df.columns:
            val = df[col].iloc[0]
            if val != -1:
                print(f"  📊 Processing: {cfg['name']}...")
                values_dict[col] = val
                
                status, color, severity = get_status_info(val, cfg['low'], cfg['high'])
                results.append({
                    'name': cfg['name'],
                    'value': val,
                    'unit': cfg['unit'],
                    'low': cfg['low'],
                    'high': cfg['high'],
                    'status': status,
                    'severity': severity
                })
    
    # رسم المخطط الموحد
    filename, normal_count, critical_count = draw_combined_professional_chart(CONFIG, values_dict)
    
    # طباعة التقرير النهائي
    print_summary_report(results, filename)