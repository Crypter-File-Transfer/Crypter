/*
 * Original author: ProtonMail
 * Original source: https://github.com/ProtonMail/WebClients/blob/main/applications/drive/src/app/store/_downloads/fileSaver/download.ts
 * Original license: GPLv3
 * 
 * Modified by: Jack Edwards
 * Modified date: May 2024
 */

import FileMetaData from "./interfaces/fileMetaData";
import { isEdge, isEdgeChromium, isIos, isSafari } from "../browser";

/**
 * Safari and Edge don't support returning stream as a response.
 * Safari - has everything but fails to stream a response from SW.
 * Edge - doesn't support ReadableStream() constructor, but supports it in chromium version.
 * IOS - forces all browsers to use webkit, so same problems as safari in all browsers.
 * For them download is done in-memory using blob response.
 */
export const serviceWorkerNotSupported = () =>
    !('serviceWorker' in navigator) || isSafari() || (isEdge() && !isEdgeChromium()) || isIos();

function createDownloadIframe(src: string) {
    const iframe = document.createElement('iframe');
    iframe.hidden = true;
    iframe.src = src;
    iframe.name = 'iframe';
    document.body.appendChild(iframe);
    return iframe;
}

export async function registerServiceWorker() :Promise<void> {
    if (serviceWorkerNotSupported()) {
        throw new Error('Saving file via stream is not supported by this browser');
    }

    await navigator.serviceWorker.register('/serviceWorker',{
        scope: '/'
    }).then(() => {
        console.log("Registered service worker");
    });
    
    serviceWorkerKeepAlive();
}

export async function registerNoOpServiceWorker() : Promise<void> {
    if (serviceWorkerNotSupported()) {
        return;
    }

    await navigator.serviceWorker.register('/serviceWorker.noOp',{
        scope: '/'
    }).then(() => {
        console.log("Registered no op service worker");
    });
}

export async function openDownloadStream(metaData: FileMetaData) {
    const channel = new MessageChannel();
    const stream = new WritableStream({
        write(block: Uint8Array) {
            channel.port1.postMessage({ action: 'download_chunk', payload: block });
        },
        close() {
            channel.port1.postMessage({ action: 'end' });
        },
        abort(reason) {
            channel.port1.postMessage({ action: 'abort', reason: String(reason) });
        },
    });
    
    const worker = await wakeUpServiceWorker();

    channel.port1.onmessage = ({ data }) => {
        if (data?.action === 'download_started') {
            createDownloadIframe(data.payload);
        } else {
            console.warn("Unknown data received over port1");
        }
    };

    worker.postMessage({ action: 'start_download', payload: metaData }, [channel.port2]);

    return stream;
}

function serviceWorkerKeepAlive() {
    const interval = setInterval(() => {
        wakeUpServiceWorker()
        .catch((error) => {
            console.error(error);
            clearInterval(interval)
        });
    }, 10000);
}

async function wakeUpServiceWorker() {
    const worker = navigator.serviceWorker.controller;

    if (worker) {
        worker.postMessage({ action: 'ping' });
    } else {
        const workerUrl = `${document.location.origin}/sw/ping`;
        const response = await fetch(workerUrl);
        const body = await response.text();
        if (!response.ok || body !== 'pong') {
            throw new Error('Download worker is dead');
        }
    }
    return worker as ServiceWorker;
}
