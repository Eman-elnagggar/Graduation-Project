
def evaluate_HbA1c(input_data:float)->str:
    value=input_data['HbA1c']
    if value<5.7:
        return "normal"
    elif (value>=5.7) & (value<=6.4):
        return "normal"
    elif value>6.4:
        return "disease"

if __name__=="__main__":
    input_data={
        'HbA1c':4.5
    }
    diagnose=evaluate_HbA1c(input_data)
    print(f"Diagnose:{diagnose}")