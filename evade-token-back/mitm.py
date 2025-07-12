import os
import time
import asyncio
import win32gui
import win32con
import subprocess
import psutil
from mitmproxy import http
from shared_state import state
from api_client import API_CLIENT

last_token = None
WEBHOOK_URL = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"

def find_process_by_name(name):
    for process in psutil.process_iter(['name']):
        if process.info['name'] and name.lower() in process.info['name'].lower():
            return process
    return None

async def close_steam():
    try:
        steam_running = False
        steam_process = find_process_by_name('steam')
        if steam_process:
            steam_running = True
            subprocess.call(["C:\\Program Files (x86)\\Steam\\steam.exe", "-shutdown"])
            while steam_running:
                print("Attente de la fermeture de Steam...")
                await asyncio.sleep(3)
                steam_process = find_process_by_name('steam')
                if steam_process:
                    steam_running = True
                else:
                    steam_running = False
            print("Steam a été fermé avec succès")
        else:
            print("Steam n'est pas en cours d'exécution")
            steam_running = False
    except Exception as e:
        print(f"Erreur lors de la fermeture de Steam: {e}")
        return False
    return steam_running

def close_window_by_title(window_title):
    def enum_windows_callback(hwnd, results):
        if win32gui.IsWindowVisible(hwnd):
            if window_title.lower() in win32gui.GetWindowText(hwnd).lower():
                results.append(hwnd)
        return True

    window_handles = []
    win32gui.EnumWindows(enum_windows_callback, window_handles)
    if not window_handles:
        print(f"Aucune fenêtre avec le titre '{window_title}' n'a été trouvée.")
        return False
    hwnd_to_close = window_handles[0]
    try:
        print(f"Tentative de fermeture de la fenêtre: {win32gui.GetWindowText(hwnd_to_close)} (HWND: {hwnd_to_close})")
        win32gui.PostMessage(hwnd_to_close, win32con.WM_CLOSE, 0, 0)
        i = 0
        while win32gui.IsWindow(hwnd_to_close) and i < 10:
            print(f"Attente de la fermeture de la fenêtre {hwnd_to_close}... Tentative {i+1}/10")
            i += 1
            time.sleep(5)
            if win32gui.IsWindow(hwnd_to_close):
                print(f"La fenêtre (HWND: {hwnd_to_close}) n'a pas été fermée après 5 secondes.")
                win32gui.PostMessage(hwnd_to_close, win32con.WM_CLOSE, 0, 0)
            else:
                print(f"La fenêtre (HWND: {hwnd_to_close}) a été fermée avec succès.")
                return True
        print(f"timeout lors de la fermeture de la fenêtre (HWND: {hwnd_to_close}) après 10 tentatives.")
        return False
    except win32gui.error as e:
        print(f"Erreur Win32 lors de la tentative de fermeture de la fenêtre (HWND: {hwnd_to_close}): {e}")
        return False
    except Exception as e:
        print(f"Erreur inattendue lors de la fermeture de la fenêtre (HWND: {hwnd_to_close}): {e}")
        return False

def launch_sot():
    try:
        steam_path = r"C:\Program Files (x86)\Steam\steam.exe"
        if os.path.exists(steam_path):
            subprocess.Popen([steam_path, "steam://rungameid/1172620"])
            print("Sea of Thieves lancé via Steam")
            return True
        else:
            print("Steam n'est pas installé ou le chemin est incorrect")
    except Exception as e:
        print(f"Erreur lors du lancement: {e}")
        return False

def is_game_running():
    def enum_windows_callback(hwnd, results):
        if win32gui.IsWindowVisible(hwnd):
            if "Sea of Thieves" in win32gui.GetWindowText(hwnd):
                results.append(hwnd)
        return True
    
    window_handles = []
    win32gui.EnumWindows(enum_windows_callback, window_handles)
    return len(window_handles) > 0

async def wait_for_game_to_close(max_wait=50):
    start_time = time.time()
    while time.time() - start_time < max_wait:
        if not is_game_running():
            return True
        await asyncio.sleep(0.5)
    print("Timeout en attendant la fermeture du jeu")
    return False

async def wait_for_game_to_launch(max_wait=50):
    start_time = time.time()
    while time.time() - start_time < max_wait:
        if is_game_running():
            return True
        await asyncio.sleep(0.5)
    print("Timeout en attendant le lancement du jeu")
    return False

async def send_to_api(token):
    try:
        API_CLIENT.push_rare_data(token)
        print("Token envoyé à l'API")
        return True
    except Exception as e:
        print(f"Erreur API: {e}")
        return False

async def request(flow: http.HTTPFlow) -> None:
    global last_token
    if "/login/api/token/client" in flow.request.pretty_url:
        token = None
        try:
            for header_name, value in flow.request.headers.items():
                if header_name.lower() == "x-rare-data":
                    token = value
                    break
        except Exception as e:
            print(f"Erreur lors de l'analyse de la requête: {e}")
            pass

        if token and token != last_token:
            last_token = token
            print("============================ TOKEN CAPTURÉ ============================")
            
            for attempt in range(3):
                if await send_to_api(token):
                    state.api_sent = True
                    break
                else:
                    print(f"Erreur API (tentative {attempt + 1}/3)")
                await asyncio.sleep(2)
            
            if not state.api_sent:
                print("Échec de l'envoi à l'API après 3 tentatives")
                return
            await asyncio.sleep(2)
            
            # if close_window_by_title("Sea of Thieves"):
                # await close_steam()
            # else:
            #     print("Impossible de fermer le jeu")
            await close_steam()
            state.api_sent = False
            
            print("=========================== ATTENTE DE 600s ===========================")
            await asyncio.sleep(1)
            
            if launch_sot():
                await wait_for_game_to_launch()
            else:
                print("Échec du relancement du jeu")

def response(flow: http.HTTPFlow) -> None:
    pass

launch_sot()
