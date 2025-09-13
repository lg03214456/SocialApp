
import api from './http'
import { useAuth } from '../stores/auth'

// ✅ 建議補型別（對齊後端 AuthResponse）
export type UserDto = {  id: number; username: string; userId?: string; email?: string }
export type AuthResponse = { accessToken: string; user: UserDto }

export async function login(username: string, password: string) {
  const { data } = await api.post<AuthResponse>('/auth/login', { username, password })
  const { setAuth } = useAuth()
  setAuth(data.accessToken, data.user ?? null) // refresh 由後端寫入 HttpOnly Cookie
  return data
}

// ✅ 修正：把 userId、email 一起送給後端
export async function register(username: string, password: string, userId: string, email: string) {
  return api.post<{ message: string }>('/auth/register', {
    username,
    password,
    userId,
    email,
  })
}
// export async function changePassword(current: string, next: string) {
//   return api.post('/auth/change-password', { current, next })
// }

export async function logout() {
  const { clearAuth } = useAuth()
  try { await api.post('/auth/logout') } finally { clearAuth() }
}

export async function trySilentAuth() {
  const { state, setAuth, clearAuth } = useAuth()
  try {
    const { data } = await api.post('/auth/refresh', null)
    setAuth(data.accessToken, data.user ?? null)
  } catch {
    if (!state.accessToken) clearAuth()
  }
}

