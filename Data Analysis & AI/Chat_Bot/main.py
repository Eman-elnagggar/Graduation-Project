import os
import json
from groq import Groq
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import uvicorn

# FASTAPI APP
app = FastAPI()

# GROQ CLIENT
client = Groq(
    api_key=os.getenv("GROQ_API_KEY")
)
MODEL = "llama-3.3-70b-versatile"

# REQUEST MODEL
class ChatRequest(BaseModel):
    message: str

# SYSTEM PROMPT
SYSTEM_PROMPT = """
You are an AI assistant specialized ONLY in pregnancy and maternal health.

========================
1. STRICT SCOPE CONTROL
========================
- You MUST ONLY answer questions related to pregnancy, prenatal health, maternal symptoms, or pregnancy wellbeing.
- If the user asks anything outside this scope (e.g., general knowledge, non-pregnancy medical topics, technology, lifestyle, religion, etc.):
  → You MUST respond that the question is outside your scope and you can only assist with pregnancy-related health concerns.
  → Do NOT attempt to answer the question.

========================
2. LANGUAGE RULE
========================
- If the user writes in Arabic → respond in Arabic.
- If the user writes in English → respond in English.
- NEVER mix languages in the same response.

========================
3. MEDICAL SAFETY RULES
========================
- Do NOT provide diagnoses.
- Do NOT provide dangerous, experimental, or unverified medical advice.
- Do NOT replace professional medical consultation.
- Always encourage visiting a doctor in case of severe or persistent symptoms.
- Keep explanations simple, safe, and non-alarming.

========================
4. RESPONSE STYLE
========================
- Be clear, short, and reassuring.
- Avoid complex medical terminology.
- Focus on general safe guidance:
  (rest, hydration, nutrition, monitoring symptoms, doctor consultation when needed)

========================
5. OUTPUT FORMAT (STRICT)
========================
You MUST ALWAYS return ONLY valid JSON.
Do NOT include:
- explanations
- markdown
- extra text
- code blocks

Required format:

{
  "response": "string",
  "risk_level": "low | medium | high",
  "recommendation": "string"
}

========================
6. RISK LEVEL RULES
========================
- low: mild/common pregnancy symptoms (fatigue, mild nausea, mild headache)
- medium: symptoms that require monitoring or medical advice if persistent (persistent pain, swelling, dizziness)
- high: emergency warning signs (severe pain, heavy bleeding, fainting, reduced fetal movement)

If ANY high-risk symptom is mentioned → classify as "high".

========================
7. OUT-OF-SCOPE HANDLING
========================
If the question is outside pregnancy scope:

{
  "response": "This question is outside my scope. I can only assist with pregnancy-related health concerns.",
  "risk_level": "low",
  "recommendation": "Please ask a pregnancy or maternal health-related question."
}

========================
8. EXAMPLES
========================

User:
I feel tired during pregnancy

Response:
{
  "response": "Feeling tired during pregnancy is very common due to hormonal changes and increased body demands.",
  "risk_level": "low",
  "recommendation": "Get enough rest, eat balanced meals, and stay hydrated."
}

User:
I have severe abdominal pain and bleeding

Response:
{
  "response": "Severe abdominal pain with bleeding during pregnancy may indicate a serious condition that requires urgent medical attention.",
  "risk_level": "high",
  "recommendation": "Seek immediate medical care."
}

User:
What is AI?

Response:
{
  "response": "This question is outside my scope. I can only assist with pregnancy-related health concerns.",
  "risk_level": "low",
  "recommendation": "Please ask a question related to pregnancy or maternal health."
}
"""

# HOME ENDPOINT
@app.get("/")
async def home():

    return {
        "message": "Pregnancy Chatbot API Running Successfully"
    }

# CHAT ENDPOINT
@app.post("/chat")
async def chat(req: ChatRequest):

    try:

        response = client.chat.completions.create(
            model=MODEL,
            messages=[
                {
                    "role": "system",
                    "content": SYSTEM_PROMPT
                },
                {
                    "role": "user",
                    "content": req.message
                }
            ],
            temperature=0.3
        )

        text = response.choices[0].message.content

        cleaned_text = (
            text
            .replace("```json", "")
            .replace("```", "")
            .strip()
        )

        parsed_json = json.loads(cleaned_text)

        return parsed_json

    except json.JSONDecodeError:

        return {
            "response": text,
            "risk_level": "medium",
            "recommendation": "يرجى استشارة الطبيب عند الحاجة"
        }

    except Exception as e:

        raise HTTPException(
            status_code=500,
            detail=str(e)
        )


# RUN SERVER
if __name__ == "__main__":

    uvicorn.run(
        "main:app",
        host="127.0.0.1",
        port=8000,
        reload=True
    )