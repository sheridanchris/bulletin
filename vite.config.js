
import { defineConfig } from 'vite'

export default defineConfig({
  build: {
    commonjsOptions: {
      include: ["/node_modules"]
    }
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      }
    },
    watch: {
      ignored: ['**/dev_data/**']
    }
  }
})
