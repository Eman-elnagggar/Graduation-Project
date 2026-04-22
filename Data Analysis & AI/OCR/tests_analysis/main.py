import tests_ocr_script as ocr
import completness
import os
# Final pipeline

def run_ocr(image_path,test_name):
    # image_path=input("Path: ")
    # test_name=input("Test name: ")
    if not os.path.exists(image_path):
        return {"error":"File not found"}
    
    print("Loading...")
    ocr_object=ocr.OCR_test(image_path)
    text, confidence=ocr_object.robust_ocr()
    dict_1={"test_name":test_name,"confidence":confidence}
    parameter_object=ocr.test_parameters(test_name)
    parameters=parameter_object.get_test_parameters()
    values_object=ocr.extract_test_values(text,parameters)
    values=values_object.extract_values()
    print("Check Completness..")
    values=completness.complete_data(test_name,values)
    Final_data = {**dict_1, **values}
    return Final_data
    # ocr.to_json(values,'results.json')
# print(values)

if __name__=="__main__":
    image_path = input("Path: ")
    test_name = input("Test name: ")
    # print(run_ocr(image_path,test_name))