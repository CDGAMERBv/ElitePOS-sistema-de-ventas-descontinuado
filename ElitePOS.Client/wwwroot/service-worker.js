// 🚀 ElitePOS Service Worker - Optimizado para PWA
// Versión: 1.2.0
// Estrategia: Cache-First con Network Fallback

const CACHE_NAME = 'elitepos-v1.2.0';
const RUNTIME_CACHE = 'elitepos-runtime';

// 📦 Recursos críticos para caché inmediato
const PRECACHE_RESOURCES = [
    '/',
    '/index.html',
    '/css/app.css',
    '/css/bootstrap/bootstrap.min.css',
    '/manifest.webmanifest',
    '/icon-192.png',
    '/icon-512.png',
    '/favicon.png'
];

// 📚 Recursos grandes que se cachean bajo demanda
const RUNTIME_CACHE_URLS = [
    // Librerías externas
    'https://cdn.sheetjs.com/xlsx-0.20.1/package/dist/xlsx.full.min.js',
    'https://cdnjs.cloudflare.com/ajax/libs/qrcodejs/1.0.0/qrcode.min.js',
    'https://fonts.googleapis.com/css2',

    // Framework Blazor (se descarga bajo demanda)
    '/_framework/blazor.webassembly.js',
    '/_framework/dotnet.wasm',
    '/_framework/blazor.boot.json'
];

// ⚙️ Instalación del Service Worker
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                return cache.addAll(PRECACHE_RESOURCES);
            })
            .then(() => {
                return self.skipWaiting(); // Activar inmediatamente
            })
            .catch(err => {
                console.error('❌ Error durante instalación:', err);
            })
    );
});

// 🔄 Activación del Service Worker
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys()
            .then(cacheNames => {
                // Eliminar cachés antiguas
                return Promise.all(
                    cacheNames
                        .filter(name => name !== CACHE_NAME && name !== RUNTIME_CACHE)
                        .map(name => caches.delete(name))
                );
            })
            .then(() => {
                return self.clients.claim(); // Tomar control inmediato
            })
    );
});

// 📡 Estrategia de Fetch: Cache-First con Network Fallback
self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);

    // Network-first para los recursos del runtime de Blazor
    if (url.pathname.startsWith('/_framework/')) {
        event.respondWith(
            fetch(request)
                .then(networkResponse => {
                    return networkResponse;
                })
                .catch(err => {
                    console.error('❌ Error al obtener recurso de _framework:', err);
                    // Intentar servir desde cache si existe, o devolver una respuesta válida
                    return caches.match(request).then(cached => {
                        return cached || new Response(JSON.stringify({ error: 'Runtime resource unavailable' }), {
                            status: 503,
                            headers: { 'Content-Type': 'application/json' }
                        });
                    });
                })
        );
        return;
    }

    // ❌ CRÍTICO: NO cachear Firebase ni APIs externas - SIEMPRE red fresca
    if (url.origin.includes('firestore.googleapis.com') ||
        url.origin.includes('firebase') ||
        url.origin.includes('api.') ||
        url.pathname.includes('/compras') ||
        url.pathname.includes('/ventas') ||
        url.pathname.includes('/productos') ||
        url.pathname.includes('/clientes')) {

        // Network-only para Firebase y datos dinámicos
        event.respondWith(
            fetch(request, {
                cache: 'no-store', // Forzar bypass de caché del navegador
                headers: {
                    'Cache-Control': 'no-cache, no-store, must-revalidate',
                    'Pragma': 'no-cache'
                }
            }).catch(err => {
                console.error('❌ Error de red en API:', err);
                // En caso de error de red, retornar respuesta JSON válida
                return new Response(JSON.stringify({ error: 'Sin conexión' }), {
                    status: 503,
                    headers: { 'Content-Type': 'application/json' }
                });
            })
        );
        return;
    }

    // Cache-First para recursos estáticos (GET)
    if (request.method === 'GET') {
        event.respondWith(
            caches.match(request)
                .then(cachedResponse => {
                    if (cachedResponse) {
                        return cachedResponse;
                    }

                    // Si no está en caché, descargar y guardar
                    return fetch(request)
                        .then(networkResponse => {
                            // Solo cachear respuestas exitosas
                            if (!networkResponse || networkResponse.status !== 200 || networkResponse.type === 'error') {
                                return networkResponse;
                            }

                            // Clonar la respuesta (solo se puede usar una vez)
                            const responseToCache = networkResponse.clone();

                            // Guardar en caché runtime (no bloquear la respuesta)
                            caches.open(RUNTIME_CACHE)
                                .then(cache => {
                                    cache.put(request, responseToCache).catch(err => {
                                        console.warn('No se pudo guardar en cache runtime:', err);
                                    });
                                });

                            return networkResponse;
                        })
                        .catch(err => {
                            console.error('❌ Error de red:', err);
                            // Fallback: Intentar desde caché runtime, o devolver una respuesta válida si no existe
                            return caches.match(request).then(res => {
                                return res || new Response(JSON.stringify({ error: 'Unavailable' }), {
                                    status: 503,
                                    headers: { 'Content-Type': 'application/json' }
                                });
                            });
                        });
                })
        );
    }
});

// 📱 Mensaje del Service Worker
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});

