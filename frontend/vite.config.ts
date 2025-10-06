import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())
  const target = env.VITE_API_PROXY_TARGET || 'http://127.0.0.1:5000'

  return {
    plugins: [vue()],
    server: {
      host: env.VITE_DEV_HOST || true,
      port: Number(env.VITE_DEV_PORT || 5173),
      proxy: {
        '/api': {
          target,
          changeOrigin: true,
          rewrite: p => p.replace(/^\/api/, '')
        }
      }
    }
  }
})
