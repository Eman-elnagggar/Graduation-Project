
def evaluate_HbA1c(input_data:dict)->list:
    value=input_data['HbA1c']
    if value<5.7:
        return ["Normal"]
    elif (value>=5.7) & (value<6.5):
        return ["Prediabetes"]
    elif value>=6.5:
        return ["Diabetes"]

# if __name__=="__main__":
#     input_data={
#         'HbA1c':4.5
#     }
#     diagnose=evaluate_HbA1c(input_data)
#     print(f"Diagnose:{diagnose}")