# Serveur d'authentification Node.js avec MariaDB

Un serveur d'authentification simple utilisant Node.js, Express et MariaDB.

## Prérequis

- Node.js
- MariaDB installé et en cours d'exécution sur localhost
- npm (Node Package Manager)

## Installation

1. Clonez ce dépôt
2. Installez les dépendances :
```bash
npm install
```

3. Configurez les variables d'environnement :
   - Copiez le fichier `.env.example` vers `.env`
   - Modifiez les informations de connexion à la base de données dans le fichier `.env`

4. Créez une base de données MariaDB nommée `auth_db` (ou le nom que vous avez spécifié dans le fichier .env)

## Démarrage du serveur

```bash
npm start
```

Le serveur démarrera sur le port 3000 (ou le port spécifié dans le fichier .env).

## API Endpoints

### POST /register
Inscription d'un nouvel utilisateur
```json
{
    "username": "votre_username",
    "password": "votre_password"
}
```

### POST /login
Connexion d'un utilisateur
```json
{
    "username": "votre_username",
    "password": "votre_password"
}
```

## Sécurité

- Les mots de passe sont hashés avec bcrypt
- Les usernames sont uniques
- Les erreurs sont gérées de manière sécurisée 