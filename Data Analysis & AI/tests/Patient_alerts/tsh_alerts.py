from TSH import tsh_pipeline

def tsh_patient_alert(tsh_patient_data)->list:
    diagnose=tsh_pipeline.evaluate_tsh(tsh_patient_data)
    alert_message=[]
    if diagnose =="disease":
        alert_message.append("The thyroid hormone may appear to be imbalanced; urgent consultation with a doctor is advised.")

    return alert_message