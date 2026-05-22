const { Client, LocalAuth } = require('whatsapp-web.js');
const qrcodeTerminal = require('qrcode-terminal');
const qrcode = require('qrcode');
const axios = require('axios');
const express = require('express');
const cors = require('cors');

const app = express();
app.use(cors());
app.use(express.json());

const PORT = 3000;
let lastQR = null;
let connectionStatus = 'DISCONNECTED';

console.log('Iniciando el motor del Bot de ElitePOS... 🚀');

// Configuración del Cliente de WhatsApp
const client = new Client({
    authStrategy: new LocalAuth(),
    puppeteer: {
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    }
});

// Evento: Generación de QR (Sincronización inicial)
client.on('qr', async (qr) => {
    console.log('¡QR Generado, Comandante! Saca tu celular y escanea esto:');
    qrcodeTerminal.generate(qr, { small: true });
    
    // Almacenar localmente para el endpoint GET
    try {
        const qrBase64 = await qrcode.toDataURL(qr);
        lastQR = qrBase64;
        connectionStatus = 'QR_READY';

        // Sincronización proactiva con el backend C#
        await axios.post('http://localhost:5183/api/whatsapp/qr', { 
            qr: qrBase64,
            empresaId: 'default_empresa' 
        });
        console.log('✅ QR sincronizado con el backend C#.');
    } catch (error) {
        console.error('❌ Error procesando el código QR:', error.message);
    }
});

// Evento: Listo para operar
client.on('ready', async () => {
    console.log('✅ ¡ÉXITO TOTAL! El bot está conectado a WhatsApp.');
    connectionStatus = 'CONNECTED';
    lastQR = null;

    try {
        await axios.post('http://localhost:5183/api/whatsapp/status', { 
            status: 'CONNECTED',
            empresaId: 'default_empresa'
        });
        console.log('✅ Estado "Conectado" reportado al backend.');
    } catch (error) {
        console.error('❌ Error enviando estado ready al backend:', error.message);
    }
});

// Evento: Recepción de Mensajes
client.on('message', async (msg) => {
    if (msg.fromMe) return;

    try {
        console.log(`[WHATSAPP] Mensaje recibido de ${msg.from}: ${msg.body}`);
        const response = await axios.post('http://localhost:5183/api/whatsapp/mensaje', {
            sender: msg.from,
            text: msg.body
        });

        if (response.status === 200 && response.data) {
            msg.reply(response.data);
        }
    } catch (error) {
        console.error('❌ Error de conexión con el backend C#:', error.message);
    }
});

// --- ENDPOINTS PARA EL CLIENTE BLAZOR ---
app.get('/api/whatsapp/qr', (req, res) => {
    res.json({ qr: lastQR, status: connectionStatus });
});

app.get('/api/whatsapp/status', (req, res) => {
    res.json({ status: connectionStatus });
});

// Iniciar servidor del bot
app.listen(PORT, () => {
    console.log(`📡 Servidor del Bot escuchando en http://localhost:${PORT}`);
});

// Inicialización del servicio de WhatsApp
client.initialize();