
def evaluate_tsh(input_data:dict, trimester:int)->list:
    tsh_value=input_data['TSH']
    if (trimester==1):
        if ((tsh_value>=0.6)&(tsh_value<=3.4)):
            return ["Normal"]
        else :
            return ["Abnormal"]
    
    if (trimester==2):
        if ((tsh_value>=0.37)&(tsh_value<=3.6)):
            return ["Normal"]
        else :
            return ["Abnormal"]
    
    if (trimester==3):
        if ((tsh_value>=0.38)&(tsh_value<=4.0)):
            return ["Normal"]
        else :
            return ["Abnormal"]
