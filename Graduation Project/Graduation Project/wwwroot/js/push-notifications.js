(function () {
    'use strict';

    if (!('serviceWorker' in navigator) || !('PushManager' in window)) return;

    function urlBase64ToUint8Array(base64String) {
        var padding = '='.repeat((4 - (base64String.length % 4)) % 4);
        var base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        var rawData = atob(base64);
        var output = new Uint8Array(rawData.length);
        for (var i = 0; i < rawData.length; ++i) {
            output[i] = rawData.charCodeAt(i);
        }
        return output;
    }

    async function registerPushNotifications() {
        try {
            var reg = await navigator.serviceWorker.register('/sw.js', { scope: '/' });
            await navigator.serviceWorker.ready;

            var existingSub = await reg.pushManager.getSubscription();
            if (existingSub) {
                // Already subscribed — re-send to server in case it was cleared from DB
                await sendSubscriptionToServer(existingSub);
                return;
            }

            if (Notification.permission === 'denied') return;

            var permission = await Notification.requestPermission();
            if (permission !== 'granted') return;

            var keyResp = await fetch('/Push/VapidPublicKey');
            if (!keyResp.ok) return;
            var vapidPublicKey = await keyResp.text();

            var subscription = await reg.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
            });

            await sendSubscriptionToServer(subscription);
        } catch (err) {
            console.warn('[NABD Push] Registration failed:', err);
        }
    }

    async function sendSubscriptionToServer(subscription) {
        try {
            await fetch('/Push/Subscribe', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(subscription)
            });
        } catch (err) {
            console.warn('[NABD Push] Failed to send subscription to server:', err);
        }
    }

    // Delay slightly so page load is not blocked
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            setTimeout(registerPushNotifications, 3000);
        });
    } else {
        setTimeout(registerPushNotifications, 3000);
    }
})();
