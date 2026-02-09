def consistancy_data(test_name,values)->dict:
    compared_urine_keys=['Protein','Glucose','Nitrite','Ketones','Blood']
    compared_blood_group_keys=['RH_Factor']
    compared_hepatitis_B_keys=['HBsAg']
    compared_hepatitis_C_keys=['HCV']
    positive_values=['reactive','pos','+']
    negative_values=['non-reactive','neg','none','nil','-']
    
    compared_group=''
    if test_name=='Urinalysis':
        compared_group=compared_urine_keys
    elif test_name=='Blood Group':
        compared_group=compared_blood_group_keys
    elif test_name == 'HBsAg (Hepatitis B)':
        compared_group=compared_hepatitis_B_keys
    elif test_name =='HCV (Hepatitis C)':
        compared_group=compared_hepatitis_C_keys
    
    for key in compared_group:
        if values[key] in positive_values:
            values[key]='positive'
        elif values[key] in negative_values:
            values[key]='negative'
    
    return values

    