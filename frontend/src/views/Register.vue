<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { register } from '../services/auth'

const router = useRouter()

// 分開管理每個欄位
const email = ref('')
const userId = ref('')
const username = ref('')
const password = ref('')
const confirm = ref('')

const loading = ref(false)
const err = ref<string | null>(null)
const ok = ref<string | null>(null)

// 簡單驗證規則
const emailRe = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const userIdRe = /^[a-z0-9._-]{3,32}$/ // 小寫英數 + . _ - ，3~32

async function handleRegister() {
  err.value = null
  ok.value = null

  const e = email.value.trim().toLowerCase()
  const uid = userId.value.trim().toLowerCase()
  const un = username.value.trim()
  const pw = password.value
  const cf = confirm.value

  if (!e || !uid || !un || !pw || !cf) { err.value = 'Please fill all fields'; return }
  if (!emailRe.test(e)) { err.value = 'Invalid email format'; return }
  if (!userIdRe.test(uid)) { err.value = 'User ID must be 3–32 chars: a-z, 0-9, . _ -'; return }
  if (pw.length < 6) { err.value = 'Password must be at least 6 characters'; return }
  if (pw !== cf) { err.value = 'Passwords do not match'; return }

  loading.value = true
  try {
    await register(un, pw, uid, e) // ← 傳四個欄位
    ok.value = 'Registered! Redirecting to login...'
    setTimeout(() => router.replace('/login'), 800)
  } catch (ex: any) {
    // 後端若有回 code，可對應訊息；否則用 message
    const resp = ex?.response?.data
    err.value =
      resp?.code === 'USERNAME_EXISTS' ? 'Username already taken' :
      resp?.code === 'USERID_EXISTS'   ? 'User ID already taken' :
      resp?.code === 'EMAIL_EXISTS'    ? 'Email already used' :
      resp?.message ?? 'Register failed'
  } finally {
    loading.value = false
  }
}
</script>

<!-- <template>
  <div class="min-h-screen grid place-items-center p-4 bg-gray-50">
    <form class="w-full max-w-sm border rounded-2xl p-6 shadow-sm bg-white space-y-4" @submit.prevent="handleRegister">
      <h1 class="text-xl font-semibold text-center">Register</h1>
      <p v-if="err" class="text-red-600 text-sm">{{ err }}</p>
      <p v-if="ok" class="text-green-600 text-sm">{{ ok }}</p>

      <label class="block">
        <span class="text-sm text-gray-600">Email</span>
        <InputText v-model="email" type="email" class="w-full mt-1" placeholder="you@example.com" autocomplete="email" />
      </label>

      <label class="block">
        <span class="text-sm text-gray-600">User ID</span>
        <InputText v-model="userId" class="w-full mt-1" placeholder="yourid (a-z,0-9,._-)" autocomplete="username" />
      </label>

      <label class="block">
        <span class="text-sm text-gray-600">Username</span>
        <InputText v-model="username" class="w-full mt-1" placeholder="Your display name" />
      </label>

      <label class="block">
        <span class="text-sm text-gray-600">Password</span>
        <InputText v-model="password" type="password" class="w-full mt-1" placeholder="••••••••" autocomplete="new-password" />
      </label>

      <label class="block">
        <span class="text-sm text-gray-600">Confirm Password</span>
        <InputText v-model="confirm" type="password" class="w-full mt-1" placeholder="••••••••" autocomplete="new-password" />
      </label>

      <PvButton
        type="submit"
        :label="loading ? 'Signing up...' : 'Sign Up'"
        icon="pi pi-user-plus"
        class="w-full"
        :disabled="loading"
      />

      <div class="text-center text-sm">
        <RouterLink to="/login" class="hover:underline">Already have an account?</RouterLink>
      </div>
    </form>
  </div>
