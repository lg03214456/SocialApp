<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { listFriends, type FriendListItem } from '../services/friends'
import { useRouter } from 'vue-router'

const router = useRouter()

const friends = ref<FriendListItem[]>([])
const loading = ref(false)
const errorMsg = ref<string | null>(null)
const take = ref(50)

async function loadFriends() {
  loading.value = true
  errorMsg.value = null
  try {
    const data = await listFriends(take.value)
    friends.value = data as FriendListItem[]
  } catch (e: any) {
    console.error('load friends failed', e)
    errorMsg.value = e?.response?.data?.message ?? '載入好友清單失敗'
    friends.value = []
  } finally {
    loading.value = false
  }
}
onMounted(loadFriends)

function initials(f: FriendListItem) {
  const s = (f.username || f.userId || '').trim()
  return s ? s[0].toUpperCase() : '?'
}
function formatTime(iso: string) {
  return new Date(iso).toLocaleString()
}
function viewProfile(f: FriendListItem) {
  router.push({ path: `/users/${encodeURIComponent(f.userId)}` })
}
function startChat(f: FriendListItem) {
  router.push({ path: '/chat', query: { to: f.userId } })
}
</script>

<template>
  <div class="max-w-md mx-auto px-4 py-6 space-y-4">
    <div class="flex items-center justify-between">
      <h1 class="text-lg font-semibold">好友清單</h1>

      <!-- 只有 ICON 的 Refresh 按鈕 -->
      <button
        class="inline-flex h-9 w-9 items-center justify-center rounded-full border hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500/40 disabled:opacity-50"
        title="Refresh"
        aria-label="Refresh"
        :disabled="loading"
        @click="loadFriends"
        
      >
        <i class="pi pi-refresh text-sm" :class="{ 'pi-spin': loading }"></i>
        <span class="sr-only">Refresh</span>
      </button>
    </div>

    <!-- 狀態區域 -->
    <p v-if="loading" class="text-sm text-gray-500 flex items-center gap-2">
      <i class="pi pi-spinner pi-spin"></i> 載入中…
    </p>
    <p v-else-if="errorMsg" class="text-sm text-red-600">
      {{ errorMsg }}
    </p>

    <!-- 清單 -->
    <div v-else class="rounded-lg border bg-white shadow-sm divide-y">
      <template v-if="friends.length">
        <div
          v-for="f in friends"
          :key="f.id"
          class="p-3 hover:bg-gray-50"
        >
          <!-- 上排：頭像 + 名稱/@username + 右側按鈕 -->
          <div class="flex items-center gap-3">
            <!-- 頭像：無 avatar 時以首字母徽章替代 -->
            <div class="h-10 w-10 grid place-items-center rounded-full bg-gray-100 text-gray-700 shrink-0">
              {{ initials(f) }}
            </div>

            <!-- 名稱與帳號 -->
            <div class="min-w-0 flex-1">
              <div class="truncate font-medium text-gray-800">
                {{ f.username }}
                <span class="ml-2 text-xs text-gray-500">@{{ f.userId }}</span>
              </div>
            </div>

            <!-- 右側按鈕 -->
            <div class="shrink-0 flex items-center gap-2">
              <button
                class="rounded-full border px-3 py-1 text-xs hover:bg-gray-50"             
                title="Profile"
                aria-label="Profile"
                @click="viewProfile(f)"
              >
                簡介
              </button>
              <button
                class="rounded-full border px-3 py-1 text-xs hover:bg-gray-50"       
                title="Chat"
                aria-label="Chat"
                @click="startChat(f)"
              >
                聊天
              </button>
            </div>
          </div>

          <!-- 下排：加入時間（縮排與上排文字對齊） -->
          <div class="text-[11px] text-gray-400 mt-1 pl-13 sm:pl-14">
            加入時間：{{ formatTime(f.createdAt) }}
          </div>
        </div>
      </template>

      <div v-else class="p-6 text-center text-sm text-gray-500">
        目前沒有好友。
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 讓下排時間與上排文字左緣對齊（頭像寬 40px + gap 12px ≈ 52px） */
.pl-13 { padding-left: 52px; }
@media (min-width: 640px) { .pl-14 { padding-left: 56px; } }
</style>
