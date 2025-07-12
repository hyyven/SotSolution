# Serveur de Données Rares

Un serveur Node.js léger pour stocker et servir des données rares.

## Installation

1. Installez les dépendances :
```bash
npm install
```

2. Démarrez le serveur :
```bash
npm start
```

Le serveur démarrera sur le port 3000.

## Endpoints

### GET /get-rare
Récupère toutes les données rares stockées.

### POST /push-rare
Ajoute de nouvelles données rares. Requiert une authentification via token.

Pour utiliser l'endpoint `/push-rare`, vous devez inclure un header `x-auth-token` avec le hash SHA-256 du chemin de la clé.

## Exemple d'utilisation

Pour obtenir les données :
```bash
curl http://localhost:3000/get-rare
```

Pour ajouter des données (avec token) :
```bash
curl -X POST http://localhost:3000/push-rare \
  -H "Content-Type: application/json" \
  -H "x-auth-token: VOTRE_TOKEN" \
  -d '{"votre": "données"}'
``` 