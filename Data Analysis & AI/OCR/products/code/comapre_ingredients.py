import pandas as pd
from thefuzz import fuzz

class compare_ingredients:
    def __init__(self,text):
        self.__text=text
        self.__df=pd.read_csv("ingredients.csv")
        self.__found_ingredients = {
            'risky':{'ingredients':[],'match_score':[]},
            'avoid':{'ingredients':[],'match_score':[]}
        }

    def analyze_ingredients(self):
        for index,row in self.__df.iterrows():
            ingredient_name=row['Products']
            status=row['status']
            score = fuzz.token_set_ratio(ingredient_name, self.__text)
            if score >= 90:
              self.__found_ingredients[status]['ingredients'].append(ingredient_name)
              self.__found_ingredients[status]['match_score'].append(str(score))
            
        return self.__found_ingredients
    

