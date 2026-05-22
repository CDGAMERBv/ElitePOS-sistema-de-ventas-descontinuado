// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // 🔧 MODO DEBUG: Cache deshabilitado temporalmente para desarrollo
    console.warn('⚠️ CACHE DESHABILITADO - Solo para debugging');

    // Fetch and cache all matching items from the assets manifest
    // const assetsRequests = self.assetsManifest.assets
    //     .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
    //     .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
    //     .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    // await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    // 🔧 MODO DEBUG: Desactivar TODA la caché temporalmente
    const DEBUG_MODE = true; // Cambiar a false para producción

    if (DEBUG_MODE) {
        console.log(`🌐 Service Worker: Bypassing cache for ${event.request.url}`);
        return fetch(event.request, {
            cache: 'no-store',
            headers: {
                'Cache-Control': 'no-cache, no-store, must-revalidate',
                'Pragma': 'no-cache'
            }
        });
    }

    // ❌ CRÍTICO: Bypass completo para Firebase y APIs - SIEMPRE red fresca
    const url = new URL(event.request.url);

    if (url.origin.includes('firestore.googleapis.com') ||
        url.origin.includes('firebase') ||
        url.origin.includes('api.') ||
        url.pathname.includes('/compras') ||
        url.pathname.includes('/ventas') ||
        url.pathname.includes('/productos') ||
        url.pathname.includes('/clientes') ||
        url.pathname.includes('/proformas') ||
        url.pathname.includes('/abonos')) {

        // Network-only - sin caché
        return fetch(event.request, {
            cache: 'no-store',
            headers: {
                'Cache-Control': 'no-cache, no-store, must-revalidate',
                'Pragma': 'no-cache'
            }
        }).catch(err => {
            console.error('❌ Error de red en API:', err);
            return new Response('{"error": "Sin conexión"}', {
                status: 503,
                headers: { 'Content-Type': 'application/json' }
            });
        });
    }

    // ✅ Cache-first para recursos estáticos
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For all navigation requests, try to serve index.html from cache,
        // unless that request is for an offline resource.
        // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
        const shouldServeIndexHtml = event.request.mode === 'navigate'
            && !manifestUrlList.some(url => url === event.request.url);

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    return cachedResponse || fetch(event.request);
}
