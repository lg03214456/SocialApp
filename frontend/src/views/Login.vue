<!-- <script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { login } from '../services/auth' // ← 用我們的 axios 版 login()

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
    // 後端應回 { accessToken, user }；refresh token 由後端設 HttpOnly Cookie
    await login(username.value, password.value)

    // 若是從保護頁被導過來登入，回去原頁；否則進首頁
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
  <div class="min-h-screen grid place-items-center p-4 bg-gray-50">
    <form @submit.prevent="handleLogin" class="w-full max-w-sm border rounded-2xl p-6 shadow-sm bg-white space-y-4">
      <h1 class="text-xl font-semibold text-center">Sign In</h1>
      <p v-if="err" class="text-red-600 text-sm">{{ err }}</p>

      <label class="block">
        <span class="text-sm text-gray-600">Username</span>
        <InputText v-model="username" class="w-full mt-1" placeholder="yourname" />
      </label>

      <label class="block">
        <span class="text-sm text-gray-600">Password</span>
        <InputText v-model="password" type="password" class="w-full mt-1" placeholder="••••••••" />
      </label>
s
      <PvButton
        type="submit"
        :label="loading ? 'Signing in...' : 'Login'"
        icon="pi pi-sign-in"
        class="w-full"
        :disabled="loading"
      />

      <div class="text-center text-sm">
        <RouterLink to="/register" class="hover:underline">Create an account</RouterLink>
      </div>
    </form>
  </div>
</template> -->
<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { login } from '../services/auth' // ← 用我們的 axios 版 login()

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
    // 後端應回 { accessToken, user }；refresh token 由後端設 HttpOnly Cookie
    await login(username.value, password.value)

    // 若是從保護頁被導過來登入，回去原頁；否則進首頁
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
  <div class="min-h-screen grid place-items-center p-4 bg-gray-50">
    <form class="w-full max-w-sm border rounded-2xl p-6 shadow-sm bg-white space-y-4" @submit.prevent="handleLogin">
      <h1 class="text-xl font-semibold text-center">Social Hi</h1>
      <p v-if="err" class="text-red-600 text-sm">{{ err }}</p>

      <label class="block">
        <span class="text-sm text-gray-600">Username</span>
        <InputText v-model="username" class="w-full mt-1" placeholder="yourname" />
      </label>

      <label class="block">
        <span class="text-sm text-gray-600">Password</span>
        <InputText v-model="password" type="password" class="w-full mt-1" placeholder="••••••••" />
      </label>

      <PvButton
        type="submit"
        :label="loading ? 'Signing in...' : 'Login'"
        icon="pi pi-sign-in"
        class="w-full"
        :disabled="loading"
      />

      <div class="text-center text-sm">
        <RouterLink to="/register" class="hover:underline">Create an account</RouterLink>
      </div>
    </form>
  </div>
</template>
