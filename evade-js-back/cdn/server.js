require('dotenv').config();
const express = require('express');
const mysql = require('mysql2');
const jwt = require('jsonwebtoken');
const cors = require('cors');
const path = require('path');
const fetch = require('node-fetch');

const app = express();

// Middleware
app.use(cors({
    origin: ['http://localhost:2798', 'http://localhost:3780', '*'],
    credentials: true
}));
app.use(express.json());

// Configuration de la base de données
const db = mysql.createPool({
    host: process.env.DB_HOST,
    user: process.env.DB_USER,
    password: process.env.DB_PASSWORD,
    database: process.env.DB_DATABASE
});

// Fonction pour obtenir l'IP réelle du client
const getRealIP = (req) => {
    // Essayer d'abord X-Forwarded-For (PRIORITÉ MAXIMALE)
    const forwarded = req.headers['x-forwarded-for'];
    if (forwarded) {
        // Prendre la première IP dans la liste
        const ips = forwarded.split(',');
        console.log('Using x-forwarded-for IP:', ips[0].trim());
        return ips[0].trim();
    }
    
    // Essayer X-Real-IP
    const realIP = req.headers['x-real-ip'];
    if (realIP) {
        console.log('Using x-real-ip IP:', realIP);
        return realIP;
    }
    
    // Si aucun en-tête proxy n'est trouvé, utiliser l'IP directe
    const ip = req.ip.replace(/^::ffff:/, '');
    
    // Éviter d'utiliser les IPs locales
    if (ip === '::1' || ip === '127.0.0.1') {
        console.log('Local IP detected, checking for actual client IP in headers...');
        // Dernière tentative de récupérer une IP non-locale depuis les headers
        for (const header in req.headers) {
            if (header.toLowerCase().includes('ip') || header.toLowerCase().includes('forwarded')) {
                console.log(`Found possible IP in header ${header}:`, req.headers[header]);
                if (req.headers[header] && !req.headers[header].includes('127.0.0.1') && !req.headers[header].includes('::1')) {
                    return req.headers[header];
                }
            }
        }
        
        // Si vraiment aucune IP externe n'est trouvée
        console.log('No external IP found, using', ip);
    }
    
    console.log('Using direct IP:', ip);
    return ip;
};

// Middleware pour vérifier le token JWT
const authenticateToken = (req, res, next) => {
    const authHeader = req.headers['authorization'];
    const token = authHeader && authHeader.split(' ')[1];

    if (!token) {
        return res.status(401).json({ error: 'Token manquant' });
    }

    jwt.verify(token, process.env.JWT_SECRET, (err, user) => {
        if (err) {
            return res.status(403).json({ error: 'Token invalide' });
        }
        req.user = user;
        next();
    });
};

// Route de login
app.post('/login', async (req, res) => {
    const { username, password } = req.body;
    
    // Afficher tous les en-têtes HTTP pour déboguer
    console.log('All HTTP headers:');
    for (const header in req.headers) {
        console.log(`${header}: ${req.headers[header]}`);
    }
    
    const clientIP = getRealIP(req);

    console.log('Login attempt:', { 
        username, 
        clientIP,
        headers: {
            'x-forwarded-for': req.headers['x-forwarded-for'],
            'x-real-ip': req.headers['x-real-ip']
        }
    });

    // Vérifier d'abord auprès du serveur d'A2F
    try {
        const authResponse = await fetch('http://localhost:3780/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ 
                username, 
                password,
                ip_address: clientIP // Transmettre l'IP au serveur d'A2F
            })
        });
        
        // Si le serveur A2F répond avec un code 403 et requiresVerification: true
        if (authResponse.status === 403) {
            const authData = await authResponse.json();
            if (authData.requiresVerification === true) {
                // Transmettre la réponse d'A2F au client
                console.log('A2F verification required');
                return res.status(403).json(authData);
            }
        } else if (authResponse.ok) {
            // Si l'authentification A2F est réussie, on peut retourner son résultat
            const authData = await authResponse.json();
            console.log('A2F authentication successful');
            return res.json(authData);
        }
        
        // Si l'authentification A2F a échoué mais pas pour des raisons de vérification,
        // continuer avec l'authentification normale
        console.log('Proceeding with normal authentication');
    } catch (error) {
        console.log('A2F check failed, continuing with normal authentication:', error.message);
        // Continuer avec l'authentification normale en cas d'erreur
    }

    const query = `
        SELECT * FROM users 
        WHERE username = ? 
        AND password = ?
        AND ip_address = ? 
        AND expiration_date > NOW()
    `;
    
    try {
        const [rows] = await db.promise().query(query, [username, password, clientIP]);
        console.log('Query parameters:', { username, password, clientIP });
        console.log('Query result:', rows);
        
        if (rows.length === 0) {
            // Vérifier si l'utilisateur existe avec ce nom/mot de passe mais une IP différente
            const userQuery = `
                SELECT * FROM users 
                WHERE username = ? 
                AND password = ?
                AND expiration_date > NOW()
            `;
            
            const [userRows] = await db.promise().query(userQuery, [username, password]);
            
            if (userRows.length > 0) {
                // L'utilisateur existe mais avec une IP différente
                // Cela devrait normalement être géré par le système d'A2F
                // Mais au cas où l'A2F a échoué, on renvoie une erreur explicite
                return res.status(403).json({ 
                    error: 'IP address not authorized',
                    requiresVerification: true
                });
            }
            
            return res.status(401).json({ error: 'Access Denied' });
        }

        const user = rows[0];

        // Inclure la date d'expiration dans le token
        const token = jwt.sign(
            { 
                id: user.id, 
                username: user.username,
                expiration_date: user.expiration_date
            },
            process.env.JWT_SECRET,
            { expiresIn: '24h' }
        );

        // Renvoyer les informations importantes
        res.json({ 
            token,
            expiration_date: user.expiration_date
        });
    } catch (error) {
        console.error('Database error:', error);
        res.status(500).json({ error: 'Connection Failed' });
    }
});

