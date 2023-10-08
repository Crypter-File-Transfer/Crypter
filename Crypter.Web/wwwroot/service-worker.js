self.pendingDownloads = new []();

self.addEventListener('install', (event) => {
    console.log('Service worker installed');
});

self.addEventListener('activate', (event) => {
    console.log('Service worker activated');
});

self.addEventListener('fetch', function (event) {
    var url = new URL(event.request.url);

    // Define the URL that needs to be intercepted
    if (url.pathname === '/blob/download') {
        event.respondWith(
            fetch(event.request)
                .then(function (response) {
                    // Create a new Response object with the necessary headers to allow file download
                    var headers = new Headers(response.headers);
                    headers.append('Content-Disposition', 'attachment');
                    headers.append('Access-Control-Expose-Headers', 'Content-Disposition');

                    var newResponse = new Response(response.body, {
                        status: response.status,
                        statusText: response.statusText,
                        headers: headers
                    });

                    return newResponse;
                })
        );
    }
});