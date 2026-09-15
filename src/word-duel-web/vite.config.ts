import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// The client always calls relative paths (/api, /hubs). In local dev, Vite
// proxies those to the API so the browser never needs cross-origin calls or
// CORS. In Docker Compose, nginx does the equivalent proxying instead.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5080',
        changeOrigin: true,
      },
      '/hubs': {
        target: 'http://localhost:5080',
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
