
def display(extracted_ingradients)->list:
    risky_list=extracted_ingradients['risky']['ingredients']
    risky_score=extracted_ingradients['risky']['match_score']
    avoid_list=extracted_ingradients['avoid']['ingredients']
    avoid_score=extracted_ingradients['avoid']['match_score']

    results={
        'safe':[],
        'risky':[],
        'avoid':[]
    }

    if not risky_list and  not avoid_list:
        results['safe'].append("According to our database, the ingredients in this product are not harmful, but if you have any doubts, it is best to consult a pharmacist or doctor.")
    else:
        if risky_list:
          risky_text=",".join(risky_list)
          risky_score_text=",".join(risky_score)

          results['risky'].append(f"Based on our data, this product has some ingredients that might be unsafe.Here are the ingredients we found [{risky_text}] and this the accuracy of our scan [{risky_score_text}]. You can consult a pharmacist or doctor about it.")
        
        if avoid_list:
            avoid_text=",".join(avoid_list)
            avoid_score_text=",".join(avoid_score)

            results['avoid'].append(f"Based on our data, this product has ingredients you should avoid. Here is a list of what we found [{avoid_text}] and this the accuracy of our scan [{avoid_score_text}]")
            

    return results