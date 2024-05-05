/*
 * Original author: ProtonMail
 * Original source: https://github.com/ProtonMail/WebClients/blob/main/applications/drive/src/app/store/_downloads/fileSaver/fileSaver.ts
 * Original license: GPLv3
 * 
 * Modified by: Jack Edwards
 * Modified date: April 2024
 */

import { registerServiceWorker, openDownloadStream } from "./download";
import { saveAs } from "file-saver";
import FileMetaData from "./interfaces/fileMetaData";
import DotNetStream from "./interfaces/dotNetStream";

class FileSaver {
    
    private static _instance: FileSaver;
    public IsServiceWorkerAvailable: boolean = false;

    public async initializeAsync() {
        await registerServiceWorker()
            .then(() => this.IsServiceWorkerAvailable = true)
            .catch((error) : void => {
                this.IsServiceWorkerAvailable = false;
                console.warn('Saving file will fallback to buffered downloads:', error.message);
            });
    }

    public static getInstance(): FileSaver
    {
        return this._instance || (this._instance = new this());
    }
    
    public async saveFileAsync(streamRef: DotNetStream, fileName: string, mimeType: string, size: number | undefined) : Promise<void> {
        let metaData: FileMetaData = { name: fileName, mimeType: mimeType, size: size };
        
        if (this.IsServiceWorkerAvailable) {
            console.log("Downloading via service worker")
            await this.saveFileViaServiceWorkerAsync(streamRef, metaData);
        } else {
            console.log("Downloading via buffer")
            await this.saveFileViaBufferAsync(streamRef, metaData);
        }
    }
    
    private async saveFileViaServiceWorkerAsync(streamRef: DotNetStream, metaData: FileMetaData) : Promise<void> {
        const saveStream: WritableStream<Uint8Array> = await openDownloadStream(metaData);

        try {
            const stream = await streamRef.stream();
            await stream.pipeTo(saveStream, { preventCancel: true});
        } catch (e) {
            console.warn('Failed to download via stream');
            throw e;
        }
    }

    private async saveFileViaBufferAsync(streamRef: DotNetStream, metaData: FileMetaData) : Promise<void> {
        if (this.isFileSaverSupported()) {
            const buffer = await streamRef.arrayBuffer();
            saveAs(new Blob([buffer], { type: metaData.mimeType }), metaData.name);
        } else {
            throw new Error("Saving via blob is not supported on this browser.");
        }
    }
    
    private isFileSaverSupported() : boolean {
        try {
            const test: boolean = !!new Blob;
            return true;
        } catch (e) {
            console.error(e);
            return false;
        }
    }
}

export async function initializeAsync() : Promise<void> {
    let thisInstance: FileSaver = FileSaver.getInstance();
    await thisInstance.initializeAsync();
}

export function browserSupportsStreamingDownloads(): boolean {
    let thisInstance: FileSaver = FileSaver.getInstance();
    return thisInstance.IsServiceWorkerAvailable;
}

export async function saveFileAsync(streamRef: DotNetStream, fileName: string, mimeType: string, size: number | undefined) : Promise<void> {
    let thisInstance: FileSaver = FileSaver.getInstance();
    await thisInstance.saveFileAsync(streamRef, fileName, mimeType, size);
}
