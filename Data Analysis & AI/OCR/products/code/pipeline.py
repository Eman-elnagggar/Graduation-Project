import cv2
import os
from doctr.models import ocr_predictor
from doctr.io import DocumentFile

class product_ocr:
    def __init__(self,image_path):
        self.__image_path=image_path
        self.__model=ocr_predictor(det_arch='db_resnet50', reco_arch='crnn_vgg16_bn', pretrained=True)

    def __preprocess_image(self):
        gray=cv2.imread(self.__image_path, cv2.IMREAD_GRAYSCALE)
        raw = cv2.resize(gray, None, fx=2, fy=2, interpolation=cv2.INTER_CUBIC)

        return raw
    
    def __ocr_doctor(self,image):
        # temp path to save numpy array and avoid type error conflict
        temp_path = "doctr_temp_file.jpg"
        cv2.imwrite(temp_path,image)
        # Standraize and normalize images
        doc = DocumentFile.from_images([temp_path])
        #Inference
        result = self.__model(doc)
        #Parsing the export
        export = result.export()
        #The reconstraction
        full_text = []
        for page in export['pages']:
           for block in page['blocks']:
               for line in block['lines']:
                   line_text = " ".join([word['value'] for word in line['words']])
                   full_text.append(line_text)

        if os.path.exists(temp_path):
           os.remove(temp_path)

        return " ".join(full_text)
    
    def __normalize_text(self,text):
        return text.lower()
    
    def robust_ocr(self):
        image = self.__preprocess_image()
        text=self.__ocr_doctor(image)
        return self.__normalize_text(text)





