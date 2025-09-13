<!-- src/components/headers/ChatHeader.vue -->
<script setup lang="ts">
import { useRouter } from 'vue-router'
const router = useRouter()
</script>

<template>
  <header class="border-b px-4 py-3 flex items-center justify-between bg-white">
    <div class="flex items-center gap-3">
      <button
        class="inline-flex items-center justify-center h-9 w-9 rounded-full hover:bg-gray-100"
        aria-label="返回"
        @click="router.back()"
      >
        <i class="pi pi-arrow-left text-base"></i>
      </button>
      <h1 class="text-base font-semibold">聊天</h1>
    </div>

    <!-- 右側預留動作（搜尋、更多） -->
    <div class="flex items-center gap-1">
      <button class="h-9 w-9 rounded-full hover:bg-gray-100" aria-label="搜尋">
        <i class="pi pi-search text-base"></i>
      </button>
      <button class="h-9 w-9 rounded-full hover:bg-gray-100" aria-label="更多">
        <i class="pi pi-ellipsis-h text-base"></i>
      </button>
    </div>
  </header>
</template>
