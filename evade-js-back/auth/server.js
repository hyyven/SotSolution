require('dotenv').config();
const express = require('express');
const mariadb = require('mariadb');

const app = express();
app.use(express.json());

console.log("Lancement du serveur...");

// Configuration de la connexion à la base de données
const pool = mariadb.createPool({
    host: process.env.DB_HOST,
    user: process.env.DB_USER,
    password: process.env.DB_PASSWORD,
    database: process.env.DB_NAME,
    connectionLimit: 5
});

// Clé API autorisée
const API_KEY = "XXXXXXXXXXXXXXXXXXXX";

// Fonction pour nettoyer l'IP
function cleanIP(ip) {
    if (ip.startsWith('::ffff:')) {
        return ip.substring(7);
    }
    return ip;
}

// Création de la table users si elle n'existe pas
async function initDB() {
    console.log("Initialisation de la base de données...");
    let conn;
    try {
        conn = await pool.getConnection();
        console.log("Connexion à la base de données établie.");
        await conn.query(`
            CREATE TABLE IF NOT EXISTS users (
                id INT PRIMARY KEY AUTO_INCREMENT,
                username VARCHAR(255) UNIQUE NOT NULL,
                password VARCHAR(255) NOT NULL,
                ip_address VARCHAR(45),
                expiration_date DATETIME NOT NULL
            )
        `);
        console.log('Table users vérifiée/créée.');
    } catch (err) {
        console.error('Erreur lors de l\'initialisation de la base de données:', err);
    } finally {
        if (conn) conn.release();
    }
}

// Middleware pour vérifier l'IP
const checkIP = async (req, res, next) => {
    const clientIP = cleanIP(req.ip);
    const username = req.body.username;

    console.log(`Vérification IP - utilisateur : ${username}, IP client : ${clientIP}`);

    if (!username) {
        console.log("Aucun nom d'utilisateur fourni, passage du middleware IP.");
        return next();
    }

    try {
        const conn = await pool.getConnection();
        const rows = await conn.query(
            'SELECT ip_address FROM users WHERE username = ?',
            [username]
        );
        conn.release();

        if (rows.length === 0) {
            console.log("Utilisateur non trouvé dans la base de données.");
            return next();
        }

        const userIP = rows[0].ip_address;
        console.log(`IP enregistrée : ${userIP}`);

        if (!userIP) {
            console.log("Aucune IP enregistrée, autorisation accordée.");
            return next();
        }

        if (userIP === clientIP) {
            console.log("IP correspondante, autorisation accordée.");
            return next();
        }

        console.warn("Refus d'accès : IP non correspondante.");
        res.status(403).json({ message: 'Access denied: IP address does not match registered IP' });
    } catch (err) {
        console.error("Erreur dans le middleware IP :", err);
        next();
    }
};

// Middleware pour vérifier la clé API
const checkApiKey = (req, res, next) => {
    const apiKey = req.body.api_key;
    console.log("Vérification clé API :", apiKey);

    if (!apiKey || apiKey !== API_KEY) {
        console.warn("Clé API invalide.");
        return res.status(401).json({ message: 'Invalid API key' });
    }

    console.log("Clé API valide.");
    next();
};

// Fonction pour calculer la date d'expiration
function calculateExpirationDate(accountType) {
    const now = new Date();
    console.log("Calcul de la date d'expiration pour le type :", accountType);
    if (accountType === 'lifetime') {
        return new Date(now.getFullYear() + 100, now.getMonth(), now.getDate());
    } else if (accountType === '1mo') {
        return new Date(now.getFullYear(), now.getMonth() + 1, now.getDate());
    }
    throw new Error('Invalid account type');
}

