import json
import HbA1c_pipeline
import Ferritin_pipeline
import tsh_pipeline
import Blood_Group_pipeline
import FBG_pipeline
import alerts
import requests
import copy
class diagnose:
    def __init__(self,path):
        self.__path=path
        self.__data=self.__read_data()
        self.__GDM_API_URL="https://mennnaa-gestational-diabetes.hf.space/predict"
        self.__Risk_API_URL="https://mariamelnemrawy25-risk-api.hf.space/predict"

    def __read_data(self):
        with open(self.__path, "r", encoding="utf-8") as f:
            my_data_dict = json.load(f)
        return my_data_dict
    
    def __get_tests_names(self):
        tests_names=[]
        for result in self.__data["results"]:
            tests_names.append(result["test_name"])
        return tests_names
    
    def __call_GDM_model(self,data_to_send):
        try:
            response=requests.post(self.__GDM_API_URL, json=data_to_send)
            if response.status_code == 200:
                return response.json() 
            else:
                return {"error": f"Request failed with status code {response.status_code}"}
        except Exception as e:
            return f"Connection Error: {str(e)}"
    
    def __call_Risk_model(self,data_to_send):
        try:
            response=requests.post(self.__Risk_API_URL, json=data_to_send)
            if response.status_code == 200:
                return response.json()
            else:
                return {"error": f"Request failed with status code {response.status_code}"}
        except Exception as e:
            return f"Connection Error: {str(e)}"

    def __data_to_send(self,test_names:list)->list:
        GDM_data={
        "age_years": self.__data["personal_information"]["age"],
        "fasting_glucose_mgdl": 0,
        "proteinuria": 0,
        "weight_kg": self.__data["personal_information"]["weight"],
        "height_cm": self.__data["personal_information"]["height"],
        "systolic_bp": self.__data["personal_information"]["avg_systolic"],
        "diastolic_bp": self.__data["personal_information"]["avg_diastolic"]
        }
        Risk_data={
        "age_years": self.__data["personal_information"]["age"],
        "gestational_age_weeks": self.__data["personal_information"]["week"],
        "systolic_bp_mmhg": self.__data["personal_information"]["avg_systolic"],
        "diastolic_bp_mmhg": self.__data["personal_information"]["avg_diastolic"],
        "fasting_glucose_mgdl":0,
        "proteinuria": 0,
        "hemoglobin_gdl": 0,
        "parity": self.__data["personal_information"]["parity"],
        "weight_kg": self.__data["personal_information"]["weight"],
        "height_cm": self.__data["personal_information"]["height"]
        }

        Models_diagnose_data=[]

        if 'Urinalysis' in test_names:
            urinalysis = next((item for item in self.__data["results"] if item["test_name"] == "Urinalysis"), None)
            GDM_data["proteinuria"] = urinalysis.get("Protein", 0) if urinalysis else 0
            Risk_data["proteinuria"] = urinalysis.get("Protein", 0) if urinalysis else 0
        else:
            GDM_data["proteinuria"] = 0
            Risk_data["proteinuria"] = 0

        if 'Fasting Blood Glucose' in test_names:
            fasting_glucose = next((item for item in self.__data["results"] if item["test_name"] == 'Fasting Blood Glucose'), None)
            GDM_data["fasting_glucose_mgdl"]=fasting_glucose.get("FBG", 0) if fasting_glucose else 0
            Risk_data["fasting_glucose_mgdl"]=fasting_glucose.get("FBG", 0) if fasting_glucose else 0
            Models_diagnose_data.append(GDM_data)
        else:
             Models_diagnose_data.append({})
             Risk_data['fasting_glucose_mgdl']=0

        if 'CBC (Complete Blood Count)' in test_names:
            cbc = next((item for item in self.__data["results"] if item["test_name"] == 'CBC (Complete Blood Count)'), None)
            Risk_data["hemoglobin_gdl"]=cbc.get('HB', 0) if cbc else 0
            Models_diagnose_data.append(Risk_data)
        else:
             Risk_data['hemoglobin_gdl']=0
        
        Models_diagnose_data.append(Risk_data)
        # print("#"*50)
        # print(Risk_data)
        # print("#"*50)
        # print(GDM_data)
        # print("#"*50)
        return Models_diagnose_data
        
    def __if_then_diagnose(self,result:dict):
        if result['test_name']=='Urinalysis':
            pass
        elif result['test_name']=='CBC (Complete Blood Count)':
            pass
        elif result['test_name']=='HbA1c (Sugar Test)':
            return HbA1c_pipeline.evaluate_HbA1c(result)
        elif result['test_name']=='Ferritin':
            return Ferritin_pipeline.evaluate_Ferritin(result)
        elif result['test_name']=='TSH (Thyroid)':
            return tsh_pipeline.evaluate_tsh(result,self.__data["personal_information"]["trimester"])
        elif result['test_name']=='Blood Group':
            return Blood_Group_pipeline.evaluate_Blood_Group(result)
        elif result['test_name']=='Fasting Blood Glucose':
            return FBG_pipeline.evaluate_FBG(result)
        elif result['test_name']=='HBsAg (Hepatitis B)':
            return result['HBsAg']
        elif result['test_name']=='HCV (Hepatitis C)':
            return result['HCV']
        

    def diagnose_function(self):
        test_names=self.__get_tests_names()
        tests_diagnose_results=[]
        model_diagnose_results=[]
        alerts_results=[]
        diagnose_data=copy.deepcopy(self.__data["results"])
        for result in diagnose_data:
            diagnose=self.__if_then_diagnose(result)
            i=0
            # print(result)
            for k,v in result.items():
                if k in ['test_name','confidence']:
                    pass
                else:
                    if not diagnose:
                        result[k]=[v,None]
                    else:
                        result[k]=[v,diagnose[i]]
                    i+=1
            # print("-"*50)
            # print(result)
            # print("="*50)
            # print(self.__data['results'])
            tests_diagnose_results.append(result)
            
            if diagnose:
                # print(diagnose)
                alert=alerts.alerts(result['test_name'], diagnose)
                if alert:
                    alerts_results.append(alert)

        Models_diagnose=self.__data_to_send(test_names)
        if Models_diagnose[0]:
            GDM_result=self.__call_GDM_model(Models_diagnose[0])
            alerts_results.append(GDM_result['recommendation'])
            filtered_result = {
            k: v for k, v in GDM_result.items()
            if k not in ['status', 'recommendation']
            }
            model_diagnose_results.append(filtered_result)
        if Models_diagnose[1]:
            Risk_result=self.__call_Risk_model(Models_diagnose[1])
            alerts_results.append(Risk_result['alert'])
            filtered_result = {
            k: v for k, v in Risk_result.items()
            if k not in ['alert']
            }
            model_diagnose_results.append(filtered_result)

        return tests_diagnose_results,model_diagnose_results,alerts_results


        

# object=diagnose()
# x,y,z=object.diagnose_function()
# print(x)
# print('-'*50)
# print(y)
# print('-'*50)
# print(z)


