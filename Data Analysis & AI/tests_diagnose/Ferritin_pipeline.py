
def evaluate_ferritin(input_data:dict)->tuple[dict, str]:
    test_name='Ferritin'
    value=input_data["Ferritin_value"]
    if value<15:
        return {'test_name': test_name,
                'Ferritin_value': ["Iron Deficiency", f"The value {value} is below the normal range 15-150 ng/mL, suggesting Iron Deficiency."]}, "For Nabd Standereds, Your ferritin level is low, which may indicate iron deficiency. It's important to consult with your healthcare provider for further evaluation and potential treatment options."

    if (value>=15) & (value<=30):
        return {'test_name': test_name,
                'Ferritin_value': ["Low Iron Stores", f"The value {value} is below the normal range 15-150 ng/mL, suggesting Low Iron Stores."]}, "For Nabd Standereds, Your ferritin level is on the lower end of the normal range, which may indicate low iron stores. It's advisable to consult with your healthcare provider for further evaluation and potential dietary adjustments or supplementation."

    if (value>=30) & (value<150):
        return {'test_name': test_name,
                'Ferritin_value': ["Normal", f"The value {value} is within the normal range 15-150 ng/mL, indicating Normal ferritin levels."]}, "For Nabd Standereds, Your ferritin level is within the normal range, which suggests that your iron stores are adequate. Continue maintaining a balanced diet and consult with your healthcare provider for regular check-ups."

    if value>150:
        return {'test_name': test_name,
                'Ferritin_value': ["High Ferritin", f"The value {value} is above the normal range 15-150 ng/mL, suggesting High Ferritin."]}, "For Nabd Standereds, Your ferritin level is elevated, which may indicate iron overload. It's important to consult with your healthcare provider for further evaluation and potential treatment options."

# ferritin_value=17
# print(f"ferritin_diagnose:{ferritin_diagnose(ferritin_value)}")