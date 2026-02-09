def ferritin_diagnose(value:float)->str:
    if value<13:
        return "High risk of anemia; requires immediate medical intervention."
    
    if (value>=13) & (value<=29):
        return "Depleted stores; iron supplements are usually recommended."
    
    if (value>=15) & (value<=150):
        return "Adequate iron stores for both mother and fetus."
    
    if value>150:
        return "May indicate inflammation or other metabolic issues in diabetics."
    

ferritin_value=17
print(f"ferritin_diagnose:{ferritin_diagnose(ferritin_value)}")