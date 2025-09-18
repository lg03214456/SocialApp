

// src/stores/auth.ts
import { reactive, computed } from 'vue'
type User = { id: number; username: string; userId?: string; email?: string } | null

const state = reactive({
  accessToken: '' as string,
  user: null as User,
})
export function useAuth() {
  const isAuthed = computed(() => !!state.accessToken && !!state.user)
 // ✅ 安全取出登入者 id，沒有登入時給 null
  const userId = computed(() => state.user?.userId ?? '')

  // ✅ 其他方便取用的屬性
  const username = computed(() => state.user?.username ?? '')
  const loginUser = computed(() => state.user) // 整個 user 物件

  const setAuth = (token: string, user: User) => { state.accessToken = token; state.user = user }
  const clearAuth = () => { state.accessToken = ''; state.user = null }
  return { state, isAuthed, userId, username, loginUser, setAuth, clearAuth }
}
