'use strict';

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
    );
});
