import path from 'path';
import compression from 'vite-plugin-compression2';
import { defineConfig } from 'vite';

export default defineConfig({
    build: {
        target: 'esnext',
        minify: false,
        lib: {
            entry: path.resolve(__dirname, 'Npm/src/streamSaver/serviceWorker.ts'),
            formats: ['es'],
            fileName: 'serviceWorker',
        },
        outDir: path.resolve(__dirname, 'wwwroot/js/dist/serviceWorker'),
        sourcemap: false,
    }
});