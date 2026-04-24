def alerts(test_name:str,diagnose:list)->str:
    if test_name=='Urinalysis':
        pass
    elif test_name=='CBC (Complete Blood Count)':
        pass
    elif test_name=='HbA1c (Sugar Test)':
        if diagnose[0]=="Prediabetes":
            return "Prediabetes detected. Consider lifestyle changes and regular monitoring."
        elif diagnose[0]=="Diabetes":
            return "Diabetes detected. Seek medical advice for management and treatment."
    elif test_name=='Ferritin':
        if diagnose[0]=="Iron Deficiency":
            return "Iron deficiency detected. Consider iron supplementation and dietary changes."
        elif diagnose[0]=="Low Iron Stores":
            return "Low iron stores detected. Monitor iron levels and consider dietary adjustments."
        elif diagnose[0]=="High Ferritin":
            return "High ferritin levels detected. Consult a healthcare provider for further evaluation."
    elif test_name=='TSH (Thyroid)':
        if diagnose[0]=="Abnormal":
            return "Abnormal TSH levels detected. Consult a healthcare provider for thyroid function evaluation."
    elif test_name=='Fasting Blood Glucose':
        if diagnose[0]=="Prediabetes":
            return "Prediabetes detected. Consider lifestyle changes and regular monitoring."
        elif diagnose[0]=="Diabetes":
            return "Diabetes detected. Seek medical advice for management and treatment."
    elif test_name=='HBsAg (Hepatitis B)':
        if diagnose==True:
            return "Hepatitis B infection detected. Seek medical advice for management and treatment."
    elif test_name=='HCV (Hepatitis C)':
        if diagnose==True:
            return "Hepatitis C infection detected. Seek medical advice for management and treatment."