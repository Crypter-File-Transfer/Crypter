Blazor.start({
    environment: window.location.hostname === "localhost"
        ? "Development"
        : "Production"
});