import { initializeServiceWorker, openDownloadStream } from "./download";
import FileMetaData from "./interfaces/fileMetaData";
import DotNetStream from "./interfaces/dotNetStream";

class StreamSaver {
    
    private static _instance: StreamSaver;

    public async init() {
        await initializeServiceWorker().catch((error: any): void => {
            console.warn('Failed to initialize service worker: ', error.message);
        }).finally(() => {
            console.log("service worker initialization finished from main");
        });
    }

    public static getInstance(): StreamSaver
    {
        return this._instance || (this._instance = new this());
    }

    public async saveFile(streamRef: DotNetStream, metaData: FileMetaData) {
        const saveStream: WritableStream<Uint8Array> = await openDownloadStream(metaData);

        try {
            await new Promise((resolve, reject) => {
                streamRef.stream()
                    .then(x=> x.pipeTo(saveStream, { preventCancel: true })
                        .then(resolve)
                        .catch(reject))
                    .then(resolve)
                    .catch(reject);
            });
        } catch (e) {
            console.warn('Failed to download via stream');
            throw e;
        }
    }
}

export async function init(): Promise<void> {
    let thisInstance: StreamSaver = StreamSaver.getInstance();
    await thisInstance.init();
}

export async function saveFile(stream: DotNetStream, fileName: string, mimeType: string, size: number | undefined): Promise<boolean> {
    let thisInstance: StreamSaver = StreamSaver.getInstance();
    let metaData: FileMetaData = { name: fileName, mimeType: mimeType, size: size };
    console.log('Sending metadata to service worker');
    console.log(metaData);
    await thisInstance.saveFile(stream, metaData);
    return true;
}