</template> -->
<template>
  <div class="min-h-svh grid place-items-center p-6 bg-[#F6F1E9]">
    <form
      class="w-full max-w-sm space-y-4"
      @submit.prevent="handleRegister"
    >
      <!-- 頁首標題（可放 Logo） -->
      <div class="flex flex-col items-center mb-2">
        <svg viewBox="0 0 64 64" class="w-64 h-64 fill-[#5B3A2E]" aria-hidden="true">
          <path d="M12 24h32a6 6 0 0 1 6 6v4a10 10 0 0 1-10 10H22A10 10 0 0 1 12 34v-8z"/>
          <path d="M44 30h6a6 6 0 1 1-6 6v-6z"/>
          <path d="M26 10c2 3 1 6-1 8M34 10c2 3 1 6-1 8" stroke="#5B3A2E" stroke-width="3" fill="none" stroke-linecap="round" vector-effect="non-scaling-stroke"/>
        </svg>
        <h1 class="mt-2 text-2xl font-semibold tracking-wide text-[#5B3A2E]">Create your account</h1>
        <p class="text-sm text-[#7A5244]">join Coffee Chat</p>
      </div>

      <!-- 訊息 -->
      <p v-if="err" class="text-sm text-[#7A1F1F] text-center bg-[#FBF7F0] rounded-md py-2 px-3">
        {{ err }}
      </p>
      <p v-if="ok" class="text-sm text-[#2E7D32] text-center bg-[#FBF7F0] rounded-md py-2 px-3">
        {{ ok }}
      </p>

      <!-- 欄位：奶白膠囊 -->
      <label class="block">
        <span class="text-sm text-[#7A5244]">Email</span>
        <InputText v-model="email" type="email" class="w-full mt-1 cream-input" placeholder="you@example.com" autocomplete="email" />
      </label>

      <label class="block">
        <span class="text-sm text-[#7A5244]">User ID</span>
        <InputText v-model="userId" class="w-full mt-1 cream-input" placeholder="yourid (a-z,0-9,._-)" autocomplete="username" />
      </label>

      <label class="block">
        <span class="text-sm text-[#7A5244]">Username</span>
        <InputText v-model="username" class="w-full mt-1 cream-input" placeholder="Your display name" />
      </label>

      <label class="block">
        <span class="text-sm text-[#7A5244]">Password</span>
        <InputText v-model="password" type="password" class="w-full mt-1 cream-input" placeholder="••••••••" autocomplete="new-password" />
      </label>

      <label class="block">
        <span class="text-sm text-[#7A5244]">Confirm Password</span>
        <InputText v-model="confirm" type="password" class="w-full mt-1 cream-input" placeholder="••••••••" autocomplete="new-password" />
      </label>

      <!-- 按鈕：可可咖啡膠囊 -->
      <PvButton
        type="submit"
        :label="loading ? 'Signing up...' : 'Sign Up'"
        icon="pi pi-user-plus"
        class="w-full cream-btn"
        :disabled="loading"
      />

      <div class="text-center text-sm">
        <RouterLink to="/login" class="text-[#5B3A2E] hover:underline">
          Already have an account?
        </RouterLink>
      </div>
    </form>
  </div>
</template>

<style scoped>
/* ------- 乳白膠囊輸入框 ------- */
:deep(.cream-input) {
  border-radius: 9999px;         /* rounded-full */
  height: 2.75rem;               /* h-11 */
  padding-left: 1.25rem;         /* px-5 */
  padding-right: 1.25rem;
  border: 0;                     /* border-0 */
  background-color: #FBF7F0;     /* 奶白底 */
  color: #5B3A2E;                /* 可可咖啡字 */
  box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.05);
}
:deep(.cream-input::placeholder) {
  color: rgba(122, 82, 68, 0.7); /* #7A5244 70% */
}
:deep(.cream-input:focus) {
  outline: none;
  box-shadow: 0 0 0 4px rgba(91, 58, 46, 0.18); /* 咖啡色柔光 */
}

/* ------- 可可咖啡膠囊按鈕（覆蓋 PrimeVue 綠） ------- */
:deep(.cream-btn) {
  width: 100%;
  height: 3rem;                  /* h-12 */
  border-radius: 9999px;
  border: 0;
  background-color: #5B3A2E !important;
  border-color: #5B3A2E !important;
  color: #fff;
  font-weight: 500;
  box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1),
              0 2px 4px -2px rgb(0 0 0 / 0.1);
}
:deep(.cream-btn .p-button:hover) {
  background-color: #4A2F25 !important;
  border-color: #4A2F25 !important;
}
:deep(.cream-btn .p-button:disabled) {
  opacity: 0.7;
  filter: grayscale(0.1);
}

/* ------- PrimeVue 主色（全站可選） ------- */
/* 若想全站把主題主色改為咖啡色，取消底下註解即可。
:root {
  --p-primary-color: #5B3A2E;
  --p-primary-contrast-color: #ffffff;
  --p-primary-hover-color: #4A2F25;
  --p-primary-500: #5B3A2E;
  --p-primary-600: #4A2F25;
}
*/
</style>
