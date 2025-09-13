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

<template>
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
</template>
