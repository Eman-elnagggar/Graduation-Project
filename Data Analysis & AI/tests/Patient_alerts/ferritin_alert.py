from Ferritin import Ferritin_pipeline

def ferritin_patient_alert(ferritin_patient_data):
    diagnose=Ferritin_pipeline.evaluate_ferritin(ferritin_patient_data)
    alert_message=""
    if diagnose =="Iron Deficiency":
        alert_message="You have a severe iron deficiency that may lead to anemia. Please see a doctor as soon as possible and also eat foods that are high in iron."
    elif diagnose =="Low Iron Stores":
        alert_message="You have good iron levels, but they may not be ideal for you and your baby, so it's recommended to include foods with higher iron content in your diet."
    elif diagnose =="High Ferritin":
        alert_message ="This percentage is abnormal. Go to your doctor or the nearest hospital."
        
    return alert_message
        