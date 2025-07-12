const express = require('express');
const mariadb = require('mariadb');
const path = require('path');
const crypto = require('crypto');
const fs = require('fs');

const app = express();
const PORT = XXXX;

// Cl� API
const API_KEY = 'XXXXXXXXXXXXXXXXX';

// Cl� de cryptage AES (32 octets = 256 bits)
const AES_KEY = Buffer.from("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXx", "hex");

// IV (16 octets = 128 bits)
const AES_IV = Buffer.from("XXXXXXXXXXXXXXXXXXXXXXX", "utf-8");

// Configuration de la base de donn�es
const pool = mariadb.createPool({
    host: '127.0.0.1',
    user: 'XXXXXXXXXX',
    password: 'XXXXXXXXXXXXXX',
    database: 'XXXXXXXX',
    connectionLimit: 5,
    connectTimeout: 10000
});

// Middleware pour parser le JSON
app.use(express.json());

// Fonction pour crypter les donn�es avec AES
function encryptData(data) {
    const cipher = crypto.createCipheriv('aes-256-cbc', Buffer.from(AES_KEY), Buffer.from(AES_IV));
    let encrypted = cipher.update(data);
    encrypted = Buffer.concat([encrypted, cipher.final()]);
    return encrypted.toString('base64');
}

// Fonction pour d�chiffrer les donn�es avec AES
function decryptData(encryptedData) {
    const encryptedBuffer = Buffer.from(encryptedData, 'base64');
    const decipher = crypto.createDecipheriv('aes-256-cbc', Buffer.from(AES_KEY), Buffer.from(AES_IV));
    let decrypted = decipher.update(encryptedBuffer);
    decrypted = Buffer.concat([decrypted, decipher.final()]);
    return decrypted.toString();
}

// Fonction pour logger les acc�s
async function logAccess(conn, rareDataId, action, req) {
    try {
        await conn.query(
            'INSERT INTO access_logs (rare_data_id, action, ip_address, user_agent) VALUES (?, ?, ?, ?)',
            [rareDataId, action, req.ip, req.get('user-agent')]
        );
    } catch (error) {
        console.error('Erreur lors du logging:', error);
    }
}

// Fonction pour v�rifier le token
const verifyToken = (req, res, next) => {
    const token = req.headers['x-auth-token'];
    if (!token) {
        return res.status(401).json({ error: 'Token manquant' });
    }

    if (token !== API_KEY) {
        return res.status(403).json({ error: 'Token invalide' });
    }

    next();
};

// Gestionnaire personnalis� pour la s�rialisation JSON des BigInt
const bigIntSerializer = {
    stringify: (obj) => {
        return JSON.stringify(obj, (key, value) => {
            if (typeof value === 'bigint') {
                return value.toString();
            }
            return value;
        });
    }
};

// Endpoint pour obtenir la cl� de cryptage (pour les tests seulement)
app.get('/get-key', (req, res) => {
    res.json({ 
        key: AES_KEY,
        iv: AES_IV
    });
});

// Endpoint pour obtenir les donn�es rares
app.get('/get-new-rare-again', async (req, res) => {
    let conn;
    try {
        conn = await pool.getConnection();
        const rows = await conn.query(
            'SELECT id, encrypted_data, created_at FROM rare_data ORDER BY created_at DESC LIMIT 1',
            []
        );
        
        // Logger l'acc�s
        await logAccess(conn, null, 'read', req);
        
        if (rows.length === 0) {
            return res.json([]);
        }
        
        // Convertir les donn�es en base64 pour la transmission
        const rawData = rows[0].encrypted_data.toString('base64');
        
        // Crypter les donn�es avec AES avant transmission
        const encryptedData = encryptData(rawData);
        
        const formattedRow = {
            id: rows[0].id,
            data: encryptedData,
            created_at: rows[0].created_at
        };
        
        res.send(bigIntSerializer.stringify(formattedRow));
    } catch (error) {
        console.error('Erreur lors de la lecture des donn�es:', error);
        res.status(500).json({ error: 'Erreur lors de la lecture des donn�es' });
    } finally {
        if (conn) conn.release();
    }
});

// Endpoint pour ajouter des donn�es rares (prot�g� par token)
app.post('/push-rare', verifyToken, async (req, res) => {
    let conn;
    try {
        const { data } = req.body;
        
        if (!data) {
            return res.status(400).json({ error: 'Donn�es manquantes' });
        }

        // Convertir les donn�es base64 en Buffer
        const bufferData = Buffer.from(data, 'base64');
        
        conn = await pool.getConnection();
        const result = await conn.query(
            'INSERT INTO rare_data (data, encrypted_data) VALUES (?, ?)',
            [JSON.stringify({ data: data }), bufferData]
        );
        
        // Logger l'acc�s
        await logAccess(conn, result.insertId, 'write', req);
        
        res.send(bigIntSerializer.stringify({ 
            message: 'Donn�es ajout�es avec succ�s',
            id: result.insertId
        }));
    } catch (error) {
        console.error('Erreur lors de l\'ajout des donn�es:', error);
        res.status(500).json({ error: 'Erreur lors de l\'ajout des donn�es' });
    } finally {
        if (conn) conn.release();
    }
});

// Endpoint pour supprimer des donn�es (prot�g� par token)
app.delete('/delete-rare/:id', verifyToken, async (req, res) => {
    let conn;
    try {
        conn = await pool.getConnection();
        await conn.query(
            'UPDATE rare_data SET status = ? WHERE id = ?',
            ['deleted', req.params.id]
        );
        
        // Logger l'acc�s
        await logAccess(conn, req.params.id, 'delete', req);
        
        res.json({ message: 'Donn�es supprim�es avec succ�s' });
    } catch (error) {
        console.error('Erreur lors de la suppression des donn�es:', error);
        res.status(500).json({ error: 'Erreur lors de la suppression des donn�es' });
    } finally {
        if (conn) conn.release();
    }
});

// D�marrage du serveur
app.listen(PORT, () => {
    console.log(`Serveur d�marr� sur le port ${PORT}`);
    console.log("AES utilis� pour le cryptage avec cl�:", AES_KEY.toString("hex"));
    console.log("IV utilis�:", AES_IV.toString("hex"));

}); 