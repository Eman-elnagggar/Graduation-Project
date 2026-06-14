const CACHE = 'nabd-v1';
const PRECACHE = ['/images/pwa-icon-192.png', '/images/pwa-icon-512.png'];

self.addEventListener('push', event => {
    const data = event.data ? event.data.json() : {};
    const title = data.title || 'NABD نبض';
    const options = {
        body: data.body || '',
        icon: data.icon || '/images/logo.png',
        badge: data.badge || '/images/logo.png',
        data: { url: data.url || '/' },
        vibrate: [200, 100, 200],
        requireInteraction: false,
        tag: data.tag || 'nabd-notification'
    };
    event.waitUntil(self.registration.showNotification(title, options));
self.addEventListener('install', e => {
    self.skipWaiting();
    e.waitUntil(caches.open(CACHE).then(c => c.addAll(PRECACHE)));
});

self.addEventListener('notificationclick', event => {
    event.notification.close();
    const targetUrl = (event.notification.data && event.notification.data.url) || '/';
    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(clientList => {
            for (const client of clientList) {
                if ('focus' in client) return client.focus();
            }
            return clients.openWindow(targetUrl);
        })
self.addEventListener('activate', e => {
    e.waitUntil(
        caches.keys()
            .then(keys => Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k))))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', e => {
    e.respondWith(fetch(e.request).catch(() => caches.match(e.request)));
});
