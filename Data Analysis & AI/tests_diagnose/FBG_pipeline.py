
def evaluate_FBG(input_data:dict)->tuple[dict, str]:
    value=input_data['FBG']
    if value<100:
        return {"test_name": "Fasting Blood Glucose",
                "FBG": ["Normal",f"The value {value} is within the normal range of less than 100 mg/dL"]},"For Nabd Standereds, your fasting blood glucose level is considered normal. It's important to maintain a healthy lifestyle, including a balanced diet and regular physical activity, to keep your blood sugar levels in check during pregnancy."
    elif (value>=100) & (value<126):
        return {"test_name": "Fasting Blood Glucose",
                "FBG": ["Prediabetes",f"The value {value} is in the prediabetes range (100-125 mg/dL)."]},"For Nabd Standereds, your fasting blood glucose level is in the prediabetes range. It's important to make lifestyle changes and consult with your healthcare provider to prevent the progression to diabetes."
    elif value>=126:
        return {"test_name": "Fasting Blood Glucose",
                "FBG": ["Diabetes",f"The value {value} is in the diabetes range (126 mg/dL and above)."]},"For Nabd Standereds, your fasting blood glucose level is in the diabetes range. It's important to work closely with your healthcare provider to manage your condition and prevent complications."
# if __name__=="__main__":
#     input_data={
#         'FBG':80
#     }
#     diagnose=evaluate_FBG(input_data)
#     print(f"Diagnose:{diagnose}")