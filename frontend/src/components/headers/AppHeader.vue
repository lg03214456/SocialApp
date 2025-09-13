<!-- src/components/headers/AppHeader.vue -->
<script setup lang="ts">
import { useAuth } from '../../stores/auth'
const auth = useAuth()
</script>

<!-- AppHeader.vue -->
<!-- <template>
  <header class="border-b p-4 flex items-center justify-between">
    <nav class="flex gap-3">
      <RouterLink to="/" class="hover:underline">Home</RouterLink>
      <RouterLink to="/users" class="hover:underline">Users</RouterLink>
    </nav>

    <button
      v-if="auth.state.user"
      type="button"
      class="group inline-flex items-center gap-2 rounded-full px-2.5 py-1.5 text-sm text-gray-700
             hover:bg-gray-100 active:bg-gray-200 transition
             focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/50"
    >
      <span
        class="grid h-7 w-7 place-items-center rounded-full bg-gradient-to-br from-blue-100 to-blue-200
               text-blue-700 text-xs font-bold uppercase shadow-sm"
        aria-hidden="true"
      >
        {{ (auth.state.user.userId ?? '').slice(0, 1) }}
      </span>
      <span class="hidden sm:block max-w-[10rem] truncate font-medium">
        {{ auth.state.user.userId }}
      </span>
    </button>
  </header>
</template> -->
<template>
  <!-- 外層滿版：背景 + 底線 -->
  <header class="w-full border-b bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/80">
    <!-- 內層內容：滿寬 + 置中 -->
    <div class="h-14 max-w-7xl mx-auto px-4 flex items-center justify-between">
      <!-- 左側導覽 -->
      <!-- <nav class="flex items-center gap-3 text-sm">
        <RouterLink to="/" class="hover:underline">Home</RouterLink>
        <RouterLink to="/users" class="hover:underline">Users</RouterLink>
      </nav> -->

      <!-- 右側功能列 -->
       
      <div class="flex items-center gap-2">

        <!-- 個人主頁 -->
        <RouterLink
          to="/profile"
          class="h-9 w-9 rounded-full grid place-items-center hover:bg-gray-100"
          aria-label="個人主頁"
        >
          <i class="pi pi-user text-base"></i>
        </RouterLink>
        <!-- 通知 -->
        <RouterLink
          to="/notifications"
          class="h-9 w-9 rounded-full grid place-items-center hover:bg-gray-100"
          aria-label="通知"
        >
          <i class="pi pi-bell text-base"></i>
        </RouterLink>

        <!-- 加好友 -->
        <RouterLink
          to="/add-friend"
          class="h-9 w-9 rounded-full grid place-items-center hover:bg-gray-100"
          aria-label="加好友"
        >
          <i class="pi pi-user-plus text-base"></i>
        </RouterLink>

        <!-- 使用者頭像與名稱 -->
        <button
          v-if="auth.state.user"
          type="button"
          class="inline-flex items-center gap-2 rounded-full px-2.5 py-1.5 text-sm text-gray-700 hover:bg-gray-100 active:bg-gray-200 transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/50"
        >
          <span
            class="grid h-7 w-7 place-items-center rounded-full bg-gradient-to-br from-blue-100 to-blue-200 text-blue-700 text-xs font-bold uppercase shadow-sm"
          >
            {{ (auth.state.user.userId ?? '').slice(0, 1) }}
          </span>
          <span class="hidden sm:block max-w-[10rem] truncate font-medium">
            {{ auth.state.user.userId }}
          </span>
        </button>
      </div>
    </div>
  </header>
</template>

