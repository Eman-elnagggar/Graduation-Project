import torch

torch.serialization.add_safe_globals([
    'ultralytics.nn.tasks.SegmentationModel',
    'ultralytics.nn.modules.head.Segment',
    'ultralytics.nn.tasks.DetectionModel'
])

from ultralytics import YOLO
import numpy as np
import cv2

def call_yolo_model(image_bytes):
    nparr = np.frombuffer(image_bytes, np.uint8)
    img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    
    if img is None:
        return {
            "error": "Invalid image format",
            "image": None,
            "risk": "Unknown",
            "ctr_percentage": 0.0
        }
    model = YOLO("./best.pt")
    results = model(source=img, conf=0.25, verbose=False)
    r = results[0]
    if r.masks is None:
        return {
            "error": "No heart or thorax detected in this fetal image",
            "image": img, 
            "risk": "Unknown",
            "ctr_percentage": 0.0
        }

    polygons = r.masks.xy
    classes = r.boxes.cls.cpu().numpy()
    
    cardiac_xs = []
    thorax_xs = []
    img_vis = img.copy() 
    
    for poly, cls_id in zip(polygons, classes):
        pts = np.array(poly, dtype=np.int32)
        
        if int(cls_id) == 0: 
            cardiac_xs.extend(pts[:, 0])
            color = (0, 0, 255) 
        else:  
            thorax_xs.extend(pts[:, 0])
            color = (255, 0, 0)  
            
        cv2.polylines(img_vis, [pts.reshape((-1, 1, 2))], isClosed=True, color=color, thickness=3)

    if len(cardiac_xs) > 0 and len(thorax_xs) > 0:
        heart_width = max(cardiac_xs) - min(cardiac_xs)
        thorax_width = max(thorax_xs) - min(thorax_xs)
        
        ctr_ratio = heart_width / thorax_width
        ctr_percentage = ctr_ratio * 100
        
        if ctr_ratio > 0.60:
            risk_flag = "CRITICAL ALERT: Severe Cardiomegaly"
        elif 0.50 < ctr_ratio <= 0.60:
            risk_flag = "WARNING: Borderline / Mild Cardiomegaly"
        else:
            risk_flag = "Normal Cardiothoracic Ratio"
            
        font = cv2.FONT_HERSHEY_SIMPLEX
        cv2.putText(img_vis, f"CTR: {ctr_percentage:.1f}%", (15, 35), font, 0.8, (255, 255, 255), 2, cv2.LINE_AA)
        
        return {
            "error": None,
            "image": img_vis,
            "risk": risk_flag,
            "ctr_percentage": round(ctr_percentage, 2)
        }
    else:
        font = cv2.FONT_HERSHEY_SIMPLEX
        cv2.putText(img_vis, "Error: Heart or Thorax missing", (15, 35), font, 0.6, (0, 0, 255), 2, cv2.LINE_AA)
        
        return {
            "error": "Could not detect both heart and thorax to calculate CTR",
            "image": img_vis,
            "risk": "Unknown",
            "ctr_percentage": 0.0
        }