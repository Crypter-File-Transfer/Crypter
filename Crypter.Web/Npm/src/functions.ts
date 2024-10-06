export function browserSupportsRequestStreaming(): boolean {
    let support = false;
    if (browserImplementsRequestStreaming()) {
        // Test for Chromium bugfix: https://issues.chromium.org/issues/339788214
        const chromiumBugfixVersion = 130;
        let match: RegExpMatchArray | null = navigator.userAgent.match(/Chrom(e|ium)\/([0-9]+)\./);
        support = match
            ? parseInt(match[2], 10) > chromiumBugfixVersion
            : true; // Browser is not Chromium and is not subject to the bug
    }
    console.log(`Browser supports request streaming: ${support}`)
    return support;
}

function browserImplementsRequestStreaming() : boolean{
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
