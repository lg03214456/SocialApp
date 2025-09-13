import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    host: '127.0.0.1',   // ✅ 強制用 127.0.0.1
    port: 5173,          // 看你原本用哪個 port
    proxy: {
      
      // 讓前端以 /api 開頭打，實際代理到後端 http://localhost:5000
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        rewrite: p => p.replace(/^\/api/, '')
      }
    }
    }
})
