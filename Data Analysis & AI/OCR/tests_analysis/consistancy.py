from difflib import SequenceMatcher
def map_protein_to_ordinal(value):
    value = str(value).lower().strip()
    # 0: Negative
    if value in ['negative', 'neg', 'nil', 'none', '0']:
        return 0
    # 1: Trace
    elif value in ['trace', 'tr']:
        return 1
    # 2: +1 or Small
    elif value in ['1','+1', '1+', 'positive', 'pos', 'small', 'present']:
        return 2
    # 3: +2 or Moderate
    elif value in ['2','+2', '2+', '++', 'moderate']:
        return 3
    # 4: +3 or +4 or Large
    elif value in ['3','+3', '3+', '+++', '+4', '4+', '++++', 'large']:
        return 4 
    return 0 # Default


def consistancy_data(values)->dict:
    compared_urine_keys=['Protein','Glucose','Nitrite','Ketones','Blood']
    compared_blood_group_keys=['RH_Factor']
    compared_hepatitis_B_keys=['HBsAg']
    compared_hepatitis_C_keys=['HCV']

    tests=['Urinalysis','Blood Group','HBsAg (Hepatitis B)','HCV (Hepatitis C)']
    compared_tests_keys=[compared_urine_keys,compared_blood_group_keys,compared_hepatitis_B_keys,compared_hepatitis_C_keys]

    positive_values=['positive','reactive','pos','+']
    negative_values=['negative','non-reactive','neg','none','nil','-']
    
    if values['test_name'] not in tests:
        return values
    else:
        compared_group=compared_tests_keys[tests.index(values['test_name'])]
    
    for key in compared_group:
        user_value = values[key].lower()
        if key =='Protein':
            values[key]=map_protein_to_ordinal(user_value)
        else:
            if user_value in positive_values:
                values[key] = 'positive'
            elif user_value in negative_values:
                values[key] = 'negative'

    return values
    



    # SIMILARITY_THRESHOLD = 0.80  # 80% threshold
    # for key in compared_group:
    #     user_value = values[key].lower()
    #     if user_value in ['not extracted','trace']:
    #         continue

    #     # Calculate similarity with positive values
    #     positive_match = max([SequenceMatcher(None, user_value, pv).ratio() for pv in positive_values])
    #     # Calculate similarity with negative values
    #     negative_match = max([SequenceMatcher(None, user_value, nv).ratio() for nv in negative_values])
        
    #     # Find the best match
    #     best_match_score = max(positive_match, negative_match)
        
    #     if best_match_score >= SIMILARITY_THRESHOLD:
    #         if positive_match > negative_match:
    #             values[key] = 'positive'
    #         else:
    #             values[key] = 'negative'
    #     else:
    #         # If similarity is below 90%, return error
    #         return {"error": f"You Enter Invalid Values for {key}"}
    
    # return values

    