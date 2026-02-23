import pipeline as model
import comapre_ingredients as compare
from display import display
import os

def run_ocr(request_state,request_data):
    text=""
    if request_state=='image':
      if not os.path.exists(request_data):
          return {"error":"File not found"}
    
      print("Loading...")

      ocr_object=model.product_ocr(request_data)
      text=ocr_object.robust_ocr()
      # print(text)
    elif request_state=='text':
        text=request_data

    if text=="":
        return [None]
    
    comapre_object=compare.compare_ingredients(text)
    founded_ingredients=comapre_object.analyze_ingredients()
    results=display(founded_ingredients)
    return results


# if __name__=='__main__':
    # request_state,request_data=process_request(input("Request_data: "))
    # image_path=input("Path: ")
    # print(run_ocr(image_path))
