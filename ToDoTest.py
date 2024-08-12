# -*- coding: utf-8 -*-
"""
Created on Fri Aug  9 14:19:33 2024

@author: Basem
"""
import requests

class KeycloakClient:
    def __init__(self, base_url, realm, client_id, client_secret, username, password):
        self.base_url = base_url
        self.realm = realm
        self.client_id = client_id
        self.client_secret = client_secret
        self.username = username
        self.password = password
        self.token = None

    def request_token(self):
        url = f"{self.base_url}/realms/{self.realm}/protocol/openid-connect/token"
        data = {
            'client_id': self.client_id,
            'client_secret': self.client_secret,
            'grant_type': 'password',
            'username': self.username,
            'password': self.password
        }
        headers = {
            'Content-Type': 'application/x-www-form-urlencoded'
        }

        response = requests.post(url, data=data, headers=headers)
        if response.status_code == 200:
            self.token = response.json().get('access_token')
            print("Token retrieved successfully")
        else:
            raise Exception(f"Failed to retrieve token: {response.status_code} - {response.text}")

    def send_request(self, method, api_url, json_data=None):
        if self.token is None:
            raise Exception("Token not found. Call 'get_token()' first.")

        headers = {
            'Authorization': f'Bearer {self.token}',
            'Content-Type': 'application/json'
        }

        response = requests.request(method, api_url, headers=headers, json=json_data)
        
        if response.status_code == 200:
            print("Request successful:", response.json())
        elif response.status_code == 201:
            print("Resource created:", response.json())
        elif response.status_code == 403:
             print("Unauthorized Access: 403")
        else:
            print(f"Request failed: {response.status_code} - {response.text}")

        return response
        

if __name__ == "__main__" :
    # Keycloak settings
    base_url = "http://localhost:8080"
    realm = "todorealm"
    client_id = "todo-client"
    client_secret = "ot6Jqo0jBn1i93uoMFApjHESbGZEBAsq"
    username = "foo"
    password = "password"
    
    if int(input("0 for authorised, 1 for unauthorised")):
        username = "bar"
        
    print(username)
    
    kc_client = KeycloakClient(base_url, realm, client_id, client_secret, username, password)
    
    kc_client.request_token()
    
    todo_api_url = "http://localhost:5272/api/TodoItems"
    
    for i in range(1,10):
        todo_record = {
            "name": f"Item {i}",
            "isComplete": i%2==0
        }
        
        kc_client.send_request("POST", todo_api_url, json_data=todo_record)
    
    print('\n\n\n')    
    
    kc_client.send_request("GET", todo_api_url)
    
