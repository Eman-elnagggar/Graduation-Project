import copy
class Preprocessing:
    def __init__(self):
        pass
    
    def preprocess(self, data):
        for k,v in data.items():
            if v=="Not Extracted":
                data[k]=None
            if v==-1:
                data[k]=0
        return data
    
    def CBC_preprocessing_before(self,cbc_data,age):
        model_data={
            "Age": age, "Hb": cbc_data['HB'], "RBC": cbc_data["RBCs_Count"], 
            "WBC": cbc_data["WBC"], "PLATELETS": cbc_data["platelet_count"],
            "HCT": 0,
            "MCV": cbc_data["MCV"], "MCH": cbc_data["MCH"], 
            "MCHC": 0,
            "RDW": cbc_data["RDW"],
            "PDW": 0, "MPV": 0, "PCT": 0, "Lymphocytes": cbc_data["lymphocytes"],
            "Monocytes":0, "Neutrophils":0, "Eosinophils":0, "Basophils": 0
        }
        cbc_data['Iron Deficiency Anemia']="None"
        del cbc_data['RBCs_Count']
        del cbc_data['MCH']
        del cbc_data['lymphocytes']

        if cbc_data['WBC'] >1000:
            model_data['WBC']=cbc_data['WBC']/1000
        if cbc_data['platelet_count'] >10000:
            model_data['PLATELETS']=cbc_data['platelet_count']/10000

        for k,v in cbc_data.items():
            if v=='CBC (Complete Blood Count)':
                continue
            cbc_data[k]=[v,None]

        return cbc_data,model_data

    def cbc_reasons(self,value,reason,max_border,normal_range,state):

        if reason =="Blood indices show patterns consistent with this condition.":
            text=f"The value {value} is higher than max border of normal range {max_border}"
        elif state=='Normal':
            text=f"The value {value} is within the normal range {normal_range}"
        else:
            text=reason
        
        return text 
        
    def cbc_preprocessing_after(self,cbc_result,cbc_data):
        diagnoses=cbc_result["detailed_analysis"]
        # if not diagnoses:
        #     return []
        
        normal_range={
            "HB":'12-15 g/dl',
            "MCV":'80-100 fL',
            "RDW":'11.5-15 %',
            "WBC":'4000-10000 cells/mcl',
            "platelet_count":'150,000-400,000 cells/mcl',
            "Iron Deficiency Anemia":'None'
        }

        max_border={
            "HB":'15 g/dl',
            "MCV":'100 fL',
            "RDW":'15 %',
            "WBC":'10 x10^3cells/mcl',
            "platelet_count":'400 x10^3 cells/mcl',
            "Iron Deficiency Anemia":'None'
        }

        diagnoses_result={
            "HB":['Anemia', 'Elevated Hemoglobin'],
            "MCV":['Microcytosis', 'Macrocytosis'],
            "RDW":['Elevated RDW'],
            "WBC":['Leukopenia', 'Leukocytosis'],
            "platelet_count":['Thrombocytopenia', 'Thrombocytosis'],
            'Iron Deficiency Anemia':['Probable Iron Deficiency Anemia (IDA)']
        }

        # returned_diagnoses=[]
        # returned_reasons=[]

        cbc_diagnoses=[]
        cbc_reasons=[]

        # print(cbc_result)
        for diagnose in diagnoses:
            cbc_diagnoses.append(diagnose['condition'])
            cbc_reasons.append(diagnose['reason'])
        
        
        for k,v in diagnoses_result.items():
            state=None
            flag=False
            current_reasone=None
            current_diagnose=None
            for item in v:
                if item in cbc_diagnoses:
                    flag=True
                    current_diagnose=item
                    current_reasone=cbc_reasons[cbc_diagnoses.index(item)]
            
            if flag ==False:
                state='Normal'
                current_diagnose=state

            if current_diagnose=='Thrombocytopenia':
                reason=f"Platelet count ({cbc_data[k][0]}) is lower than normal, suggesting Thrombocytopenia."
            if current_diagnose=='Microcytosis':
                reason=f"MCV ({cbc_data[k][0]}) is lower than normal, suggesting Microcytosis."
            else:
                reason = self.cbc_reasons(cbc_data[k][0],current_reasone,max_border[k],normal_range[k],state)
            cbc_data[k][0]=current_diagnose
            cbc_data[k][1]=reason
            
        return cbc_data,cbc_result['final_report']['message']
    
    def urine_model_data(self,urine_data):
        print(urine_data)
        if urine_data['Specific_Gravity']>=1000:
            urine_data['Specific_Gravity']=urine_data['Specific_Gravity']/1000
        model_data={
            "color": urine_data['Color'],
            "ph": urine_data['PH'],
            "specific_gravity": urine_data['Specific_Gravity'],
            "protein": urine_data['Protein'],
            "glucose": urine_data['Glucose'],
            "ketones": urine_data['Ketones'],
            "blood": urine_data['Blood'],
            "rbcs": urine_data['RBCs'],
            "leukocytes": urine_data['Leukocytes'],
            "nitrite": str(urine_data['Nitrite'])
        }

        return model_data
    
    def urine_result_preprocess(self,model_result,original_data):
        normal_range={
            "color": "Pale Yellow to Amber",
            "ph": "4.6 – 8.0",
            "specific_gravity": "1.005 – 1.025",
            "protein": "Negative",
            "glucose": "Negative",
            "ketones": "Negative",
            "blood": "Negative",
            "rbcs": "0 – 2",
            "leukocytes": "0 – 5",
            "nitrite":"Negative"
        }
        alerts="According to urine test you need to "
        diagnose_data=copy.deepcopy(original_data)
        
        print(model_result)
        for k,v in model_result.items():
            str1="";str2=k.title()
            if k=='PH': str2=k
            if k=='RBCS':str2='RBCs'
            if (type(v)==list) and (k!='additional_result'):
                if v[0]=='Normal':
                    str1=f'The value {original_data[str2]} within the normal range {normal_range[k.lower()]}'
                else:
                    alerts+=(v[1]+"\n"+" ")
                    str1=f'The value {original_data[str2]} not in the normal range {normal_range[k.lower()]}'
                diagnose_data[str2]=[v[0],str1]
            if k=='additional_result':
                diagnose_data['other_diagnoses']=v

        
        return diagnose_data,alerts
    
    def map_protein_to_ordinal(self,value):
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
        return 0 # Defaul
        