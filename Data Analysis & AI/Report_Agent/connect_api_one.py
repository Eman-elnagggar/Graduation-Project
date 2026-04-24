import requests

def connect_api_one(data: dict)->dict:
    # print("connect")
    api_one_url="https://eman123yasser-internal-api-one.hf.space/submit"
    response = requests.post(api_one_url, json=data)
    # print(response.json())
    if response.status_code == 200:
        return response.json()
    else:
        raise Exception(f"API call failed with status code {response.status_code}: {response.text}")