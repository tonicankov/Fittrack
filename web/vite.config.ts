import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// In development the API runs on :5080; proxying keeps everything on one origin,
// exactly like nginx does in the Docker setup.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5080',
      '/hubs': { target: 'http://localhost:5080', ws: true },
    },
  },
  test: {
    environment: 'node',
  },
})
