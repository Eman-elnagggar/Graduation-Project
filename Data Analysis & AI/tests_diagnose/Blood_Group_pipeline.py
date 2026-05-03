
def evaluate_Blood_Group(result:dict)->tuple[dict, str]:
    blood_group=result['ABO_Group']
    rh_factor=result['RH_Factor']
    return {'test_name':'Blood Group',
            'ABO_Group': [f"{blood_group}", f"Mom ,Blood group is {blood_group}"],
            'RH_Factor': [f"{rh_factor}", f"Mom ,Rh factor is {rh_factor}"]},"Hey,Mom! Your blood group is {blood_group} and your Rh factor is {rh_factor}. This information is important for your healthcare providers to know, especially during pregnancy."
