// export default api
// src/services/http.ts
import axios from 'axios'
import { useAuth } from '../stores/auth'

export const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://127.0.0.1:5000'

const api = axios.create({ baseURL: API_BASE, withCredentials: true })

let refreshing = false
let queue: Array<() => void> = []
const queueRequest = <T,>(fn: () => Promise<T>) =>
  new Promise<T>((resolve, reject) => { queue.push(async () => { try { resolve(await fn()) } catch (e) { reject(e) } }) })
const flushQueue = () => { queue.forEach(fn => fn()); queue = [] }

api.interceptors.request.use((config) => {
  const { state } = useAuth()
  if (state.accessToken) {
    config.headers = config.headers ?? {}
    config.headers.Authorization = `Bearer ${state.accessToken}`
  }
  return config
})

// api.interceptors.response.use(
//   (res) => res,
//   async (error) => {
//     const original = error.config
//     const { setAuth, clearAuth } = useAuth()
    
//     if (error?.response?.status === 401 && !original._retry) {
//       if (refreshing) return queueRequest(() => api(original))
//       original._retry = true
//       refreshing = true
//       try {
//         const { data } = await api.post('/auth/r+efresh', null) // refresh 在 Cookie
//         setAuth(data.accessToken, data.user ?? null)
//         refreshing = false; flushQueue()
//         return api(original)
//       } catch (e) {
//         refreshing = false; clearAuth(); flushQueue()
//         return Promise.reject(e)
//       }
//     }
//     return Promise.reject(error)
//   }
// )
api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config
    const { setAuth, clearAuth } = useAuth()

    const status = error?.response?.status
    const url = (original?.url || '').toString()

    // ✅ 關鍵 1：/auth/refresh 自己 401 時，直接清狀態＋導去登入（不要再打 refresh 造成死循環）
    if (status === 401 && url.endsWith('/auth/refresh')) {
      clearAuth()
      try {
        const { default: router } = await import('../router')
        router.push({ path: '/login', query: { redirect: window.location.pathname + window.location.search } })
      } finally {
        // 若你有 queue 機制，務必清掉
        refreshing = false
        flushQueue()
      }
      return Promise.reject(error)
    }

    // ✅ 關鍵 2：一般 401（非 /auth/refresh）才嘗試刷新
    if (status === 401 && !original._retry) {
      if (refreshing) return queueRequest(() => api(original))
      original._retry = true
      refreshing = true
      try {
        const { data } = await api.post('/auth/refresh', null) // 這裡會觸發上面的判斷
        setAuth(data.accessToken, data.user ?? null)
        return api(original)
      } catch (e) {
        clearAuth()
        const { default: router } = await import('../router')
        router.push({ path: '/login', query: { redirect: window.location.pathname + window.location.search } })
        return Promise.reject(e)
      } finally {
        // ✅ 關鍵 3：無論成功/失敗都要把佇列清掉，避免整個 app 卡住 = 白畫面
        refreshing = false
        flushQueue()
      }
    }

    return Promise.reject(error)
  }
)
export default api
