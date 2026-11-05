import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // secure: false accepts the local ASP.NET Core dev certificate.
      '/api': { target: 'https://localhost:7199', secure: false },
    },
  },
})
