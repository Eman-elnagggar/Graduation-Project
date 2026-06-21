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

// Browsers/push services (e.g. FCM on Android) periodically rotate the subscription.
// Re-subscribe with the current VAPID key and re-send to the server so delivery survives.
self.addEventListener('pushsubscriptionchange', event => {
    event.waitUntil((async () => {
        try {
            const keyResp = await fetch('/Push/VapidPublicKey');
            if (!keyResp.ok) return;
            const vapidPublicKey = await keyResp.text();

            const padding = '='.repeat((4 - (vapidPublicKey.length % 4)) % 4);
            const base64 = (vapidPublicKey + padding).replace(/-/g, '+').replace(/_/g, '/');
            const raw = atob(base64);
            const appServerKey = new Uint8Array(raw.length);
            for (let i = 0; i < raw.length; ++i) appServerKey[i] = raw.charCodeAt(i);

            const newSub = await self.registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: appServerKey
            });

            await fetch('/Push/Subscribe', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(newSub)
            });
        } catch (err) {
            // Best effort — nothing else we can do from the SW.
        }
    })());
});
