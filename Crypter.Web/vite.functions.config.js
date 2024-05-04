import path from 'path';
import compression from 'vite-plugin-compression2';
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
    },
    plugins: [
        compression({
            algorithm: 'gzip',
            ext: /\.(js|css|html|svg)$/,
            exclude: [/\.(br)$/, /\.(gz)$/]
        }),
        compression({
            algorithm: 'brotliCompress',
            ext: /\.(js|css|html|svg)$/,
            exclude: [/\.(br)$/, /\.(gz)$/],
            options: { level: 11 }
        })
    ],
});