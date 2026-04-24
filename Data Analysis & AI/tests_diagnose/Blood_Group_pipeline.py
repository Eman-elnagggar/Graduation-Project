
def evaluate_Blood_Group(result:dict)->list:
    blood_group=result['ABO_Group']
    rh_factor=result['RH_Factor']
    return [f"{blood_group} {rh_factor}"]
