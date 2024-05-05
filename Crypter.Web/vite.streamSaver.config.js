import path from 'path';
import compression from 'vite-plugin-compression2';
import { defineConfig } from 'vite';

export default defineConfig({
    build: {
        target: 'esnext',
        minify: false,
        lib: {
            entry: path.resolve(__dirname, 'Npm/src/streamSaver/streamSaver.ts'),
            formats: ['es'],
            fileName: 'streamSaver.bundle',
        },
        outDir: path.resolve(__dirname, 'wwwroot/js/dist/streamSaver'),
        sourcemap: false,
        rollupOptions: {
            external: [
                "Npm/src/serviceWorker/serviceWorker.ts"
            ],
        }
    }
});