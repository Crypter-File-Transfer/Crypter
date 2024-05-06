export function browserSupportsRequestStreaming(): boolean {
    let duplexAccessed = false;
    const hasContentType = new Request('https://www.crypter.dev', {
        body: new ReadableStream(),
        method: 'POST',

        // @ts-ignore
        get duplex() {
            duplexAccessed = true;
            return 'half';
        },
    }).headers.has('Content-Type');
    return duplexAccessed && !hasContentType;
}
