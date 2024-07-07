import path from 'path';
import { defineConfig } from 'vite';

export default defineConfig({
    build: {
        target: 'esnext',
        minify: false,
        lib: {
            entry: path.resolve(__dirname, 'Npm/src/fileSaver/serviceWorker.noOp.ts'),
            formats: ['es'],
            fileName: 'serviceWorker.noOp',
        },
        outDir: path.resolve(__dirname, 'wwwroot/js/dist/serviceWorker.noOp'),
        sourcemap: false,
    }
});