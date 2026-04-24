import os
from dotenv import load_dotenv
from langchain_groq import ChatGroq
from langchain_core.prompts import ChatPromptTemplate
from langchain_core.output_parsers import StrOutputParser

import divide_result_1

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
        You are a Medical Reporting Specialist. Generate a professional medical report in ENGLISH.

        STRICT RULES:
        1. OMIT any section that has empty or null JSON data.
        2. DO NOT use the word "Section" or any numbers in the titles. Use only the title names.
        3. For the first section, use the EXACT layout provided below without any additional explanations or sentences.
        4. Combine 'avg_systolic' and 'avg_diastolic' into one BP reading (e.g., 120/80 mmHg).
        5. For Laboratory Test Results:
       - You will receive a list of JSON objects. Each object represents one test (e.g., CBC, fasting blood glucose).
       - Inside each JSON: 
         * 'test_name' is the header.
         * Ignore 'confidence' if present.
         * Map each 'value_name' to its corresponding 'value_result'.
         * The 'diagnose' attribute is a LIST of strings. Match each value with its diagnosis by index.
         * If a diagnosis is "None", display "-" instead.
       - FORMAT: Create a Markdown table with columns: (test_name, Value_name, Value_result, Diagnose).
       6.For Predictions and Risk Analysis (Input is a List containing 2 JSON objects):
       - First JSON (Gestational Diabetes): Extract 'prediction', 'risk_score', and 'risk_level'. 
         Display under header: "Gestational Diabetes Prediction".
       - Second JSON (Risk Assessment): Extract 'risk_level' and 'confidence'. 
         Display under header: "Overall Risk Assessment".
       - Present both sections with professional medical phrasing suitable for a doctor.
       7.Table Optimization: In the "Laboratory Test Results" table, write the 'test_name' ONLY for the first row of that test. For all following rows belonging to the same test, leave the 'test_name' column empty. This creates a cleaner, grouped look.

        Structure & Layout:

        Patient Essential Information:
        Name: (Value)          Age: (Value)          Trimester: (Value)          Current Weeks: (Value)
        Baby Gender: (Value)   Height:(Value)           Weight: (Value)
        Last Week Random Blood Sugar Average (RBS): (Value)
        Last Week Blood Pressure Average (BP): (Value)
        Gestational Diabetes (GD) state: (Value)
        Risk state: (Value)

        Laboratory Test Results:
        | test_name | Value_name | Value_result | Diagnose |
        |-----------|------------|--------------|----------|
        | (Test]    | (Name)     | (Result)     | (Diag/-) |

        Gestational Diabetes Prediction:
        - Prediction Result: (prediction)
        - Risk Score: (risk_score)
        - Risk Level: (risk_level)

       Overall Risk Assessment:
        - Risk Level: (risk_level)
        - Confidence: (confidence)
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

l1,l2,l3,l4= divide_result_1.divide_result_1({
  "result_1": {
    "name": "Sara Ahmed",
    "age": 28,
    "trimester": 2,
    "week": 24,
    "baby_gender": "Female",
    "height": 165,
    "weight": 80,
    "rbs_avg": 145,
    "avg_systolic": 125,
    "avg_diastolic": 80,
    "parity": 1,
    "dg_state": "Stable",
    "risk_state": "Low"
  },
  "result_2": [
    {
      "test_name": "CBC (Complete Blood Count)",
      "confidence": "0.95",
      "HB": 12.5,
      "RBCs_Count": 5.2,
      "MCV": 87.75,
      "MCH": 27.2,
      "MCHC": 32.8,
      "WBC": 9000,
      "lymphocytes": 31,
      "platelet_count": 150000,
      "diagnose": None
    },
    {
      "test_name": "Urinalysis",
      "confidence": "0.97",
      "Color": "amber yellow",
      "PH": 5,
      "Specific_Gravity": 1030,
      "Protein": 0,
      "Glucose": "nil",
      "Ketones": "nil",
      "Blood": "trace",
      "RBCs": "6-8",
      "Leukocytes": "8-10",
      "Nitrite": "Not Extracted",
      "diagnose": None
    },
    {
      "test_name": "Fasting Blood Glucose",
      "confidence": "0.94",
      "FBG": 245,
      "diagnose": [
        "Diabetes"
      ]
    }
  ],
  "result_3": [
    {
      "prediction": "Diabetic",
      "risk_score": "100.0%",
      "risk_level": "High Risk"
    },
    {
      "risk_level": "High Risk",
      "confidence": 1
    }
  ],
  "result_4": [
    "Diabetes detected. Seek medical advice for management and treatment.",
    "Please consult a doctor for a formal diagnosis.",
    "High Risk detected! Immediate medical attention is strongly recommended."
  ]
})

agent= ReportAgent(l1,l2,l3)
final_report= agent.generate_report()
print(final_report)