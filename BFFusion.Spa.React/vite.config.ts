import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import mkcert from 'vite-plugin-mkcert'

// https://vite.dev/config/
export default defineConfig({
    plugins: [
        react(),
        mkcert()
    ],
    server: {
        host: 'localhost',
        port: 3000,
        strictPort: true,
    }
})
