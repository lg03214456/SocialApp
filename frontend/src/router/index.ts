// src/router/index.ts
import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuth } from '../stores/auth'
import { trySilentAuth } from '../services/auth'
//import AppHeader from '../components/headers/AppHeader.vue'

// Header 元件：為了效能你也可以改成動態 import
import AppLayout from '../layouts/AppLayout.vue'
import AppHeader from '../components/headers/AppHeader.vue'
import ChatHeader from '../components/headers/ChatHeader.vue'

const routes: RouteRecordRaw[] = [
  { path: '/login', component: () => import('../views/Login.vue'), meta: { public: true } },
  { path: '/register', component: () => import('../views/Register.vue'), meta: { public: true } },

  {
    path: '/',
    component: AppLayout, // 🔒 只有登入後才會載入這個「有 bar」的框架
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        components: {
          default: () => import('../views/HomePage.vue'),
          header: AppHeader,
        },
      },
      {
        path: 'add-friend',
        components: { default: () => import('../views/AddFriendPage.vue'), 
        header: AppHeader }, // ★ 新增
      },
      {
        path: 'friend-List',
        components: { default: () => import('../views/FriendList.vue'), 
        header: AppHeader }, // ★ 新增
      },
      {
        path: 'chat',
        components: {
          default: () => import('../views/ChatPage.vue'),
          header: ChatHeader,
        },
      },
      { path: 'users', component: () => import('../views/UsersPage.vue') },
    ],
  },

  // 兜底：不明路徑導首頁（會再被守衛處理）
  { path: '/:pathMatch(.*)*', redirect: '/' },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

let bootstrapped = false
router.beforeEach(async (to) => {
  const { isAuthed } = useAuth()

  // 首次導航：嘗試用 refresh cookie 靜默登入
  if (!bootstrapped) {
    bootstrapped = true
    await trySilentAuth()
  }

  // 公開頁：已登入者不該看到 login/register → 送回首頁
  if (to.meta.public) {
    if (isAuthed.value) return { path: '/' }
    return true
  }

  // 受保護頁：未登入者導去 login（帶回跳參數）
  if (to.meta.requiresAuth && !isAuthed.value) {
    return { path: '/login', query: { redirect: to.fullPath } }
  }

  return true
})

export default router
