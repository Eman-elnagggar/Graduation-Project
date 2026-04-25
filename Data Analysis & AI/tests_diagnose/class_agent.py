import os
from dotenv import load_dotenv
from langchain_groq import ChatGroq
from langchain_core.prompts import ChatPromptTemplate
from langchain_core.output_parsers import StrOutputParser

class ReportAgent:
    def __init__(self, personal_results: dict, tests_diagnose_results: list, model_diagnose_results: list):
        self.__personal_results = personal_results
        self.__tests_diagnose_results = tests_diagnose_results
        self.__model_diagnose_results = model_diagnose_results
        self.__llm = self.__Load_Agent()

    def __Load_Agent(self):
        load_dotenv()
        llm = ChatGroq(
        model="llama-3.3-70b-versatile",
        api_key=os.getenv("Groq_API_Key"),
        temperature=0 )
        return llm
    
    def __system_message(self)-> str:
        system_message = """
        You are an expert Medical Diagnostic Consultant. Your goal is to analyze three lists of patient data (Personal Information, Laboratory Results, and Risk Predictions) to provide a concise clinical synthesis of the patient's current status and recommended next steps.
        DATA STRUCTURE INPUT:
        1. List 1 (Personal Info): Contains patient vitals, age, trimester, and averages (BP, RBS).
        2. List 2 (Lab Results): A list of JSON objects. Each has 'test_name', 'value_name', 'value_result', and a 'diagnose' list (match values to diagnoses by index).
        3. List 3 (Risk Profiles): Two JSON objects:
        - Object A: Gestational Diabetes (GD) prediction, risk_score, and risk_level.
        - Object B: Overall Risk Assessment including risk_level and confidence.

        STRICT PROCESSING RULES:
        - Map each 'value_name' in List 2 to its corresponding 'diagnose' string. If "None", treat as normal/negative.
        - Combine 'avg_systolic' and 'avg_diastolic' from List 1 into a standard (120/80 mmHg) format for your internal analysis.
        - Do not use the word "Section" or numbers in your final response headers.
        
        STRICT OPERATIONAL RULES:
        1. LANGUAGE: The final output must be in professional clinical ENGLISH.
        2. STRUCTURE:
        - Provide a brief summary of the patient's current clinical profile (1-2 sentences).
        - Provide a "Current Medical Status & Analysis" section (3-4 concise, evidence-based sentences).
        - Provide a "Future Action Plan & Recommendations" section (bullet points or 2-3 sentences).
        3. DATA INTEGRATION:
        - Use the Personal Info list to understand context (age, trimester, vitals).
        - Analyze the Laboratory Results list, specifically looking at the 'diagnose' key for each value to identify abnormalities.
        - Integrate the Risk Prediction list (Gestational Diabetes and Overall Risk) to determine the urgency of the case.
        4. MEDICAL REASONING: Your analysis must be grounded in established medical guidelines (e.g., ACOG, ADA, or WHO standards).
        5. TONE: Professional, objective, and supportive. Avoid medical jargon that a patient wouldn't understand, but maintain clinical accuracy for a physician's review.

        OUTPUT FORMAT:
        Current Medical Status & Analysis:
        [Your 3-4 sentences synthesizing the data from all three lists, highlighting specific abnormal lab values and their correlation with the predicted risk levels.]

        Future Action Plan:
        [Suggest specific next steps: e.g., further diagnostic tests (OGTT), lifestyle modifications, specialized consultations, or immediate medical intervention based on the risk scores.]
        """
        return system_message
    
    def generate_report(self) -> str:
        prompt = ChatPromptTemplate.from_messages([
        ("system", self.__system_message()),
        ("user", "Input Data:\nSection 1: {personal_results}\nSection 2: {tests_diagnose_results}\n"
        "Section 3: {model_diagnose_results}")
        ])

        report_chain = prompt | self.__llm | StrOutputParser()
        final_report = report_chain.invoke({
        "personal_results": self.__personal_results,
        "tests_diagnose_results": self.__tests_diagnose_results,
        "model_diagnose_results": self.__model_diagnose_results
        })
        return final_report

# l1,l2,l3,l4= divide_result_1.divide_result_1({
#   "result_1": {
#     "name": "Sara Ahmed",
#     "age": 28,
#     "trimester": 2,
#     "week": 24,
#     "baby_gender": "Female",
#     "height": 165,
#     "weight": 80,
#     "rbs_avg": 145,
#     "avg_systolic": 125,
#     "avg_diastolic": 80,
#     "parity": 1,
#     "dg_state": "Stable",
#     "risk_state": "Low"
#   },
#   "result_2": [
#     {
#       "test_name": "CBC (Complete Blood Count)",
#       "confidence": "0.95",
#       "HB": 12.5,
#       "RBCs_Count": 5.2,
#       "MCV": 87.75,
#       "MCH": 27.2,
#       "MCHC": 32.8,
#       "WBC": 9000,
#       "lymphocytes": 31,
#       "platelet_count": 150000,
#       "diagnose": None
#     },
#     {
#       "test_name": "Urinalysis",
#       "confidence": "0.97",
#       "Color": "amber yellow",
#       "PH": 5,
#       "Specific_Gravity": 1030,
#       "Protein": 0,
#       "Glucose": "nil",
#       "Ketones": "nil",
#       "Blood": "trace",
#       "RBCs": "6-8",
#       "Leukocytes": "8-10",
#       "Nitrite": "Not Extracted",
#       "diagnose": None
#     },
#     {
#       "test_name": "Fasting Blood Glucose",
#       "confidence": "0.94",
#       "FBG": 245,
#       "diagnose": [
#         "Diabetes"
#       ]
#     }
#   ],
#   "result_3": [
#     {
#       "prediction": "Diabetic",
#       "risk_score": "100.0%",
#       "risk_level": "High Risk"
#     },
#     {
#       "risk_level": "High Risk",
#       "confidence": 1
#     }
#   ],
#   "result_4": [
#     "Diabetes detected. Seek medical advice for management and treatment.",
#     "Please consult a doctor for a formal diagnosis.",
#     "High Risk detected! Immediate medical attention is strongly recommended."
#   ]
# })

# agent= ReportAgent(l1,l2,l3)
# final_report= agent.generate_report()
# print(final_report)