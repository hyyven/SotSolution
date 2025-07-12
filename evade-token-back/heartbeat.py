import time
import threading
from flask import Flask, jsonify
from shared_state import state

app = Flask(__name__)
is_running = True
last_timestamp = time.time()

@app.route('/health', methods=['GET'])
def health_check():
    return jsonify({
        "status": "ok" if is_running else "error",
        "timestamp": last_timestamp,
        "uptime": time.time() - last_timestamp
    })

def monitor_state_changes():
    global is_running
    last_api_sent_value = None

    while is_running:
        if state.api_sent != last_api_sent_value:
            last_api_sent_value = state.api_sent
            state.time_since_last = 0
        
        state.time_since_last += 1
        time.sleep(1)
        if state.time_since_last > 120:
            is_running = False

def run_heartbeat_server():
    try:
        port = 6970
        app.run(host='127.0.0.1', port=port, debug=False)
    except Exception as e:
        print(f"[HEARTHBEAT] Error starting heartbeat service: {e}")
        global is_running
        is_running = False

def start_heartbeat_service():
    heartbeat_thread = threading.Thread(target=run_heartbeat_server, daemon=True)
    heartbeat_thread.start()
    monitor_thread = threading.Thread(target=monitor_state_changes, daemon=True)
    monitor_thread.start()
    print("[HEARTHBEAT] Heartbeat service thread started")
    return heartbeat_thread, monitor_thread