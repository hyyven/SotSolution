> **Avertissement**
> Les informations sensibles de ce projet (clés API, URLs, ports, etc.) ont été expurgées et remplacées par des `X`.

# Projet Evade — Documentation Complète

## Vue d'ensemble

Suite d'applications et de services interconnectés pour l'authentification, la distribution de contenu, la gestion de tokens et la manipulation du trafic réseau Sea of Thieves. Architecture modulaire : backends Node.js/Python, frontends C# Windows Forms.

---

## Composants du Projet

### 1. `evade-internet-website` (Vitrine Commerciale)

- **Description** : Landing page statique responsive pour la commercialisation ("SoT Solution").
- **Technologies** : HTML5, CSS3, Google Fonts.
- **Fonctionnalités** :
  - Présentation produit, fonctionnement, témoignages, FAQ.
  - Vente d’abonnements (15€/mois) via Discord.
  - Design sombre, palette bleu/cyan, animations, responsive mobile.

---

### 2. `evade-js-back` (Backends Node.js)

Monorepo Node.js : trois services indépendants.

#### a. `auth` (Service d'Authentification)

- **Dépendances** : `express`, `mariadb`, `dotenv`.
- **Fonctionnalités** :
  - Table `users` : `username`, `password`, `ip_address`, `expiration_date`.
  - `/register` : création utilisateur (`1mo`/`lifetime`), clé API, vérification IP.
  - `/login` : authentification, vérification IP, mise à jour IP si première connexion.
  - `/users` : liste des utilisateurs (clé API requise).
  - Sécurité : contrôle strict IP, clé API statique.

#### b. `cdn` (Content Delivery Network)

- **Dépendances** : `express`, `mysql2`, `jsonwebtoken`, `cors`, `node-fetch`.
- **Fonctionnalités** :
  - Auth multi-étapes : `/login` (identifiants/IP/expiration), délégation 2FA si IP inconnue.
  - 2FA : `/2fa` (UI), `/verify-2fa` (proxy vers serveur A2F), mise à jour IP après succès.
  - JWT : token avec date d’expiration.
  - `/download/loader` : téléchargement sécurisé (`EvadeLoader.exe`), JWT requis.
  - Pages statiques : `index.html`, `2fa.html`, `download.html`.
  - Gestion IP avancée (`getRealIP`), logs détaillés.

#### c. `rare-data` (Coffre-fort à Tokens)

- **Dépendances** : `express`, `mariadb`, `crypto`.
- **Fonctionnalités** :
  - Chiffrement AES-256-CBC (clé/IV codés en dur).
  - `/push-rare` : ajout token chiffré (clé API requise).
  - `/get-new-rare-again` : récupération/déchiffrement d’un token non utilisé.
  - `/delete-rare/:id` : suppression logique (clé API requise).
  - Journalisation : table `access_logs` (IP, user-agent, action).
  - Robustesse : gestion erreurs, sérialisation BigInt, endpoint test clé AES.

---

### 3. `evade-loader-front` (Chargeur d'Application)

- **Technologies** : C#, .NET 6.0, Windows Forms, WebClient, System.Text.Json.
- **Fonctionnalités** :
  - UI personnalisée : fenêtre arrondie, barre titre custom, logo dynamique, animation glow, design sombre.
  - Sélecteur produit (Evade/PlayerList), ComboBox custom.
  - Saisie/sauvegarde locale des identifiants (`authBanEvade.json`/`authPlayerlist.json`).
  - Authentification : requête API selon produit, gestion 2FA intégrée (champ code, feedback, secousse erreur).
  - Téléchargement/lancement asynchrone de l’exécutable (`evade.exe`/`playerlist.exe`), progression affichée.
  - Téléchargement dynamique logo/icône, application à la fenêtre/barre des tâches.
  - Robustesse : logs détaillés, gestion exceptions, nettoyage ressources à la fermeture.
- **Rôle** : point d’entrée utilisateur pour l’auth, la gestion des identifiants, la 2FA et le téléchargement/lancement sécurisé.

---

### 4. `evade-proxy-front` (Outil de Contournement de Bannissement)

