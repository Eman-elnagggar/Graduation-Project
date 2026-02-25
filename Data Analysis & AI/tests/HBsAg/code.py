def is_HBsAG(value:str)->str:
    words_of_good_test_results=['positive','reactive']
    words_of_bad_test_results=['negative','non-reactive']

    if value in words_of_good_test_results:
        return "Indicates the absence of the Hepatitis B Surface Antigen in the blood"
    if value in words_of_bad_test_results:
        return "Indicates the presence of the Surface Antigen, meaning there is an active Hepatitis B infection (either acute or chronic)."
    
HBsAG="positive"
print(is_HBsAG(HBsAG))