import pipeline as model
import comapre_ingredients as compare
from display import display
import os

def run_ocr(image_path):
    if not os.path.exists(image_path):
        return {"error":"File not found"}
    
    print("Loading...")

    ocr_object=model.product_ocr(image_path)
    text=ocr_object.robust_ocr()
    # print(text)
    if text=="":
        return [None],[None]
    
    comapre_object=compare.compare_ingredients(text)
    founded_ingredients=comapre_object.analyze_ingredients()
    status,explained_text=display(founded_ingredients)
    return status,explained_text


if __name__=='__main__':
    image_path=input("Path: ")
    # print(run_ocr(image_path))
