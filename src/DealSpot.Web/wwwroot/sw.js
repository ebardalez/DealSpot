// DealSpot push notification service worker
self.addEventListener('push', function (event) {
    if (!event.data) return;

    const payload = event.data.json();
    event.waitUntil(
        self.registration.showNotification(payload.title || 'DealSpot', {
            body: payload.body || '',
            icon: payload.iconUrl || '/favicon.ico',
            data: { url: payload.url || '/' }
        })
    );
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    const url = event.notification.data?.url || '/';
    event.waitUntil(clients.openWindow(url));
});
