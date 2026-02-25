import joblib
import pandas as pd

model=joblib.load("cbc_model.pkl")
mlb=joblib.load('mlb.pkl')
def predicit_cbc(patient_data):
    input_data=pd.DataFrame([patient_data.dict()])
    prediction=model.predict(input_data)
    predictied_labels=mlb.inverse_transform(prediction)[0]
    return predictied_labels