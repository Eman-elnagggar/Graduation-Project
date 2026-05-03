
def evaluate_tsh(input_data:dict, trimester:int)->tuple[dict, str]:
    test_name='TSH (Thyroid)'
    tsh_value=input_data['TSH']
    if (trimester==1):
        if ((tsh_value>=0.6)&(tsh_value<=3.4)):
            return {'test_name': test_name,
                    'TSH': ["Normal Thyroid Function", f"The Value {tsh_value} is within the normal range 0.6-3.4 mIU/L for the first trimester."]}, "For Nabd Standereds, Your TSH level is within the normal range for the first trimester. Continue maintaining a healthy lifestyle and consult with your healthcare provider for regular check-ups."
        else :
            return {'test_name': test_name,
                    'TSH': ["Abnormal Thyroid Function", f"The Value {tsh_value} is outside the normal range 0.6-3.4 mIU/L for the first trimester."]}, "For Nabd Standereds, Your TSH level is outside the normal range for the first trimester. It's important to consult with your healthcare provider for further evaluation and potential treatment options."

    if (trimester==2):
        if ((tsh_value>=0.37)&(tsh_value<=3.6)):
            return {'test_name': test_name,
                    'TSH': ["Normal Thyroid Function", f"The Value {tsh_value} is within the normal range 0.37-3.6 mIU/L for the second trimester."]}, "For Nabd Standereds, Your TSH level is within the normal range for the second trimester. Continue maintaining a healthy lifestyle and consult with your healthcare provider for regular check-ups."
        else :
            return {'test_name': test_name,
                    'TSH': ["Abnormal Thyroid Function", f"The Value {tsh_value} is outside the normal range 0.37-3.6 mIU/L for the second trimester."]}, "For Nabd Standereds, Your TSH level is outside the normal range for the second trimester. It's important to consult with your healthcare provider for further evaluation and potential treatment options."
    
    if (trimester==3):
        if ((tsh_value>=0.38)&(tsh_value<=4.0)):
            return {'test_name': test_name,
                    'TSH': ["Normal Thyroid Function", f"The Value {tsh_value} is within the normal range 0.38-4.0 mIU/L for the third trimester."]}, "For Nabd Standereds, Your TSH level is within the normal range for the third trimester. Continue maintaining a healthy lifestyle and consult with your healthcare provider for regular check-ups."
        else :
            return {'test_name': test_name,
                    'TSH': ["Abnormal Thyroid Function", f"The Value {tsh_value} is outside the normal range 0.38-4.0 mIU/L for the third trimester."]}, "For Nabd Standereds, Your TSH level is outside the normal range for the third trimester. It's important to consult with your healthcare provider for further evaluation and potential treatment options."
