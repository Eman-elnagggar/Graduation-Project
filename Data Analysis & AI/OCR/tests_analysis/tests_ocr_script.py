# %% [markdown]
# **This NoteBook Will provide an OCR Version to Tests vesion**

# %% [markdown]
# - First step is to try the old steps we tried before on product notebbok and see the results

# %% [markdown]
# # Install Libraries

# %%
# !pip install "python-doctr[torch]"
# !pip install tf2onnx
# !pip install tesseract
# !pip install pytesseract
# !pip install easyocr

# %% [markdown]
# # Import Libraries

# %%
import cv2
# import pytesseract as pyt
# import easyocr
from doctr.models import ocr_predictor
from doctr.io import DocumentFile
# import matplotlib.pyplot as plt
# from PIL import Image
import os
import numpy as np
import re
# from difflib import get_close_matches
import json
# import pandas as pd

# %% [markdown]
# # Try Modling Process

# %%
# def preprocess_variants(gray):
#     variants = []
#     #Raw resized
#     raw = cv2.resize(gray, None, fx=2, fy=2, interpolation=cv2.INTER_CUBIC)
#     variants.append(raw)
#     #CLAHE only
#     clahe = cv2.createCLAHE(clipLimit=2.0,tileGridSize=(8,8))
#     v2 = clahe.apply(raw)
#     variants.append(v2)
#     #Adaptive mean threshold
#     v3=cv2.adaptiveThreshold(raw,127,cv2.ADAPTIVE_THRESH_MEAN_C,cv2.THRESH_BINARY,9,2)
#     variants.append(v3)
#     #bitwise_not
#     v4=cv2.bitwise_not(raw)
#     return variants


# # %%
# def ocr_tesseract(image):
#     config = r'--oem 3 --psm 6 -l eng'
#     pil = Image.fromarray(image)
#     return pyt.image_to_string(pil, config=config)


# # %%
# reader = easyocr.Reader(['en'], gpu=False)

# def ocr_easy(image):
#     result = reader.readtext(image, detail=0)
#     return " ".join(result)


# # %%
# def score_text(text):
#     if not text:
#         return 0
#     return sum(c.isalnum() for c in text)


# # %%
# def robust_ocr(image_path):
#     gray = cv2.imread(image_path, cv2.IMREAD_GRAYSCALE)

#     variants = preprocess_variants(gray)

#     best_text = ""
#     best_score = 0
#     best_model=''
#     for img in variants:
#         # Tesseract
#         t_text = ocr_tesseract(img)
#         t_score = score_text(t_text)

#         if t_score > best_score:
#             best_score = t_score
#             best_text = t_text
#             best_model='tesseract'

#         # EasyOCR
#         e_text = ocr_easy(img)
#         e_score = score_text(e_text)

#         if e_score > best_score:
#             best_score = e_score
#             best_text = e_text
#             best_model='easy_ocr'

#     print(best_model)
#     print("\n")
#     return best_text


# # %% [markdown]
# # ##Try algorithm

# # %%
# folder = "/content/drive/MyDrive/Graduation/Tests Samples"

# for file in os.listdir(folder):
#     if file.lower().endswith((".jpg", ".png", ".jpeg")):
#         path = os.path.join(folder, file)
#         print(f"\n{file}")
#         print(robust_ocr(path))


# # %%
# def ocr_doctr(image_rgb):
#   #Initalize model with detection and recognition stage
#   doctr_model = ocr_predictor(det_arch='db_resnet50', reco_arch='crnn_vgg16_bn', pretrained=True)
#   try:
#       # temp path to save numpy array and avoid type error conflict
#       temp_path = "doctr_temp_file.jpg"
#       cv2.imwrite(temp_path, cv2.cvtColor(image_rgb, cv2.COLOR_RGB2BGR))
#       # Standraize and normalize images
#       doc = DocumentFile.from_images([temp_path])
#       #Inference
#       result = doctr_model(doc)
#       #Parsing the export
#       export = result.export()
#       #The reconstraction
#       full_text = []
#       for page in export['pages']:
#           for block in page['blocks']:
#               for line in block['lines']:
#                   line_text = " ".join([word['value'] for word in line['words']])
#                   full_text.append(line_text)
#       if os.path.exists(temp_path):
#           os.remove(temp_path)

