import requests, urllib3

class RareDataClient:
    def __init__(self):
        self.base_url = "http://XXX.XX.XXX.XXX:XXXX"
        self.api_key = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXx"
        self.session = requests.Session()
        self.session.verify = False
        urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
    
    def push_rare_data(self, rare_data):
        """Envoie le token Rare Data au serveur"""
        try:
            proxies = {
                "http": None,
                "https": None
            }
            response = self.session.post(
                f"{self.base_url}/push-rare",
                json={"data": rare_data},
                headers={"x-auth-token": self.api_key},
                timeout=15,
                proxies=proxies
            )
            if response.status_code == 200:
                return "Token envoyé avec succès"
            else:
                return f"Erreur serveur: {response.status_code}"
        except requests.exceptions.RequestException as e:
            print(f"Erreur réseau lors de l'envoi au serveur: {e}")
            raise
        except Exception as e:
            print(f"Erreur inattendue lors de l'envoi au serveur: {e}")
            raise
        
API_CLIENT = RareDataClient()
