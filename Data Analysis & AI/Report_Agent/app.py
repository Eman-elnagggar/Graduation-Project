from fastapi import FastAPI
import connect_api_one
import divide_result_1
import class_agent
app = FastAPI()


@app.post("/submit-json")
async def submit_json(data: dict):
    result_1= connect_api_one.connect_api_one(data)
    list_1,list_2,list_3,list_4= divide_result_1.divide_result_1(result_1)
    agent= class_agent.ReportAgent(list_1,list_2,list_3)
    final_report= agent.generate_report()
    return final_report