#       return " ".join(full_text)

#   except Exception as e:
#       print(f"DocTR Error: {e}")
#       return ""

# # %%
# folder = "/content/drive/MyDrive/Graduation/Tests Samples"

# for file in os.listdir(folder):
#     if file.lower().endswith((".jpg", ".png", ".jpeg")):
#         path = os.path.join(folder, file)
#         print(f"\n{file}")
#         img_bgr = cv2.imread(path)
#         print(ocr_doctr(img_bgr))


# # %% [markdown]
# # **Conclusion**
# # - Depend on doctor_ocr because it is faster, more flexailbe and realiable
# # - I found the key words to extract number based on it

# # %% [markdown]
# # # Analyze Text ,Solve Problems, and actual implement

# %%
# class OCR functions
class OCR_test:
  def __init__(self,image_path) -> None:
     self.__image_path=image_path
     #Initalize model
     self.doctr_model = ocr_predictor(det_arch='db_resnet50', reco_arch='crnn_vgg16_bn', pretrained=True)

  def __ocr_doctr(self, image_rgb):
        try:
            temp_path = "doctr_temp_file.jpg"
            cv2.imwrite(temp_path, cv2.cvtColor(image_rgb, cv2.COLOR_RGB2BGR))
            doc = DocumentFile.from_images([temp_path])
            result = self.doctr_model(doc)
            export = result.export()

            full_text = []
            all_confidences = [] 
            for page in export['pages']:
                for block in page['blocks']:
                    for line in block['lines']:
                        words_list = line['words']
                        line_text = " ".join([word['value'] for word in words_list])
                        full_text.append(line_text)
                        for word in words_list:
                            all_confidences.append(word['confidence'])

            if os.path.exists(temp_path):
                os.remove(temp_path)
            avg_confidence = (sum(all_confidences) / len(all_confidences)) if all_confidences else 0
            
            return " ".join(full_text), f"{avg_confidence:0.2f}"

        except Exception as e:
            print(f"DocTR Error: {e}")
            return "", 0
        

  def __normalize_text(self,text):
    text=re.sub(r'(\d+):(\d+)',r'\1.\2',text)
    text=re.sub(r'(\d+);(\d+)',r'\1.\2',text)
    text=text.lower()
    index=text.find("unit")
    if index!=-1:
      text=text[index+4:]
    return text
  
  def robust_ocr(self)->str:
        img_bgr = cv2.imread(self.__image_path)
        if img_bgr is None:
            return "Error: Image not found",0

        raw_rgb = cv2.resize(cv2.cvtColor(img_bgr, cv2.COLOR_BGR2RGB),
                             None, fx=2, fy=2, interpolation=cv2.INTER_CUBIC)

        d_text, confidence = self.__ocr_doctr(raw_rgb)
        d_text = self.__normalize_text(d_text)
        return d_text,confidence

# %%
# ocr_object=OCR_test("D:\\lenovo\\EMAN\\1.1.1 Graduation\\Graduation-Project\\Data Analysis & AI\\OCR\\tests_analysis\\Photos\\fbg.jpg")
# text,confidence=ocr_object.robust_ocr()
# print(text)
# print(confidence)

