> **Warning**
> Sensitive information in this project (API keys, URLs, ports, etc.) has been redacted and replaced by `X`.

# Evade Project — Complete Documentation

## Overview

Suite of interconnected applications and services for authentication, content delivery, token management, and Sea of Thieves network traffic manipulation. Modular architecture: Node.js/Python backends, C# Windows Forms frontends.

---

## Project Components

### 1. `evade-internet-website` (Commercial Landing Page)

- **Description**: Responsive static landing page for commercial purposes ("SoT Solution").
- **Technologies**: HTML5, CSS3, Google Fonts.
- **Features**:
  - Product presentation, how it works, testimonials, FAQ.
  - Subscription sales (€15/month) via Discord.
  - Dark design, blue/cyan palette, animations, mobile responsive.

---

### 2. `evade-js-back` (Node.js Backends)

Node.js monorepo: three independent services.

#### a. `auth` (Authentication Service)

- **Dependencies**: `express`, `mariadb`, `dotenv`.
- **Features**:
  - `users` table: `username`, `password`, `ip_address`, `expiration_date`.
  - `/register`: user creation (`1mo`/`lifetime`), API key, IP verification.
  - `/login`: authentication, IP verification, IP update on first login.
  - `/users`: user list (API key required).
  - Security: strict IP control, static API key.

#### b. `cdn` (Content Delivery Network)

- **Dependencies**: `express`, `mysql2`, `jsonwebtoken`, `cors`, `node-fetch`.
- **Features**:
  - Multi-step auth: `/login` (credentials/IP/expiration), 2FA delegation if unknown IP.
  - 2FA: `/2fa` (UI), `/verify-2fa` (proxy to 2FA server), IP update after success.
  - JWT: token with expiration date.
  - `/download/loader`: secure download (`EvadeLoader.exe`), JWT required.
  - Static pages: `index.html`, `2fa.html`, `download.html`.
  - Advanced IP management (`getRealIP`), detailed logs.

#### c. `rare-data` (Token Vault)

- **Dependencies**: `express`, `mariadb`, `crypto`.
- **Features**:
  - AES-256-CBC encryption (hardcoded key/IV).
  - `/push-rare`: add encrypted token (API key required).
  - `/get-new-rare-again`: retrieve/decrypt unused token.
  - `/delete-rare/:id`: logical deletion (API key required).
  - Logging: `access_logs` table (IP, user-agent, action).
  - Robustness: error handling, BigInt serialization, AES key test endpoint.

---

### 3. `evade-loader-front` (Application Loader)

- **Technologies**: C#, .NET 6.0, Windows Forms, WebClient, System.Text.Json.
- **Features**:
  - Custom UI: rounded window, custom title bar, dynamic logo, glow animation, dark design.
  - Product selector (Evade/PlayerList), custom ComboBox.
  - Local credential input/save (`authBanEvade.json`/`authPlayerlist.json`).
  - Authentication: API request per product, integrated 2FA (code field, feedback, error shake).
  - Async download/launch of executable (`evade.exe`/`playerlist.exe`), progress display.
  - Dynamic logo/icon download, applied to window/taskbar.
  - Robustness: detailed logs, exception handling, resource cleanup on close.
- **Role**: user entry point for auth, credential management, 2FA, and secure download/launch.

---

### 4. `evade-proxy-front` (Ban Evasion Tool)

- **Technologies**: C#, .NET, Titanium.Web.Proxy, System.Management, System.Net.Sockets.
- **Features**:
  - Full UI: rounded window, custom title bar, minimize/close buttons, logo, legal notices, Discord button.
  - Main button "START EVADE"/"STOP EVADE": enable/disable proxy/injection routine.
  - Dynamic display: username, expiration date, proxy state, "Player List" state, server IP, Stamp ID, XUIDs.
  - Authentication/HWID: local credential reading, HWID generation (ProcessorId, BIOS, volume), backend auth.
  - Local HTTPS proxy (Titanium.Web.Proxy, port 8000), root certificate install/reuse, Windows proxy activation.
  - Conditional SSL decryption (playfabapi.com, xboxlive.com).
  - Token injection/substitution: intercept `/login/api/token/client`, replace `X-Rare-Data` header with valid token (retrieved/decrypted AES-256-CBC).
  - XUID collection/transmission: extract from `/permission/validate`, thread-safe list, periodic send (1s) to external TCP server, reset on new network connection.
  - TCP server communication: persistent connection, wait for "ACCEPTED", transmit XUIDs JSON, receive/process server messages (GAMERTAGS, COLORS, MORES, download commands), auto-reconnect/timeouts.
  - Robustness: full cleanup on close, detailed logs, network/system error handling.
- **Role**: automated interface for token injection, network traffic manipulation, and player ID collection/transmission.

---

### 5. `evade-token-back` (Token Capture Farm)

- **Technologies**: Python, `mitmproxy`, `Flask`, `discord.py`, `requests`, `psutil`, `pywin32`.
- **Features**:
  - Game automation: launch Sea of Thieves via Steam, auto-close Steam/windows after capture.
  - Traffic interception: `launcher.py` sets up system proxy (registry + winhttp), launches `mitmdump` (`mitm.py`), only intercepts `prod.athena.msrareservices.com`.
  - Token extraction/transmission: `mitm.py` detects `/login/api/token/client`, extracts `x-rare-data` header, sends POST `/push-rare` (`rare-data`, API key), 3 attempts, relaunches game after success.
  - Monitoring: `heartbeat.py` exposes `/health` (Flask, port 6970), service state/time since last capture, thread monitors `state.api_sent`, inactivity >120s = service inactive.
  - Discord reporting: `discord_bot.py` monitors `/health`, alerts Discord channel on inactivity/error (mentions owners), "Online"/"Offline" status message maintained/refreshed, relative timestamps/detailed logs.
  - Shared state management: `shared_state.py` centralizes `api_sent`, `time_since_last` for thread/process sync.
  - Robustness: network/system error handling, detailed logs, multiple attempts, system/proc proxy cleanup on close.
- **Role**: automated token collection farm, self-monitored, real-time reporting, auto game relaunch.

---

## Architecture and Data Flow (Ban Evasion)

1. **Authentication/Launch**: user logs in via `evade-loader-front`, which downloads/launches `evade-proxy-front.exe`.
2. **Proxy Startup**: `evade-proxy-front` starts.
3. **Token Capture (other user)**: another process (non-banned user) captures their token via `evade-token-back`, sent/encrypted to `rare-data`.
4. **Valid Token Retrieval**: `evade-proxy-front` (banned user) retrieves a valid token via `rare-data`.
5. **Injection Proxy Initialization**: `evade-proxy-front` launches `evade-token-back` and provides it the valid token.
6. **Interception/Injection**: `evade-token-back` sets up system proxy, intercepts auth request, `mitm.py` replaces token with valid token.
7. **Bypass**: modified request validated by game server, banned user connected.
8. **Interface/Control**: `evade-proxy-front` manages/supervises the process.
9. **Showcase**: `evade-internet-website` serves as commercial showcase.
