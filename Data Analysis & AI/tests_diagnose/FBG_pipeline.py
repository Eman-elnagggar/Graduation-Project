
def evaluate_FBG(input_data:dict)->list:
    value=input_data['FBG']
    if value<100:
        return ["Normal"]
    elif (value>=100) & (value<126):
        return "Prediabetes"
    elif value>=126:
        return ["Diabetes"]

# if __name__=="__main__":
#     input_data={
#         'FBG':80
#     }
#     diagnose=evaluate_FBG(input_data)
#     print(f"Diagnose:{diagnose}")