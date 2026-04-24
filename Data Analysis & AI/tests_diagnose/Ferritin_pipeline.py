
def evaluate_ferritin(input_data:dict)->list:
    value=input_data["Ferritin_value"]
    if value<15:
        return ["Iron Deficiency"]
    
    if (value>=15) & (value<=30):
        return ["Low Iron Stores"]
    
    if (value>=30) & (value<150):
        return ["Normal"]
    
    if value>150:
        return ["High Ferritin"]

# ferritin_value=17
# print(f"ferritin_diagnose:{ferritin_diagnose(ferritin_value)}")