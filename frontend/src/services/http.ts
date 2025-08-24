// // src/services/http.ts
// // 可改成你的 .NET 8 API 位址；或用 Vite 環境變數切環境
// export const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://127.0.0.1:5000'



// export async function getForecast() {
//   const res = await fetch(`${API_BASE}/weatherforecast`)
//   if (!res.ok) throw new Error(`API ${res.status}`)
//   return res.json()
// }

// export async function getFriends() {
//   const res = await fetch(`${API_BASE}/friends`);
//   if (!res.ok) throw new Error(`API ${res.status}`);
//   return res.json();
// }

// export async function addFriend(name: string) {
//   const res = await fetch(`${API_BASE}/friends`, {
//     method: 'POST',
//     headers: { 'Content-Type': 'application/json' },
//     body: JSON.stringify({ name })
//   });
//   if (!res.ok) throw new Error(`API ${res.status}`);
//   return res.json();
// }

// src/services/http.ts
import axios from 'axios'
import { useAuth } from '../stores/auth'

export const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://127.0.0.1:5000'

const api = axios.create({
  baseURL: API_BASE,
  withCredentials: true, // 夾帶 HttpOnly Cookie (refresh)
})

// ── Request：自動加 Authorization
api.interceptors.request.use((config) => {
  const { state } = useAuth()
  if (state.accessToken) {
    config.headers = config.headers ?? {}
    config.headers.Authorization = `Bearer ${state.accessToken}`
  }
  return config
})

// ── Response：401 → refresh → 重送一次
let refreshing = false
let queue: Array<() => void> = []
const queueRequest = <T,>(fn: () => Promise<T>) =>
  new Promise<T>((resolve, reject) => {
    queue.push(async () => { try { resolve(await fn()) } catch (e) { reject(e) } })
  })
const flushQueue = () => { queue.forEach(fn => fn()); queue = [] }

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config
    const { setAuth, clearAuth } = useAuth()
    if (error?.response?.status === 401 && !original._retry) {
      if (refreshing) return queueRequest(() => api(original))
      original._retry = true
      refreshing = true
      try {
        const { data } = await api.post('/auth/refresh', null) // 用 Cookie 換新 access
        setAuth(data.accessToken, data.user ?? null)
        refreshing = false
        flushQueue()
        return api(original)
      } catch (e) {
        refreshing = false
        clearAuth()
        flushQueue()
        return Promise.reject(e)
      }
    }
    return Promise.reject(error)
  }
)

export default api
