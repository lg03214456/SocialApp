<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { listFriends, type FriendListItem } from '../services/friends'

const friends = ref<FriendListItem[]>([])
const loading = ref(false)
const errorMsg = ref<string | null>(null)
const take = ref(50)

// 重新載入
async function loadFriends() {
  loading.value = true
  errorMsg.value = null
  try {
    const data = await listFriends(take.value)
    // 若後端已是 FriendListItem 結構，直接指定即可
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

// （可選）點擊查看
function viewFriend(f: FriendListItem) {
  alert(`查看 @${f.userId}（${f.username}）`)
}
</script>

<template>
  <div class="max-w-md mx-auto px-4 py-6 space-y-4">
    <div class="flex items-center justify-between">
      <h1 class="text-lg font-semibold">好友清單</h1>
      <button
        class="text-sm text-blue-600 hover:underline"
        :disabled="loading"
        @click="loadFriends"
      >
        重新整理
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
          class="flex items-center justify-between p-3 hover:bg-gray-50"
        >
          <!-- 左側 -->
          <div>
            <div class="font-medium text-gray-800 flex items-center gap-2">
              <i class="pi pi-user text-gray-500 text-xs"></i>
              {{ f.username }}
            </div>
            <div class="text-xs text-gray-500">
              @{{ f.userId }}
            </div>
            <div class="text-[11px] text-gray-400 mt-0.5">
              加入時間：{{ new Date(f.createdAt).toLocaleString() }}
            </div>
          </div>

          <!-- 右側操作（可自行擴充，例如開聊/移除好友） -->
          <button class="text-sm text-blue-600 hover:underline" @click="viewFriend(f)">
            查看
          </button>
        </div>
      </template>

      <div v-else class="p-6 text-center text-sm text-gray-500">
        目前沒有好友。
      </div>
    </div>
  </div>
</template>
