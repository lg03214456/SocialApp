<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import {
  searchUsers,
  sendFriendInvite,
  listFriendRequests,
  cancelFriendRequest,
  acceptFriendRequest,
  rejectFriendRequest,
  type UserLite,
} from '../services/friends'
import { useAuth } from '../stores/auth'

//type UserLite = { id: number; userId: string; username: string; avatar?: string }
type PendingReq = {
  requestId: number
  fromUserId: string
  toUserId: string
  message: string
  status: 'Pending' | 'Accepted' | 'Rejected'
  createdAt: string
}

const q = ref('')
const searching = ref(false)
const results = ref<UserLite[]>([])
const hasSearched = ref(false) // 👈 是否已經觸發過搜尋

// 新增一個提示用字串（你也可以用 toast）
const notice = ref<string | null>(null)

const sending = ref<string | null>(null) // 目前送出的 userId（用於按鈕 loading）

// ✅ 待處理清單 + 載入中狀態
const pending = ref<PendingReq[]>([])
const pendingLoading = ref(false)

// 取得目前登入者 Id（要能和 from/toUserId 對上）
const auth = useAuth()
const selfId = computed(() => auth.state.user?.userId ?? '')

// ✅ 封裝一個載入「我送出的 pending 邀請」的函式
async function loadPending() {
  pendingLoading.value = true
  try {
    // 後端固定回 pending 清單：requestId, fromUserId, toUserId, message?, createdAt, status='Pending'
    const items = await listFriendRequests(50)
    // 對齊你頁面使用的型別（只留必要欄位）
    pending.value = items.map((i) => ({
      requestId: i.requestId,
      fromUserId: i.fromUserId,
      toUserId: i.toUserId,
      message: i.message ?? '',
      createdAt: i.createdAt,
      status: 'Pending' as const,
    }))
  } catch (e) {
    console.error('load pending failed', e)
    pending.value = []
  } finally {
    pendingLoading.value = false
  }
}
// ✅ 進頁就載一次
onMounted(loadPending)
console.log(pending)
// 依角色分流（純前端）

// outgoing = 我送出的（fromUserId === 自己）
const outgoing = computed(() => pending.value.filter((p) => p.fromUserId === selfId.value))
// incoming = 別人送給我的（toUserId === 自己）
const incoming = computed(() => pending.value.filter((p) => p.toUserId === selfId.value))

async function onSearch() {
  const keyword = q.value.trim()

  // 清空：回到初始提示、不打 API
  if (!keyword) {
    results.value = []
    hasSearched.value = false
    notice.value = null
    return
  }

  // 沒輸入就清空結果、不要打 API
  if (!keyword) {
    results.value = []
    return
  }

  searching.value = true
  notice.value = null

  try {
    results.value = await searchUsers(keyword) // ← 呼叫 services/friends.ts
 
  } catch (e) {
    console.error('search failed', e)
    results.value = [] // 失敗時清空或保留舊結果自行決定
  } finally {
    searching.value = false
    hasSearched.value = true
  }
}

async function sendInvite(u: UserLite) {
  // TODO: POST /api/friend-requests { toUserId: u.id or u.userId, message? }
  //alert(`已送出好友邀請給 ${u.userId}（示範）`)
  if (sending.value) return
  sending.value = u.userId
  try {
    await sendFriendInvite(u.userId) // 1) 送出 API（按鈕右側會轉圈）
    await loadPending() // 2) 重抓清單（列表區塊會顯示「載入中…」）
    //alert(`已送出好友邀請給 @${u.userId}`)
  } catch (e: any) {
    alert(e?.response?.data?.message ?? '送出邀請失敗')
  } finally {
    sending.value = null
  }
}

async function cancelInvite(r: PendingReq) {
  // TODO: DELETE /api/friend-requests/{requestId}
  // alert(`已取消邀請 #${r.requestId}（示範）`)
  try {
    await cancelFriendRequest(r.requestId)
    // ✅ 取消成功後重整列表（或直接從陣列移除）
    pending.value = pending.value.filter((x) => x.requestId !== r.requestId)
  } catch (e: any) {
    alert(e?.response?.data?.message ?? '取消失敗')
  }
}

