import path from 'path';
import { defineConfig } from 'vite';

export default defineConfig({
    build: {
        target: 'esnext',
        minify: false,
        lib: {
            entry: path.resolve(__dirname, 'Npm/src/fileSaver/fileSaver.ts'),
            formats: ['es'],
            fileName: 'fileSaver.bundle',
        },
        outDir: path.resolve(__dirname, 'wwwroot/js/dist/fileSaver'),
        sourcemap: false,
        rollupOptions: {
            external: [
                "Npm/src/fileSaver/serviceWorker.ts"
            ],
        }
    }
});