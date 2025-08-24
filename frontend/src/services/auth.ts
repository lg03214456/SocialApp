// src/services/auth.ts
import api from './http'
import { useAuth } from '../stores/auth'

export async function login(username: string, password: string) {
  const { data } = await api.post('/auth/login', { username, password })
  const { setAuth } = useAuth()
  setAuth(data.accessToken, data.user ?? null)   // refresh 由後端放在 HttpOnly Cookie
  return data
}

export async function register(username: string, password: string) {
  return api.post('/auth/register', { username, password })
}

export async function logout() {
  const { clearAuth } = useAuth()
  try { await api.post('/auth/logout') } finally { clearAuth() }
}
