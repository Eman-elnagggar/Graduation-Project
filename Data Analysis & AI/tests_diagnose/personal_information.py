from flask import json


def personal_results(path):
    with open(path, "r", encoding="utf-8") as f:
            my_data_dict = json.load(f)
    personal_information=my_data_dict["personal_information"]
    return personal_information