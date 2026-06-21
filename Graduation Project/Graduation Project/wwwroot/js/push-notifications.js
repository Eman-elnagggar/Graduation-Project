(function () {
    'use strict';

    // Service workers + Web Push require a secure context (HTTPS, or localhost).
    // On plain HTTP these APIs are absent, so push can never work there.
    var secure = (window.isSecureContext === true) ||
        location.protocol === 'https:' ||
        location.hostname === 'localhost' ||
        location.hostname === '127.0.0.1';
    var supported = secure &&
        ('serviceWorker' in navigator) && ('PushManager' in window) && ('Notification' in window);

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

    // Subscribe (and persist). Must be called after permission is granted.
    async function subscribeAndSend() {
        var reg = await navigator.serviceWorker.ready;

        var existingSub = await reg.pushManager.getSubscription();
        if (existingSub) {
            await sendSubscriptionToServer(existingSub);
            return true;
        }

        var keyResp = await fetch('/Push/VapidPublicKey');
        if (!keyResp.ok) return false;
        var vapidPublicKey = await keyResp.text();

        var subscription = await reg.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
        });

        await sendSubscriptionToServer(subscription);
        return true;
    }

    // Called from a user gesture (the "Enable notifications" button).
    async function enableFromGesture() {
        if (!supported) return 'unsupported';
        try {
            var permission = await Notification.requestPermission();
            if (permission !== 'granted') return permission; // 'denied' or 'default'
            var ok = await subscribeAndSend();
            return ok ? 'granted' : 'error';
        } catch (err) {
            console.warn('[NABD Push] Enable failed:', err);
            return 'error';
        }
    }

    // Fire a server-side test push to this user's devices.
    async function sendTest() {
        try {
            var resp = await fetch('/Push/Test', { method: 'POST' });
            if (!resp.ok) return { ok: false, count: 0 };
            var data = await resp.json();
            return { ok: true, count: (data && typeof data.subscriptions === 'number') ? data.subscriptions : 0 };
        } catch (err) {
            console.warn('[NABD Push] Test failed:', err);
            return { ok: false, count: 0 };
        }
    }

    function currentState() {
        if (!secure) return 'insecure';
        if (!supported) return 'unsupported';
        return Notification.permission; // 'granted' | 'denied' | 'default'
    }

    // Expose a small API for the notification banner UI.
    window.NabdPush = {
        supported: supported,
        state: currentState,
        enable: enableFromGesture,
        test: sendTest
    };

    // On load: register the SW. If already granted, silently (re-)subscribe.
    // We intentionally do NOT auto-prompt — the banner button drives the prompt.
    async function init() {
        if (!supported) {
            // Distinguish "served over HTTP" from "browser doesn't support push".
            var reason = (!secure) ? 'insecure' : 'unsupported';
            document.dispatchEvent(new CustomEvent('nabd:push-state', { detail: { state: reason } }));
            return;
        }
        try {
            await navigator.serviceWorker.register('/sw.js', { scope: '/' });
            if (Notification.permission === 'granted') {
                await subscribeAndSend();
            }
        } catch (err) {
            console.warn('[NABD Push] Registration failed:', err);
        }
        document.dispatchEvent(new CustomEvent('nabd:push-state', { detail: { state: currentState() } }));
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