async function acceptInvite(r: PendingReq) {
  // TODO: DELETE /api/friend-requests/{requestId}
  // alert(`已取消邀請 #${r.requestId}（示範）`)
  try {
    await acceptFriendRequest(r.requestId)
    // ✅ 取消成功後重整列表（或直接從陣列移除）
    pending.value = pending.value.filter((x) => x.requestId !== r.requestId)
  } catch (e: any) {
    alert(e?.response?.data?.message ?? '接受失敗')
  }
}

async function rejectInvite(r: PendingReq) {
  // TODO: DELETE /api/friend-requests/{requestId}
  // alert(`已取消邀請 #${r.requestId}（示範）`)
  try {
    await rejectFriendRequest(r.requestId)
    // ✅ 取消成功後重整列表（或直接從陣列移除）
    pending.value = pending.value.filter((x) => x.requestId !== r.requestId)
  } catch (e: any) {
    alert(e?.response?.data?.message ?? '拒絕失敗')
  }
}
</script>

<template>
  <div class="max-w-2xl mx-auto px-4 py-4 space-y-6">
    <!-- 標題列 -->
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-semibold">加好友</h1>
      <RouterLink
        to="/chat"
        class="inline-flex items-center gap-2 text-sm text-blue-600 hover:underline"
      >
        <i class="pi pi-comments text-sm"></i> 進入聊天
      </RouterLink>
    </div>

    <!-- 搜尋卡片 -->
    <section
  class="rounded-2xl border bg-white p-4 shadow-sm"
  :aria-busy="searching ? 'true' : 'false'"
  aria-live="polite"
>
  <label class="block text-sm text-gray-600 mb-2">以 UserID / 使用者名稱 / Email 搜尋</label>

  <div class="flex gap-2">
    <input
      v-model.trim="q"
      type="text"
      placeholder="例如：alice 或 alice@example.com"
      class="flex-1 rounded-lg border px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500/50"
      @keydown.enter.prevent="onSearch"  
    />
    <button
      class="inline-flex items-center gap-2 rounded-lg border px-3 py-2 hover:bg-gray-50 active:bg-gray-100 disabled:opacity-50"
      :disabled="searching || !q"        
      @click="onSearch"
    >
      <i class="pi pi-search text-sm"></i>
      <span>搜尋</span>
    </button>
  </div>

  <!-- 結果 / 狀態 -->
  <div class="mt-4">
    <!-- 有結果 -->
    <div v-if="results.length" class="divide-y">
      <div v-for="u in results" :key="u.id" class="flex items-center justify-between py-3">
        <div class="flex items-center gap-3">
          <div
            class="h-9 w-9 grid place-items-center rounded-full bg-gradient-to-br from-blue-100 to-blue-200 text-blue-700 text-xs font-bold uppercase"
          >
            {{ ((u.userId || u.username) ?? '').slice(0, 1).toUpperCase() }}
          </div>
          <div>
            <div class="font-medium">{{ u.username }}</div>
            <div class="text-xs text-gray-500">@{{ u.userId }}</div>
          </div>
        </div>

        <!-- 右側動作區：已是好友顯示徽章，否則顯示邀請按鈕 -->
        <div class="flex items-center gap-2">
          <span
            v-if="u.alreadyFriend"
            class="inline-flex items-center gap-1 rounded-full bg-green-50 px-2 py-1 text-xs font-medium text-green-700"
          >
            <i class="pi pi-check-circle text-xs"></i> 已是好友
          </span>

          <button
            v-else
            class="inline-flex items-center gap-2 rounded-full border px-3 py-1.5 text-sm hover:bg-gray-50 active:bg-gray-100 disabled:opacity-50"
            :disabled="sending === u.userId"
            @click="sendInvite(u)"
          >
            <i class="pi pi-user-plus text-sm" :class="{ 'pi-spin': sending === u.userId }"></i>
            邀請
          </button>
        </div>
      </div>
    </div>

    <!-- 搜尋中 -->
    <p v-else-if="searching" role="status" class="text-sm text-gray-500 flex items-center gap-2">
      <i class="pi pi-spinner pi-spin"></i> 搜尋中…
    </p>

    <!-- 搜尋過但沒有結果 -->
    <p v-else-if="hasSearched" class="text-sm text-gray-500">查無符合的使用者</p>

    <!-- 尚未搜尋 -->
    <p v-else class="text-sm text-gray-500">輸入關鍵字後按 Enter 或點搜尋。</p>

  </div>
