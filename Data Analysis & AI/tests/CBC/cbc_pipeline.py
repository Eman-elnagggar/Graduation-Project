import joblib
import pandas as pd
from sklearn.preprocessing import PowerTransformer

class cbc_model_prediction:
    def __init__(self):
        self.__model=joblib.load("cbc_model.pkl")
        self.__mlb=joblib.load('mlb.pkl')
        self.__pt=joblib.load('power_transformer.pkl')
    
    def __input_to_df(self,input_data):
        return pd.DataFrame([input_data.dict()])
    
    def __preprocessing(self,input_data):
        input_data['']
    def predicit_cbc(patient_data):
        prediction=model.predict(input_data)
        predictied_labels=mlb.inverse_transform(prediction)[0]
        return predictied_labels