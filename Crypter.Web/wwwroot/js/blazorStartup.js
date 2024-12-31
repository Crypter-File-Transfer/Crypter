const environmentMap = {
    "localhost": "Development",
    "stage.crypter.dev": "Staging",
    "www.crypter.dev": "Production"
}

Blazor.start({
    environment: environmentMap[window.location.hostname]
});