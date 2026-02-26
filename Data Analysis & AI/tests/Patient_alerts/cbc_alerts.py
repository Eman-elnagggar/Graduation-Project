from CBC.cbc_pipeline import cbc_model_prediction

def cbc_patient_alert(cbc_patient_data):
    patient_diagnoses_alerts=['Anemia','Leukopenia']
    cbc_obj=cbc_model_prediction()
    diagnose=cbc_obj.predicit_cbc(cbc_model_prediction)
    alert_message=""
    if patient_diagnoses_alerts[0] in diagnose:
        alert_message="You may have some mild anemia, so consult your doctor and make sure you eat healthy foods with the necessary vitamins."
    elif patient_diagnoses_alerts[1] in diagnose:
        alert_message="It seems that you may have a weakened immune system, so consult a doctor and eat some healthy immune-boosting foods."
    
    return alert_message
        
    
    