# %%
# Select the most inportant values in each text which
# explained in Test.txt file
class test_parameters:
  def __init__(self,test_name:str)->None:
    self.__test_name=test_name
    # self.__text=text

  def __CBC_Parameters(self)->dict:
      CBC_Parameters={
          'HB':r'(?:hemoglobin|haemoglobin|hb).*?(\d+\.?\d*)',
          'RBCs_Count': r'(?:rbc|red\s*blood\s*cell).*?(\d+\.?\d*)',
          'MCV': r'(?:mcv|mean\s*corpuscular\s*volume).*?(\d+\.?\d*)',
          'MCH': r'(?:mch(?!\s*c)).*?(\d+\.?\d*)',
          'MCHC': r'(?:mchc).*?(\d+\.?\d*)',
          'WBC': r'(?:wbc|white\s*blood\s*cell|leucocytes).*?(\d+\.?\d*)',
          'lymphocytes': r'(?:lymph|lymphocytes).*?(\d+\.?\d*)',
          'platelet_count': r'(?:platelet|plt).*?(\d+[.,]?\d*)'
      }
      return CBC_Parameters
  def __HBA1C_Parameters(self)->dict:
      HBA1C_Parameters={
          "HbA1c":r"(?:hba1c|a1c|glycosylated).*?\b(\d{1,2}(?:\.\d{1,2})?)\b\s*(?=%|normal|high|low)"
      }
      return HBA1C_Parameters
  def __FBG_Parameters(self)->dict:
      FBG_Parameters={
          "FBG": r"(?:fasting|fbs|fbg|glucose|sugar).*?\b(\d{2,3}(?:\.\d{1,2})?)\b\s*(?=mg/dl|mmol/l|normal|high|low|\s|$)"
      }
      return FBG_Parameters
  
  def __URINE_Parameters(self)->dict:
      URINE_Parameters = {
      'Color':r'(?:colour|color)\s*[:\-]?\s*(.*?yellow|.*?amber)',
      'PH':r'\bph\b\s*[:\-]?\s*(\d+(?:\.\d+)?)\b',
      'Specific_Gravity':r'(?:specific\s*gravity|gravity)\s*[:\.]?\s*(\d+(?:\.\d+)?)',
      'Protein':r'(?:protein|albumin)\s*[:\-]?\s*(negative|neg|nil|none|trace|small|moderate|large|[\+\d]+(?:\+)?|pos|present)',
      'Glucose': r'(?:glucose|sugar)\s*[:\-]?\s*(\d+\.?\d*|negative|positive|trace|neg|pos|nil|normal|small|moderate|large|\++)',
      'Nitrite': r'nitrite\s*[:\-]?\s*(negative|positive|neg\w*|pos\w*|nil|none|\++)',
      'Ketones':r'(?:ketones|acetone|bcta-hydroxybutyrate|kctoncs)[\s\w]*?[:\-]?\s*(\d+\.?\d*|negative|positive|trace|neg\w*|pos\w*|nil|none|small|moderate|large|\++)',
      'Blood': r'blood\s*[:\-\(]?\s*(negative|positive|nil|trace|pos\w*|neg\w*|\++)',
      }

      return URINE_Parameters
  def __Blood_Group_Parameters(self)->dict:
      Blood_Group_Parameters={
              'ABO_Group':r'(?:blood\s*group|abo\s*group|abo)\s*[:\-]?\s*([ABO]{1,2})',
              'RH_Factor':r'(?:rh\s*factor|rh)\s*[:\-]?\s*([\+\-]|pos|neg|positive|negative)'
          }
      return Blood_Group_Parameters
  def __HBsAg_Parameters(self)->dict:
      HBsAg_Parameters={
          'HBsAg': r'(?:hbsag|s\.antigen|hepatitis\s*b\s*surface).*?(non\s*-?\s*reactive|reactive|negative|positive|neg\w*|pos\w*)',
          # 'HBsAg_Cutoff': r'(?:cut\s*off|c\.o|s/co)\s*[:\-]?\s*(\d+\.?\d*)'
      }
      return HBsAg_Parameters
  def __HCV_Parameters(self)->dict:
      HCV_Parameters = {
    'HCV': r'(?:hcv|anti\s*-?\s*hcv|hepatitis\s*c).*?(non\s*-?\s*reactive|reactive|negative|positive|neg\w*|pos\w*)',
    # 'HCV_Cutoff': r'(?:cut\s*off|c\.o|s/co)\s*[:\-]?\s*(\d+\.?\d*)'
     }
      return HCV_Parameters
  def __TSH_Parameters(self)->dict:
    TSH_NAMES = r'tsh|s\.tsh|thyrotropin|thyroid\s*stimulating\s*hormone'

    TSH_Parameters = {
        'TSH':fr'(?:{TSH_NAMES}).*?\b(\d{{1,2}}(?:\.\d{{1,2}})?)\b',
    }
    return TSH_Parameters
  def __FERRITIN_Parameters(self)->dict:
      FERRITIN_Parameters={
          'Ferritin_value':r"(?:ferritin|s\.ferritin).*?\b(\d{1,3}(?:\.\d{1,2})?)\b\s*(?=%|ng\/ml|normal|high|low)"
      }
      return FERRITIN_Parameters

  def get_test_parameters(self)->None:
    if self.__test_name=='CBC (Complete Blood Count)':
      return self.__CBC_Parameters()
    elif self.__test_name=='Urinalysis':
      return self.__URINE_Parameters()
    elif self.__test_name=='HbA1c (Sugar Test)':
      return self.__HBA1C_Parameters()
    elif self.__test_name=='TSH (Thyroid)':
      return self.__TSH_Parameters()
    elif self.__test_name=='Ferritin':
      return self.__FERRITIN_Parameters()
    elif self.__test_name=='Blood Group':
      return self.__Blood_Group_Parameters()
    elif self.__test_name=='HBsAg (Hepatitis B)':
      return self.__HBsAg_Parameters()
    elif self.__test_name=='HCV (Hepatitis C)':
      return self.__HCV_Parameters()
    elif self.__test_name=='Fasting Blood Glucose':
      return self.__FBG_Parameters()
    


