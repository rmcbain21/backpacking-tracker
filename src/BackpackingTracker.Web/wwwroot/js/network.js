// Tells AutoSyncRunner when the device goes online or offline, and when the app comes back to the
// front (a PWA left in the background isn't reloaded, so this counts as opening it again).

export function watch(runner) {
    const reportOnline = () => runner.invokeMethodAsync('SetOnline', navigator.onLine);
    window.addEventListener('online', reportOnline);
    window.addEventListener('offline', reportOnline);
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') {
            runner.invokeMethodAsync('AppResumed');
        }
    });
    return navigator.onLine;
}
