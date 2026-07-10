// DealSpot push notification client helper
window.dealspotPush = {
    async getVapidKey() {
        const resp = await fetch('/api/push/vapid-key');
        if (!resp.ok) return null;
        const data = await resp.json();
        return data.key;
    },

    async subscribe() {
        if (!('serviceWorker' in navigator) || !('PushManager' in window)) return false;

        const reg = await navigator.serviceWorker.register('/sw.js');
        await navigator.serviceWorker.ready;

        const vapidKey = await this.getVapidKey();
        if (!vapidKey) return false;

        const sub = await reg.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: this.urlBase64ToUint8Array(vapidKey)
        });

        const json = sub.toJSON();
        const resp = await fetch('/api/push/subscribe', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                endpoint: json.endpoint,
                p256dh: json.keys.p256dh,
                auth: json.keys.auth
            })
        });

        return resp.ok;
    },

    async unsubscribe() {
        if (!('serviceWorker' in navigator)) return false;

        const reg = await navigator.serviceWorker.getRegistration();
        if (!reg) return false;

        const sub = await reg.pushManager.getSubscription();
        if (!sub) return true;

        await fetch('/api/push/unsubscribe', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ endpoint: sub.endpoint })
        });

        await sub.unsubscribe();
        return true;
    },

    async isSubscribed() {
        if (!('serviceWorker' in navigator)) return false;
        const reg = await navigator.serviceWorker.getRegistration();
        if (!reg) return false;
        const sub = await reg.pushManager.getSubscription();
        return sub !== null;
    },

    isSupported() {
        return 'serviceWorker' in navigator && 'PushManager' in window;
    },

    urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        const raw = atob(base64);
        const arr = new Uint8Array(raw.length);
        for (let i = 0; i < raw.length; i++) arr[i] = raw.charCodeAt(i);
        return arr;
    }
};
