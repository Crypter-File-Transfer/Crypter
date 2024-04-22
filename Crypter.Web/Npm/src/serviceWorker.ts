import FileMetaData from "./interfaces/fileMetaData";

interface DownloadConfig {
    stream: ReadableStream<Uint8Array>;
    port: MessagePort;
    filename: string;
    mimeType: string;
    size?: number;
}

const SECURITY_HEADERS = {
    'Content-Security-Policy': "default-src 'none'",
    'X-Content-Security-Policy': "default-src 'none'",
    'X-WebKit-CSP': "default-src 'none'",
    'Referrer-Policy': 'strict-origin-when-cross-origin',
    'Strict-Transport-Security': 'max-age=31536000',
    'X-Content-Type-Options': 'nosniff',
    'X-Frame-Options': 'deny',
    'X-XSS-Protection': '1; mode=block',
    'X-Permitted-Cross-Domain-Policies': 'none',
};

let pendingDownloads = new Map<string, DownloadConfig>();
let downloadId = 1;

const generateUID = (): number => {
    if (downloadId > 9000) {
        downloadId = 0;
    }
    return downloadId++;
};

function createDownloadStream(port: MessagePort) {
    return new ReadableStream({
        start(controller: ReadableStreamDefaultController) {
            port.onmessage = ({ data }) => {
                switch (data?.action) {
                    case 'end':
                        return controller.close();
                    case 'download_chunk':
                        return controller.enqueue(data?.payload);
                    case 'abort':
                        return controller.error(data?.reason);
                    default:
                        console.error(`received unknown action "${data?.action}"`);
                }
            };
        },
        cancel() {
            port.postMessage({ action: 'download_canceled' });
        },
    });
}

self.addEventListener('install', (event) => {
    void (self as any).skipWaiting();
});

self.addEventListener('activate', (event) => {
    (event as any).waitUntil((self as any).clients.claim());
});

self.addEventListener('fetch', (event : any) => {
    const url = new URL(event.request.url);

    if (!url.pathname.startsWith('/sw')) {
        return;
    }

    if (url.pathname.endsWith('/sw/ping')) {
        return event.respondWith(new Response('pong', {
            headers: new Headers(SECURITY_HEADERS)
        }));
    }

    try {
        const chunks = url.pathname.split('/').filter((item) => !!item);
        const id = chunks[chunks.length - 1];

        const pendingDownload = pendingDownloads.get(id.toString());
        if (!pendingDownload) {
            return event.respondWith(
                new Response(undefined, {
                    status: 404,
                    headers: new Headers(SECURITY_HEADERS)
                })
            );
        }

        const { stream, filename, size, mimeType } = pendingDownload;

        pendingDownloads.delete(id.toString());

        const headers = new Headers({
            ...(size ? { 'Content-Length': `${size}` } : {}),
            'Content-Type': mimeType,
            'Content-Disposition': `attachment; filename="${encodeURIComponent(filename)}"`,
            ...SECURITY_HEADERS,
        });

        event.respondWith(new Response(stream, { headers }));
    } catch (e) {
        event.respondWith(new Response((e as Error).message, {
            status: 500,
            headers : new Headers(SECURITY_HEADERS)
        }));
    }
});

self.addEventListener('message', (event) => {
    if (event.data?.action !== 'start_download') {
        return;
    }
    
    const id = generateUID();
    const { name, mimeType, size } = event.data.payload as FileMetaData;
    const downloadUrl = new URL(`/sw/${id}`, (self as any).registration.scope);

    const port = event.ports[0];
    
    pendingDownloads.set(`${id}`, {
        stream: createDownloadStream(port),
        filename: name,
        mimeType,
        size,
        port,
    });

    port.postMessage({ action: 'download_started', payload: downloadUrl.toString() });
});
