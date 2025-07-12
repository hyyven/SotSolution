import subprocess, winreg, socket, os, sys
from mitmproxy.tools.main import mitmdump
from heartbeat import start_heartbeat_service

INTERNET_SETTINGS = winreg.OpenKey(winreg.HKEY_CURRENT_USER, r'SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings', 0, winreg.KEY_SET_VALUE | winreg.KEY_QUERY_VALUE)

def get_free_port():
	with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
		s.bind(('', 0))
		return str(s.getsockname()[1])

def enable_proxy(port):
	os.system(f"netsh winhttp set proxy 127.0.0.1:{port} > nul 2>&1")
	os.system(r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings" '
			r'/v ProxyEnable /t REG_DWORD /d 1 /f > nul 2>&1')
	os.system(r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings" '
			f'/v ProxyServer /t REG_SZ /d 127.0.0.1:{port} /f > nul 2>&1')

def disable_proxy():
	os.system("netsh winhttp reset proxy > nul 2>&1")
	os.system(r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings" '
			r'/v ProxyEnable /t REG_DWORD /d 0 /f > nul 2>&1')

if __name__ == "__main__":
	print("============================ Launching SotCapturer ============================")
	port = get_free_port()
	enable_proxy(port)

	heart_thread, monitor_thread = start_heartbeat_service()

	try:
		sys.argv = [
			"mitmdump",
			"--quiet",
			"--listen-port", port,
			"--set", "block_global=false",
			"--allow-hosts", "prod.athena.msrareservices.com",
			"-s", "mitm.py",
		]
		mitmdump()
	except Exception as e:
		print(f"Erreur lors de l'exécution de mitmdump: {e}")
	finally:
		subprocess.run(["taskkill", "/F", "/IM", "mitmdump.exe"], shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
		disable_proxy()
		heart_thread.join()
		monitor_thread.join()
