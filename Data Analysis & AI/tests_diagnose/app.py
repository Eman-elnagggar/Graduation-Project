from fastapi import FastAPI
import json
import diagnose
import personal_information

app = FastAPI()


@app.post("/submit-json")
async def submit_json(data: dict):
    with open("stored_data.json", "w") as f:
        json.dump(data, f, indent=4)
    
    result_1=personal_information.personal_results("./stored_data.json")
    diagnose_object=diagnose.diagnose("./stored_data.json")
    result_2,result_3,result_4=diagnose_object.diagnose_function()
    return {
        "result_1": result_1,
        "result_2": result_2,
        "result_3": result_3,
        "result_4": result_4    
    }