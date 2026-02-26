
def evaluate_tsh(input_data:dict,trimester:int)->str:
    tsh_value=input_data['TSH']

    if (trimester==1):
        if ((tsh_value>=0.6)&(tsh_value<=3.4)):
            return "normal"
        else :
            return "disease"
    
    if (trimester==2):
        if ((tsh_value>=0.37)&(tsh_value<=3.6)):
            return "normal"
        else :
            return "disease"
    
    if (trimester==3):
        if ((tsh_value>=0.38)&(tsh_value<=4.0)):
            return "normal"
        else :
            return "disease"