# %%
# parameter_object=test_parameters('Fasting Blood Glucose')
# parameters=parameter_object.get_test_parameters()
# print(parameters)

# %%
class extract_test_values:
    def __init__(self,ocr_text: str, parameters: dict)->None:
        self.__ocr_text=ocr_text
        self.__parameters=parameters
    
    def __extract_microscopic_results(self, text, result_1):
        text = text.lower()
        start = text.find("microscopic")
        if start == -1:
            return result_1
        section = text[start:]
        TEST_PATTERNS = {
            "RBCs": r"\b(rbcs|red\s*blood\s*cells)\b",
            "Leukocytes": r"\b(leukocytes?|pus\s*cells)\b"
        }
        VALUE_PATTERN = r"(\d+\s*-\s*\d+|\d+|nil|negative|positive|pos\w*|neg\w*|\++)"
        tests_found = []
        for test_name, pattern in TEST_PATTERNS.items():
            for match in re.finditer(pattern, section):
                tests_found.append((match.start(), test_name))

        tests_found.sort()
        ordered_tests = [t[1] for t in tests_found]
        values = re.findall(VALUE_PATTERN, section)
        result_2 = {}

        for test, value in zip(ordered_tests, values):
            result_2[test] = value.strip().upper()
        result = {**result_1, **result_2}

        return result
    
    def extract_values(self) -> dict:
        results = {}
        urine_flag=0
        for param_name, pattern in self.__parameters.items():
            if param_name=='Color':
                urine_flag=1
            match = re.search(pattern, self.__ocr_text, re.IGNORECASE)
            if match:
                if match.lastindex and match.lastindex >= 1:
                    value = match.group(1)
                    # print(f'{value}-> {type(value)}')
                    value=value.strip(".")
                    # Try to convert all numerical values to float
                    try:
                        value = float(value)
                    except ValueError:
                        # Keep as string if conversion fails (non-numerical values)
                        pass
                    
                    # Convert ABO_Group values to uppercase
                    if param_name == 'ABO_Group':
                        value = value.upper() if isinstance(value, str) else value
                
                results[param_name] = value
        if urine_flag:
            results=self.__extract_microscopic_results(self.__ocr_text,results)
        
        return results
        
    


# # %%
# extract_obj=extract_test_values(text,parameters)
# print(extract_obj.extract_values())

# # %% [markdown]
# # # Full Final Pipeline

# # %%
# image_path=input()
# test_name=input()
# ocr=OCR_test(image_path)
# text=ocr.robust_ocr()
# parameter_object=test_parameters(test_name)
# parameters=parameter_object.get_test_parameters()
# extract_obj=extract_test_values(text,parameters)
# print(extract_obj.extract_values())

# # %%
# def to_json(dictionary, filename):
#   with open(filename,'w') as fp:
#     json.dump(dictionary,fp)

# to_json(results,'results.json')

# %%



