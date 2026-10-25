// Thin Google interop for GoogleDriveFileStore: signs in with Google Identity Services and reads and
// writes the one sync file in the Drive app-data folder. All sync behaviour lives in the C# core.
// Errors are thrown with a short code as the message (offline, cancelled, signin-required, ...),
// which the C# adapter turns into words for the backpacker.

const scope = 'https://www.googleapis.com/auth/drive.appdata';
const fileName = 'backpacking-tracker-sync.json';
const api = 'https://www.googleapis.com';

// The access token is held in memory only: reloading the app means signing in again.
let token = null;
let gisScript = null;

function loadGis() {
    gisScript ??= new Promise((resolve, reject) => {
        const script = Object.assign(document.createElement('script'), { src: 'https://accounts.google.com/gsi/client', async: true });
        script.onload = resolve;
        script.onerror = () => {
            gisScript = null;
            script.remove();
            reject(new Error('offline'));
        };
        document.head.append(script);
    });
    return gisScript;
}

// Loads Google's sign-in script ahead of time, so the sign-in pop-up opens straight from the tap
// (browsers block pop-ups that open too long after one).
export async function preload() {
    try {
        await loadGis();
    } catch {
        // Offline: signing in will try again.
    }
}

export function isSignedIn() {
    return token !== null && Date.now() < token.expiresAt - 60_000;
}

export async function signIn(clientId) {
    await loadGis();
    await new Promise((resolve, reject) => {
        const client = google.accounts.oauth2.initTokenClient({
            client_id: clientId,
            scope,
            callback: response => {
                if (response.error) {
                    reject(new Error(response.error));
                } else if (!google.accounts.oauth2.hasGrantedAllScopes(response, scope)) {
                    reject(new Error('scope-denied'));
                } else {
                    token = { value: response.access_token, expiresAt: Date.now() + response.expires_in * 1000 };
                    resolve();
                }
            },
            error_callback: error => reject(new Error(error.type === 'popup_closed' ? 'cancelled' : error.type)),
        });
        client.requestAccessToken();
    });
}

export function signOut() {
    token = null;
}

async function drive(path, options = {}) {
    if (!isSignedIn()) {
        throw new Error('signin-required');
    }

    let response;
    try {
        response = await fetch(api + path, { ...options, headers: { ...options.headers, Authorization: `Bearer ${token.value}` } });
    } catch {
        throw new Error('offline');
    }

    if (response.status === 401) {
        token = null;
        throw new Error('signin-required');
    }

    if (!response.ok) {
        throw new Error(`http-${response.status}`);
    }

    return response;
}

async function findFile() {
    const query = encodeURIComponent(`name = '${fileName}'`);
    const response = await drive(`/drive/v3/files?spaces=appDataFolder&q=${query}&fields=files(id,modifiedTime)&orderBy=modifiedTime desc`);
    const { files } = await response.json();
    return files[0] ?? null;
}

export async function read() {
    const file = await findFile();
    return file ? await (await drive(`/drive/v3/files/${file.id}?alt=media`)).text() : null;
}

export async function lastModified() {
    return (await findFile())?.modifiedTime ?? null;
}

export async function write(text) {
    let file = await findFile();
    if (!file) {
        const created = await drive('/drive/v3/files?fields=id', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name: fileName, parents: ['appDataFolder'] }),
        });
        file = await created.json();
    }

    await drive(`/upload/drive/v3/files/${file.id}?uploadType=media`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: text,
    });
}
