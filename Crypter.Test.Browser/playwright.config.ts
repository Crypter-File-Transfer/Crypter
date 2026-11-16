import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  timeout: 120_000,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: process.env.BASE_URL ?? 'https://localhost',
    // Caddy serves the local stack with its own internal certificate authority.
    ignoreHTTPSErrors: true,
  },
  projects: [
    {
      name: 'chromium',
      use: { browserName: 'chromium' },
    },
    {
      name: 'firefox',
      use: {
        browserName: 'firefox',
        channel: 'moz-firefox',
        // Unset, moz-firefox falls back to /snap/bin/firefox on Linux.
        launchOptions: { executablePath: process.env.FIREFOX_PATH },
      },
    },
  ],
});
