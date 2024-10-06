import path from 'path';
import { defineConfig } from 'vite';

export default defineConfig({
    build: {
        target: 'esnext',
        minify: false,
        lib: {
            entry: path.resolve(__dirname, 'Npm/src/functions.ts'),
            formats: ['es'],
            fileName: 'functions.bundle',
        },
        outDir: path.resolve(__dirname, 'wwwroot/js/dist'),
        emptyOutDir: false,
        sourcemap: false
    }
});