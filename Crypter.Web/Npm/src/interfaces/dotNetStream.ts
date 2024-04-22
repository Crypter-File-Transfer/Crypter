export default interface DotNetStream {
    stream() : Promise<ReadableStream>;
    arrayBuffer() : Promise<ArrayBuffer>;
}