// Route d'inscription
app.post('/register', checkIP, checkApiKey, async (req, res) => {
    const { username, password, account_type } = req.body;

    console.log("Requête d'inscription reçue :", { username, account_type });

    if (!username || !password || !account_type) {
        console.warn("Paramètres manquants pour l'inscription.");
        return res.status(400).json({ message: 'Username, password and account type are required' });
    }

    if (!['1mo', 'lifetime'].includes(account_type)) {
        console.warn("Type de compte invalide :", account_type);
        return res.status(400).json({ message: 'Account type must be either "1mo" or "lifetime"' });
    }

    try {
        const expirationDate = calculateExpirationDate(account_type);
        const conn = await pool.getConnection();

        await conn.query(
            'INSERT INTO users (username, password, expiration_date) VALUES (?, ?, ?)',
            [username, password, expirationDate]
        );

        conn.release();
        console.log(`Utilisateur ${username} inscrit avec succès.`);
        res.status(201).json({
            message: 'User registered successfully',
            expiration_date: expirationDate.toISOString(),
            account_type: account_type
        });
    } catch (err) {
        if (err.code === 'ER_DUP_ENTRY') {
            console.warn("Nom d'utilisateur déjà existant :", username);
            res.status(400).json({ message: 'Username already exists' });
        } else {
            console.error("Erreur lors de l'inscription :", err);
            res.status(500).json({ message: 'Error registering user' });
        }
    }
});

// Route de connexion
app.post('/login', checkIP, async (req, res) => {
    const { username, password } = req.body;
    console.log("Requête de connexion reçue :", { username });

    if (!username || !password) {
        console.warn("Paramètres manquants pour la connexion.");
        return res.status(400).json({ message: 'Username and password are required' });
    }

    try {
        const conn = await pool.getConnection();
        const rows = await conn.query(
            'SELECT * FROM users WHERE username = ? AND password = ?',
            [username, password]
        );
        conn.release();

        if (rows.length === 0) {
            console.warn("Identifiants invalides pour :", username);
            return res.status(401).json({ message: 'Invalid credentials' });
        }

        const user = rows[0];
        const expirationDate = new Date(user.expiration_date);

        if (expirationDate < new Date()) {
            console.warn(`Compte expiré pour ${username}.`);
            return res.status(403).json({ message: 'Account has expired' });
        }

        if (!user.ip_address) {
            const conn = await pool.getConnection();
            const clientIP = cleanIP(req.ip);
            await conn.query(
                'UPDATE users SET ip_address = ? WHERE username = ?',
                [clientIP, username]
            );
            conn.release();
            console.log(`IP ${clientIP} enregistrée pour l'utilisateur ${username}.`);
        }

        console.log(`Connexion réussie pour ${username}.`);
        res.json({
            message: 'Login successful',
            expiration_date: user.expiration_date,
            days_remaining: Math.ceil((expirationDate - new Date()) / (1000 * 60 * 60 * 24))
        });
    } catch (err) {
        console.error("Erreur lors de la connexion :", err);
        res.status(500).json({ message: 'Error logging in' });
    }
});

// Route pour voir l'IP autorisée (à des fins de test)
app.get('/authorized-ip', (req, res) => {
    console.log("Route de test /authorized-ip appelée.");
    res.json({ authorizedIP });
});

// Route pour lister tous les utilisateurs
app.get('/users', checkApiKey, async (req, res) => {
    console.log("Requête pour lister tous les utilisateurs reçue.");
    try {
        const conn = await pool.getConnection();
        const rows = await conn.query('SELECT id, username, ip_address, expiration_date FROM users');
        conn.release();

        console.log("Utilisateurs récupérés :", rows.length);
        res.json({
            users: rows,
            total: rows.length
        });
    } catch (err) {
        console.error('Erreur lors de la récupération des utilisateurs:', err);
        res.status(500).json({ message: 'Error listing users' });
    }
});

// Initialisation de la base de données au démarrage
initDB();

const PORT = process.env.PORT || XXXX;
app.listen(PORT, () => {
    console.log(`Serveur en cours d'exécution sur le port ${PORT}`);
});
