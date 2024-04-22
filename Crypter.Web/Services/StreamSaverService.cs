using System;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Crypter.Web.Services;

public interface IStreamSaverService
{
    Task InitializeAsync();
    Task SaveFileAsync(Stream stream, string fileName, string mimeType, long? size);
}

public class StreamSaverService(IJSRuntime jsRuntime) : IStreamSaverService
{
    private IJSInProcessObjectReference? _moduleReference;

    public async Task InitializeAsync()
    {
        if (OperatingSystem.IsBrowser())
        {
            Console.WriteLine("initializing StreamSaverService");
            await JSHost.ImportAsync("streamSaver", "../js/dist/streamSaver/streamSaver.bundle.js");
            _moduleReference = await jsRuntime.InvokeAsync<IJSInProcessObjectReference>("import", "../js/dist/streamSaver/streamSaver.bundle.js");
            await _moduleReference.InvokeVoidAsync("init");
        }
    }

    public async Task SaveFileAsync(Stream stream, string fileName, string mimeType, long? size)
    {
        using DotNetStreamReference streamReference = new DotNetStreamReference(stream, leaveOpen: false);
        await _moduleReference!.InvokeVoidAsync("saveFile", streamReference, fileName, mimeType, size);
    }
}
