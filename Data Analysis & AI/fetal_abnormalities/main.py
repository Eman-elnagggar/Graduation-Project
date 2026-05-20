from ultralytics import YOLO
import os
import numpy as np
import cv2

def call_yolo_model(image_bytes):

    nparr = np.frombuffer(image_bytes, np.uint8)
    img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    
    model=YOLO("./best.pt")
    pixel_spacing_x=0.04
    pixel_spacing_y=0.04

    results_list = []
    results = model(source= img, conf=0.5, verbose=False)

    for r in results:
        masks = r.masks.data.cpu().numpy() if r.masks is not None else []
        if len(masks) == 0:
            return {
            "error": "No NT detected in this image",
            "image":None,
            "risk": "Unknown",
            "thickness_mm": 0
            }

        for mask_idx, mask in enumerate(masks):
            mask = (mask > 0.5).astype(np.uint8)
            ys, xs = np.where(mask > 0)
            if len(ys) == 0:
                return {"error": "Mask is empty",
                    "image":None,
                    "risk": "Unknown",
                    "thickness_mm": 0}


            nt_height_px = ys.max() - ys.min()
            # nt_width_px = xs.max() - xs.min()

            nt_length_mm = nt_height_px * pixel_spacing_y
            # nt_width_mm = nt_width_px * pixel_spacing_x
            # nt_area_px = np.sum(mask)
            # nt_area_mm2 = nt_area_px * pixel_spacing_x * pixel_spacing_y

            risk_flag = "High Risk" if nt_length_mm > 3.0 else "Normal"

            photo_after = r.plot()
            results_list.append({
                'error':None,
                "image": photo_after,
                "risk": risk_flag, 
                "thickness_mm": nt_length_mm,
            })
    
    return results_list[0]



    