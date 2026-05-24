import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [react()],
    base: './', // This tells Vite to use relative paths for the embedded UI
    build: {
        outDir: 'dist',
        chunkSizeWarningLimit: 800,
    }
});