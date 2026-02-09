
def evaluate_HbA1c(value:float)->str:
    if value<5.7:
        return "normal"
    elif (value>=5.7) & (value<=6.4):
        return "No diabetes or pre-diabetes."
    elif value>6.4:
        return "Indicates a diagnosis of diabetes."

HbA1c=5.7
diagnose=evaluate_HbA1c(HbA1c)
print(f"Diagnose:{diagnose}")