</section>


    <!-- 待處理邀請 -->
    <!-- 待處理邀請：我送出的 -->
    <section
      class="rounded-2xl border bg-white p-4 shadow-sm"
      :aria-busy="pendingLoading ? 'true' : 'false'"
    >
      <div class="flex items-center justify-between mb-2">
        <h2 class="font-semibold">待處理（我送出的）</h2>
        <span class="text-xs text-gray-500">{{ outgoing.length }} 筆</span>
      </div>

      <p v-if="pendingLoading" class="text-sm text-gray-500 flex items-center gap-2">
        <i class="pi pi-spinner pi-spin"></i> 載入中…
      </p>

      <div v-else-if="outgoing.length" class="space-y-2">
        <div
          v-for="r in outgoing"
          :key="r.requestId"
          class="flex items-center justify-between rounded-lg border px-3 py-2"
        >
          <div class="flex items-center gap-3">
            <div class="h-8 w-8 grid place-items-center rounded-full bg-gray-100 text-gray-600">
              <i class="pi pi-user text-xs"></i>
            </div>
            <div>
              <div class="text-sm">
                傳送給 <span class="font-medium">@{{ r.toUserId }}</span>
              </div>
              <div class="text-xs text-gray-500">狀態：Pending</div>
              <div class="text-xs text-gray-400">
                {{ new Date(r.createdAt).toLocaleString() }}
              </div>
            </div>
          </div>

          <button class="text-sm text-red-600 hover:underline" @click="cancelInvite(r)">
            取消
          </button>
        </div>
      </div>

      <p v-else class="text-sm text-gray-500">目前沒有我送出的待處理邀請。</p>
    </section>

    <!-- 待處理邀請：別人送給我的 -->
    <section
      class="rounded-2xl border bg-white p-4 shadow-sm"
      :aria-busy="pendingLoading ? 'true' : 'false'"
    >
      <div class="flex items-center justify-between mb-2">
        <h2 class="font-semibold">待處理（別人送給我的）</h2>
        <span class="text-xs text-gray-500">{{ incoming.length }} 筆</span>
      </div>

      <p v-if="pendingLoading" class="text-sm text-gray-500 flex items-center gap-2">
        <i class="pi pi-spinner pi-spin"></i> 載入中…
      </p>

      <div v-else-if="incoming.length" class="space-y-2">
        <div
          v-for="r in incoming"
          :key="r.requestId"
          class="flex items-center justify-between rounded-lg border px-3 py-2"
        >
          <div class="flex items-center gap-3">
            <div class="h-8 w-8 grid place-items-center rounded-full bg-gray-100 text-gray-600">
              <i class="pi pi-user text-xs"></i>
            </div>
            <div>
              <div class="text-sm">
                來自 <span class="font-medium">@{{ r.fromUserId }}</span>
              </div>
              <div class="text-xs text-gray-500">狀態：Pending</div>
              <div class="text-xs text-gray-400">
                {{ new Date(r.createdAt).toLocaleString() }}
              </div>
            </div>
          </div>

          <div class="flex items-center gap-3">
            <button class="text-sm text-green-600 hover:underline" @click="acceptInvite(r)">
              接受
            </button>
            <button class="text-sm text-gray-600 hover:underline" @click="rejectInvite(r)">
              拒絕
            </button>
          </div>
        </div>
      </div>

      <p v-else class="text-sm text-gray-500">目前沒有別人送給我的待處理邀請。</p>
    </section>
  </div>
</template>
