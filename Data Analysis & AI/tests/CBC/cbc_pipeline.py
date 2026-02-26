import joblib
import pandas as pd
from sklearn.preprocessing import PowerTransformer

class cbc_model_prediction:
    def __init__(self):
        self.__model=joblib.load("cbc_model.pkl")
        self.__mlb=joblib.load('mlb.pkl')
        self.__pt=joblib.load('power_transformer.pkl')
        self.__labels=['Leukopenia','Leukocytosis','Anemia','Elevated Hemoglobin','Thrombocytopenia','Thrombocytosis','Microcytosis','Macrocytosis'
                    ,'Lymphopenia','Lymphocytosis']
    
    def __input_to_df(self,input_data):
        return pd.DataFrame([input_data]).astype(float)
    
    def __preprocessing(self,input_data):
        if input_data.loc[0, 'wbc'] >= 1000:
            input_data.loc[0, 'wbc'] = input_data.loc[0, 'wbc'] / 1000
        if input_data.loc[0, 'platelets'] >= 1000:
            input_data.loc[0, 'platelets'] = input_data.loc[0, 'platelets'] / 1000
        
        input_data['abs_lymp']=(input_data['lymp'] * input_data['wbc']) / 100
        cols_to_fix=['wbc','platelets','abs_lymp']
        input_data[cols_to_fix] = self.__pt.transform(input_data[cols_to_fix])

        del input_data['lymp']
        del input_data['mch']

        return input_data

    def predicit_cbc(self,input_data):
        input_data=self.__input_to_df(input_data)
        input_data=self.__preprocessing(input_data)
        print(input_data)
        sample_pred = self.__model.predict(input_data)[0]
        predicted_diagnoses = [self.__labels[i] for i in range(len(self.__labels)) if sample_pred[i] == 1]
        return predicted_diagnoses
    
if __name__=="__main__":

    input_data={
        "age":48,
        "hb":10.9,
        "rbc":3.75,
        "wbc":231100,
        "platelets":333000.0,
        "lymp":79.0,
        "mcv":75.6,
        "mch":24.3,
        "mchc":33.1,
    }
    obj=cbc_model_prediction()
    print(obj.predicit_cbc(input_data))