- **Technologies** : C#, .NET, Titanium.Web.Proxy, System.Management, System.Net.Sockets.
- **Fonctionnalités** :
  - UI complète : fenêtre arrondie, barre titre custom, boutons minimiser/fermer, logo, mentions légales, bouton Discord.
  - Bouton principal "START EVADE"/"STOP EVADE" : activation/désactivation proxy/routine d’injection.
  - Affichage dynamique : pseudo, date d’expiration, état proxy, état "Player List", IP serveur, Stamp ID, XUIDs.
  - Authentification/HWID : lecture identifiants locaux, génération HWID (ProcessorId, BIOS, volume), auth backend.
  - Proxy local HTTPS (Titanium.Web.Proxy, port 8000), installation/réutilisation certificat racine, activation proxy Windows.
  - Déchiffrement SSL conditionnel (playfabapi.com, xboxlive.com).
  - Injection/substitution token : interception `/login/api/token/client`, remplacement header `X-Rare-Data` par token valide (récupéré/déchiffré AES-256-CBC).
  - Collecte/transmission XUIDs : extraction depuis `/permission/validate`, liste thread-safe, envoi périodique (1s) à serveur TCP externe, reset à chaque nouvelle connexion réseau.
  - Communication serveur TCP : connexion persistante, attente "ACCEPTED", transmission XUIDs JSON, réception/traitement messages serveur (GAMERTAGS, COLORS, MORES, commandes de téléchargement), reconnexions/timeouts automatiques.
  - Robustesse : nettoyage complet à la fermeture, logs détaillés, gestion erreurs réseau/système.
- **Rôle** : interface automatisée pour l’injection de tokens, la manipulation du trafic réseau et la collecte/transmission d’identifiants joueurs.

---

### 5. `evade-token-back` (Ferme de Capture de Tokens)

- **Technologies** : Python, `mitmproxy`, `Flask`, `discord.py`, `requests`, `psutil`, `pywin32`.
- **Fonctionnalités** :
  - Automatisation jeu : lancement Sea of Thieves via Steam, fermeture automatisée Steam/fenêtres après capture.
  - Interception trafic : `launcher.py` configure proxy système (registre + winhttp), lance `mitmdump` (`mitm.py`), n’intercepte que `prod.athena.msrareservices.com`.
  - Extraction/transmission token : `mitm.py` détecte `/login/api/token/client`, extrait header `x-rare-data`, envoie POST `/push-rare` (`rare-data`, clé API), 3 tentatives, relance jeu après succès.
  - Monitoring : `heartbeat.py` expose `/health` (Flask, port 6970), état service/temps depuis dernière capture, thread surveille `state.api_sent`, inactivité >120s = service inactif.
  - Reporting Discord : `discord_bot.py` surveille `/health`, alerte canal Discord si inactivité/erreur (mention propriétaires), message de statut "Online"/"Offline" maintenu/rafraîchi, timestamps relatifs/logs détaillés.
  - Gestion état partagé : `shared_state.py` centralise `api_sent`, `time_since_last` pour synchronisation threads/processus.
  - Robustesse : gestion erreurs réseau/système, logs détaillés, tentatives multiples, nettoyage proxy système/processus à la fermeture.
- **Rôle** : ferme automatisée de collecte de tokens, auto-surveillée, reporting temps réel, relance automatique du jeu.

---

## Architecture et Flux de Données (Contournement de Bannissement)

1. **Authentification/Lancement** : utilisateur se connecte via `evade-loader-front`, qui télécharge/lance `evade-proxy-front.exe`.
2. **Démarrage Proxy** : `evade-proxy-front` démarre.
3. **Capture Token (autre utilisateur)** : un autre processus (utilisateur non-banni) fait capturer son token par `evade-token-back`, envoyé/chiffré dans `rare-data`.
4. **Récupération Token Valide** : `evade-proxy-front` (utilisateur banni) récupère un token valide via `rare-data`.
5. **Initialisation Proxy d’Injection** : `evade-proxy-front` lance `evade-token-back` et lui fournit le token valide.
6. **Interception/Injection** : `evade-token-back` configure le proxy système, intercepte la requête d’authentification, `mitm.py` remplace le token par le token valide.
7. **Contournement** : requête modifiée validée par le serveur de jeu, utilisateur banni connecté.
8. **Interface/Contrôle** : `evade-proxy-front` gère/supervise le processus.
9. **Vitrine** : `evade-internet-website` sert de vitrine commerciale.
