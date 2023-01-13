
import { defineConfig } from 'vite'

export default defineConfig({
  root: "./src/Client",
  build: {
    outDir: "../../deploy/public",
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      }
    }
  }
})
