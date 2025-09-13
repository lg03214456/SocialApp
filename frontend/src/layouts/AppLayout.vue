<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'

import { useRoute, useRouter } from 'vue-router'
//import { useAuth } from '../stores/auth'
import { logout } from '../services/auth'

// ↓↓↓ 新增（本元件內部狀態）
const menuOpen = ref(false) // 是否展開下拉
const menuRef = ref<HTMLElement | null>(null) // 用來做點外關閉

//const auth = useAuth()
const router = useRouter()
const route = useRoute()
const loggingOut = ref(false)

function toggleMenu() {
  // 點使用者名稱切換開關
  menuOpen.value = !menuOpen.value
}

function onDocClick(e: MouseEvent) {
  // 點擊外部就關閉
  if (menuRef.value && !menuRef.value.contains(e.target as Node)) {
    menuOpen.value = false
  }
}

onMounted(() => {
  document.addEventListener('pointerdown', onDocClick) // ← 新增，涵蓋觸控
  document.addEventListener('mousedown', onDocClick)
})
onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', onDocClick)
  document.removeEventListener('mousedown', onDocClick)
})

async function onLogout() {
  console.log('------')
  if (loggingOut.value) return
  menuOpen.value = false // ← 先關選單
  loggingOut.value = true
  try {
    await logout() // 會呼叫 /auth/logout；finally 內已 clearAuth()
  } finally {
    // 立刻導回登入頁，並帶回目前頁面做 redirect（登入後可回到剛才的位置）
    const redirect =
      route.fullPath && route.fullPath !== '/login'
        ? { path: '/login', query: { redirect: route.fullPath } }
        : { path: '/login' }
    router.replace(redirect)
    loggingOut.value = false
  }
}

const isActive = (path: string) =>
  route.path === path || (path !== '/' && route.path.startsWith(path))

const btnClass = (path: string) =>
  isActive(path) ? 'text-blue-600' : 'text-gray-500 hover:text-gray-700'
</script>

<template>
  <div class="min-h-screen flex flex-col">
    <!-- ⬇️ 用 named view 渲染每頁指定的 header -->
    <RouterView name="header" />

    <main class="flex-1 p-4 pb-[calc(64px+env(safe-area-inset-bottom))]">
      <RouterView />
    </main>

    <!-- Footer (5 欄底部導覽；先放 2 個：主頁、聊天，其餘保留空位) -->
    <footer
      class="fixed bottom-0 inset-x-0 border-t bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/60 z-50"
    >
      <nav class="max-w-7xl mx-auto">
        <ul class="grid grid-cols-5">
          <!-- 主頁 -->
          <li>
            <RouterLink
              to="/"
              class="flex flex-col items-center justify-center py-2 gap-1"
              :class="btnClass('/')"
              aria-label="主頁"
              :aria-current="isActive('/') ? 'page' : undefined"
            >
              <i class="pi pi-home text-xl"></i>
              <span class="text-xs">主頁</span>
            </RouterLink>
          </li>

          <!-- 聊天（請確保有 /chat 路由；若不同可改成你的實際路徑） -->
          <li>
            <RouterLink
              to="/chat"
              class="flex flex-col items-center justify-center py-2 gap-1"
              :class="btnClass('/chat')"
              aria-label="聊天"
              :aria-current="isActive('/chat') ? 'page' : undefined"
            >
              <i class="pi pi-comments text-xl"></i>
              <span class="text-xs">聊天</span>
            </RouterLink>
          </li>

          <!-- 預留 3 個位置（先空著保持 5 欄對齊） -->
          <li><div class="py-2"></div></li>
          <li><div class="py-2"></div></li>
          <li>
            <div class="py-2">
              <!-- 右側：actions + 使用者下拉（精緻化樣式） -->
              <div ref="menuRef" class="relative flex items-center gap-3">
                <slot name="actions" />
                <!-- 設定按鈕 -->
                <button
                  class="flex flex-col items-center justify-center py-2 gap-1"
                  aria-label="設定"
                  @click="toggleMenu"
                >
                  <i class="pi pi-cog text-xl"></i>
                  <span class="text-xs">設定</span>
                </button>

                <!-- 下拉選單卡片 -->
                <div
                  v-show="menuOpen"
                  id="user-menu"
                  role="menu"
                  class="absolute right-0 bottom-full mb-2 w-48 rounded-xl border border-gray-200 bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/80 shadow-lg p-1 z-50"
                >
                  <!-- 更改密碼 -->
                  <RouterLink
                    to="/change-password"
                    role="menuitem"
                    class="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 active:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500/50"
                  >
                    <i class="pi pi-key text-base"></i>
                    <span>更改密碼</span>
                  </RouterLink>

                  <button
                    type="button"
                    role="menuitem"
                    class="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 active:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500/50"
                    @click.stop="onLogout"
                  >
                    <i class="pi pi-sign-out text-base"></i>
                    <span>登出</span>
                  </button>
                </div>
              </div>
            </div>
          </li>
        </ul>
      </nav>
    </footer>
  </div>
</template>
