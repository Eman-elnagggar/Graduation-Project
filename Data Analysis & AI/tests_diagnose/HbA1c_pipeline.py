
def evaluate_HbA1c(input_data:dict)->tuple[dict, str]:
    test_name="HbA1c (Sugar Test)"
    value=input_data['HbA1c']
    if value<5.7:
        return {"test_name": test_name,
                "HbA1c": ["Normal", f"The value {value} is within the normal range (less than 5.7%)"]}, "For Nabd Standereds, Your HbA1c level is normal. Maintain a healthy lifestyle to keep it in this range."
    elif (value>=5.7) & (value<6.5):
        return {"test_name": test_name,
                "HbA1c": ["Prediabetes", f"The value {value} is in the prediabetes range (5.7% - 6.4%)"]}, "For Nabd Standereds, You have prediabetes. Consider making lifestyle changes to prevent progression to diabetes."
    elif value>=6.5:
        return {"test_name": test_name,
                "HbA1c": ["Diabetes", f"The value {value} is in the diabetes range (6.5% or higher)"]}, "For Nabd Standereds, You diagnosed with diabetes. Seek medical advice for management and treatment."

# if __name__=="__main__":
#     input_data={
#         'HbA1c':4.5
#     }
#     diagnose=evaluate_HbA1c(input_data)
#     print(f"Diagnose:{diagnose}")