from HbA1c import HbA1c_pipeline

def HbA1c_patient_alert(HbA1c_patient_data)->list:
    diagnose=HbA1c_pipeline.evaluate_HbA1c(HbA1c_patient_data)
    alert_message=[]
    if diagnose =="disease":
        alert_message.append("You have a slightly elevated blood sugar level. Try to maintain a healthy eating pattern until you see a doctor.")
    return alert_message
        