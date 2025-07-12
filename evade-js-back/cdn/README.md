# CDN avec Authentification et Vérification d'IP

Ce système permet de créer un CDN sécurisé qui vérifie l'authentification des utilisateurs et leurs adresses IP avant de donner accès au fichier EvadeLoader.exe.

## Configuration

1. Créez la base de données et la table en exécutant le fichier `init.sql`
2. Configurez le fichier `.env` avec vos informations :
   - DB_HOST : l'hôte de votre base de données
   - DB_USER : l'utilisateur de la base de données
   - DB_PASSWORD : le mot de passe de la base de données
   - JWT_SECRET : une chaîne secrète pour la génération des tokens
   - PORT : le port sur lequel le serveur écoutera

## Installation

```bash
npm install
```

## Démarrage du serveur

```bash
node server.js
```

## Utilisation

1. Authentification :
   ```bash
   POST http://localhost:3000/login
   Content-Type: application/json
   
   {
     "username": "votre_username",
     "password": "votre_password"
   }
   ```

2. Téléchargement du fichier :
   ```bash
   GET http://localhost:3000/download/loader
   Authorization: Bearer votre_token_jwt
   ```

## Structure des dossiers

- `/public` : Dossier contenant les fichiers à servir via le CDN
- `server.js` : Fichier principal du serveur
- `init.sql` : Script d'initialisation de la base de données

## Sécurité

- Vérification du token JWT pour chaque requête
- Vérification de l'IP de l'utilisateur
- Hachage des mots de passe avec bcrypt
- Protection CORS activée
- Accès limité au fichier spécifique 