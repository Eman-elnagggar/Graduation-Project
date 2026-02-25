def get_keys(test_name)->list:
    """
    Docstring for get_keys
    
    :param test_name: test name
    :param values: OCR dictionary 
    :return: two lists one for numerical keys and others for string keys and that for all test
    :rtype: list
    """
    if test_name=='Blood Group':
        return [],['ABO_Group','RH_Factor']
    
    if test_name=='CBC (Complete Blood Count)':
        return ["HB","RBCs_Count","MCV", "MCH", "MCHC", "WBC", "lymphocytes", "platelet_count"],[]
    
    if test_name=='HbA1c (Sugar Test)':
        return ["HbA1c"],[]
    
    if test_name=="Urinalysis":
        return ['PH','Specific_Gravity'],["Color","Protein", "Glucose", "Nitrite", "Ketones", "Blood", "RBCs", "Leukocytes"]
    
    if test_name=='HBsAg (Hepatitis B)':
        return [],['HBsAg']
    
    if test_name=='HCV (Hepatitis C)':
        return [],['HCV']
    
    if test_name=='TSH (Thyroid)':
        return ['TSH'],['TSH_Unit']
    
    if test_name=='Ferritin':
        return ['Ferritin_value'],[]
    