// Route protégée pour accéder au fichier EvadeLoader.exe
app.get('/download/loader', authenticateToken, async (req, res) => {
    try {
        // Vérifier si l'utilisateur existe toujours et n'est pas expiré
        const [rows] = await db.promise().query(
            'SELECT * FROM users WHERE id = ? AND expiration_date > NOW()',
            [req.user.id]
        );

        if (rows.length === 0) {
            return res.status(403).json({ error: 'License Expired' });
        }

        const filePath = '/home/ubuntu/file/SoTSolutionLoader.exe';
        
        res.download(filePath, 'EvadeLoader.exe', (err) => {
            if (err) {
                console.error('Download error:', err);
                return res.status(404).json({ error: 'Download Failed' });
            }
        });
    } catch (error) {
        console.error('Download verification error:', error);
        res.status(500).json({ error: 'Connection Failed' });
    }
});

// Route proxy pour vérifier le code 2FA
app.post('/verify-2fa', async (req, res) => {
    const { username, code } = req.body;
    
    console.log('Verify 2FA attempt:', { username, code });
    
    // Obtenir la vraie IP du client
    const clientIP = getRealIP(req);
    console.log('Client IP for 2FA verification:', clientIP);
    
    if (!username || !code) {
        return res.status(400).json({ error: 'Username and code are required' });
    }
    
    try {
        console.log('Sending request to A2F server with IP:', clientIP);
        
        // Transférer la demande au serveur d'A2F avec l'IP du client
        const authResponse = await fetch('http://localhost:3780/verify-2fa', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Forwarded-For': clientIP,
                'X-Real-Client-IP': clientIP,
                'X-Original-IP': clientIP,
                'X-Client-IP': clientIP,
                'X-User-IP': clientIP
            },
            body: JSON.stringify({ 
                username, 
                code,
                client_ip: clientIP,
                user_ip: clientIP,
                real_ip: clientIP,
                forwarded_ip: clientIP,
                original_ip: clientIP
            })
        });
        
        console.log('A2F server response status:', authResponse.status);
        
        // Récupérer la réponse du serveur d'A2F
        const authData = await authResponse.json();
        console.log('A2F server response data:', authData);
        
        // Si la vérification 2FA a réussi (status 200), mettre à jour l'IP du client dans la base de données
        if (authResponse.status === 200 && authData.verified === true) {
            console.log('2FA verification successful! Updating IP address in the database...');
            try {
                // Mettre à jour l'IP dans la base de données
                const [result] = await db.promise().query(
                    'UPDATE users SET ip_address = ? WHERE username = ?',
                    [clientIP, username]
                );
                
                if (result.affectedRows > 0) {
                    console.log(`✅ Database updated successfully. IP address for user ${username} set to ${clientIP}`);
                } else {
                    console.log(`⚠️ No user found with username ${username} when trying to update IP`);
                }
            } catch (dbError) {
                console.error('Error updating IP in database:', dbError);
                // On continue même si la mise à jour de l'IP échoue
            }
        }
        
        // Retourner la réponse au client avec le même code d'état
        return res.status(authResponse.status).json(authData);
    } catch (error) {
        console.error('Error verifying 2FA:', error);
        return res.status(500).json({ error: 'Error verifying 2FA' });
    }
});

// Servir les fichiers statiques
app.use(express.static('public'));

// Route pour la page de téléchargement
app.get('/download.html', (req, res) => {
    res.sendFile(path.join(__dirname, 'public', 'download.html'));
});

// Route pour la page de vérification 2FA
app.get('/2fa', (req, res) => {
    res.sendFile(path.join(__dirname, 'public', '2fa.html'));
});

// Route par défaut
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

const PORT = process.env.PORT || 2798;
app.listen(PORT, () => {
    console.log(`Server started on port ${PORT}`);
}); 