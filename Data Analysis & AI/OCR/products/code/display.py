
def display(extracted_ingradients)->list:
    risky_list=extracted_ingradients['risky']['ingredients']
    risky_score=extracted_ingradients['risky']['match_score']
    avoid_list=extracted_ingradients['avoid']['ingredients']
    avoid_score=extracted_ingradients['avoid']['match_score']
    explained_text=[]
    status =[]
    if not risky_list and  not avoid_list:
        explained_text.append("According to our database, the ingredients in this product are not harmful, but if you have any doubts, it is best to consult a pharmacist or doctor.")
        status.append('safe')
    else:
        if risky_list:
          risky_text=",".join(risky_list)
          risky_score_text=",".join(risky_score)

          explained_text.append(f"Based on our data, this product has some ingredients that might be unsafe.Here are the ingredients we found [{risky_text}] and this the accuracy of our scan [{risky_score_text}].\n You can consult a pharmacist or doctor about it.")
          status.append("risky")
        
        if avoid_list:
            avoid_text=",".join(avoid_list)
            avoid_score_text=",".join(avoid_score)

            explained_text.append(f"Based on our data, this product has ingredients you should avoid. Here is a list of what we found [{avoid_text}] and this the accuracy of our scan [{avoid_score_text}]")
            status.append("avoid")

    return status,explained_text
