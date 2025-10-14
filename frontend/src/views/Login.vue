<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { login } from '../services/auth'

const router = useRouter()
const route = useRoute()

const username = ref('')
const password = ref('')
const loading = ref(false)
const err = ref<string | null>(null)

async function handleLogin() {
  loading.value = true
  err.value = null
  try {
    await login(username.value, password.value)
    const redirect = (route.query.redirect as string) || '/'
    router.replace(redirect)
  } catch (e: any) {
    err.value = e?.response?.data?.message ?? 'Login failed'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <!-- 外層：設定品牌色變數 -->
  <div
    class="min-h-svh flex items-center justify-center bg-[#D4A07A] p-6"
    style="--brand:#5B3A2E; --brand-hover:#4A2F25;"
  >
    <form class="w-full max-w-xs space-y-4" @submit.prevent="handleLogin">
      <!-- Logo 區：用 text-brand，SVG 用 currentColor -->
      <div class="flex flex-col items-center mb-6 text-brand">
        <svg viewBox="0 0 64 64" class="w-64 h-64" aria-hidden="true">
          <path d="M12 24h32a6 6 0 0 1 6 6v4a10 10 0 0 1-10 10H22A10 10 0 0 1 12 34v-8z" fill="currentColor"/>
          <path d="M44 30h6a6 6 0 1 1-6 6v-6z" fill="currentColor"/>
          <path d="M26 10c2 3 1 6-1 8M34 10c2 3 1 6-1 8" stroke="currentColor" stroke-width="3" fill="none" stroke-linecap="round" vector-effect="non-scaling-stroke"/>
        </svg>
        <h1 class="mt-2 text-2xl font-semibold tracking-wide">Coffee Chat</h1>
        <p class="text-xl text-[#7A5244]">sign in to continue</p>
      </div>

      <p v-if="err" class="text-[#7a1f1f] text-sm text-center bg-white/30 rounded-md py-2 px-3">
        {{ err }}
      </p>

      <!-- 膠囊輸入框 -->
      <label class="block">
        <span class="sr-only">Username</span>
        <InputText v-model="username" class="w-full coffee-input" placeholder="Username" />
      </label>

      <label class="block">
        <span class="sr-only">Password</span>
        <InputText v-model="password" type="password" class="w-full coffee-input" placeholder="Password" />
      </label>

      <!-- 膠囊按鈕（用同一品牌色） -->
      <PvButton
        type="submit"
        :label="loading ? 'Signing in...' : 'Login'"
        icon="pi pi-sign-in"
        class="w-full coffee-btn"
        :disabled="loading"
      />

      <div class="text-center text-sm">
        <RouterLink to="/register" class="text-brand/90 hover:underline">
          Create an account
        </RouterLink>
      </div>
    </form>
  </div>
</template>

<style scoped>
/* 文字品牌色（供 SVG 用 currentColor / 標題用） */
.text-brand { color: var(--brand); }

/* Input：膠囊、淺拿鐵底、深咖啡字 */
:deep(.coffee-input) {
  border-radius: 9999px;
  height: 2.75rem;
  padding-left: 1.25rem;
  padding-right: 1.25rem;
  border: 0;
  box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.05);
  background-color: #E9B284;
  color: var(--brand);
}
:deep(.coffee-input::placeholder) {
  color: rgba(90, 58, 46, 0.7);
}
:deep(.coffee-input:focus) {
  outline: none;
  box-shadow: 0 0 0 4px rgba(91, 58, 46, 0.2);
}

/* Button：用同一品牌色（!important 壓過主題） */
:deep(.coffee-btn) {
  width: 100%;
  height: 3rem;
  border-radius: 9999px;
  border: 0;
  font-weight: 500;
  color: #fff;
  background-color: var(--brand) !important;
  border-color: var(--brand) !important;
  box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1),
              0 2px 4px -2px rgb(0 0 0 / 0.1);
}
:deep(.coffee-btn .p-button:hover) {
  background-color: var(--brand-hover) !important;
  border-color: var(--brand-hover) !important;
}
:deep(.coffee-btn .p-button:disabled) {
  opacity: 0.7;
  filter: grayscale(0.1);
}
</style>
