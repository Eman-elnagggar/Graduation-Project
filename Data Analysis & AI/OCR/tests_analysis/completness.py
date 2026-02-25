import get_main_keys
def complete_data(test_name,values):
    numeric_key,string_key=get_main_keys.get_keys(test_name)
    result_keys=list(values.keys())
    if len(numeric_key)>0:
        for key in numeric_key:
            if key not in result_keys:
                values[key]=-1
    
    if len(string_key)>0:
        for key in string_key:
            if key not in result_keys:
                values[key]="Not Extracted"
    
    return values

# # try 1
# test_name_1='Urinalysis'
# # with random values just for try
# values_1={"Specific_Gravity":5, "Protein":"Nil", "Glucose":"Nil", "Nitrite":"Nil", "Ketones":"Nil",
#          "Blood":"Nil", "RBCs":"Nil", "Leukocytes":"Nil"}

# values_1=complete_data(test_name_1,values_1)
# print(values_1)

# print("="*50)

# # try 2
# test_name_2='HbA1c (Sugar Test)'
# # with random values just for try
# values_2={}

# values_2=complete_data(test_name_2,values_2)
# print(values_2)