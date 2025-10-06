<script setup lang="ts">
import { fmtChatTime} from '../utils/date'
import { ref, computed, watch, nextTick, onMounted } from 'vue'
import {
  listFriendsInbox,
  ensureDmByUid,
  listMessages, // afterId（未讀/較新）
  listMessagesBefore, // beforeId（已讀/較舊）
  setReadUpTo,
  sendTextMessage,
  getDmPeerRead, // ← 新增：取得對方已讀游標
  type FriendInboxRow,
  type MessageItem,
} from '../services/chats'

type ChatKind = 'text' | 'sticker' | 'file'
type Msg = {
  id: number
  kind: ChatKind
  text?: string
  time: string
  mine: boolean
}

/** 左側來源：來自 /chat/friends-inbox */
const inbox = ref<FriendInboxRow[]>([])
const loadingList = ref(false)
const errorList = ref<string | null>(null)

/** 右側狀態 */
const activeRow = ref<FriendInboxRow | null>(null)
const activeId = ref<number | null>(null) // conversationId
const q = ref('')
const draft = ref('')
const STICKER_EMOJI = '😊'

/** 每個會話的訊息緩存 */
const messagesMap = ref<Record<number, Msg[]>>({})

/** 每個會話的「未讀第一則 id」（用來插分隔線，不進資料陣列） */
const unreadFirstIdMap = ref<Record<number, number | null>>({})
const currentUnreadFirstId = computed(() =>
  activeId.value ? (unreadFirstIdMap.value[activeId.value] ?? null) : null
)

/** 每個會話「對方已讀游標」 */
const peerReadUpToMap = ref<Record<number, number | null>>({})



/** 判斷我方訊息是否已被對方讀取 */
function isReadByPeer(msgId: number): boolean {
  const cid = activeId.value
  if (!cid) return false
  const peerUpTo = peerReadUpToMap.value[cid]
  return peerUpTo != null && msgId <= peerUpTo
}



/** 載入清單（好友 + 最後訊息 + 未讀） */
async function loadInbox() {
  loadingList.value = true
  errorList.value = null
  try {
    inbox.value = await listFriendsInbox(100)
  } catch (e: any) {
    console.error(e)
    errorList.value = e?.response?.data?.message ?? '載入清單失敗'
    inbox.value = []
  } finally {
    loadingList.value = false
  }
}
onMounted(loadInbox)

/** 左側顯示資料（支援搜尋） */
const listForView = computed(() => {
  const kw = q.value.toLowerCase()
  return inbox.value.filter((r) => {
    const name = (r.friendName ?? '').toLowerCase()
    const uid = (r.friendUid ?? '').toLowerCase()
    const lm = (r.lastMessage?.text ?? r.lastMessage?.kind ?? '').toLowerCase()
    return !kw || name.includes(kw) || uid.includes(kw) || lm.includes(kw)
  })
})

/** 目前會話的訊息清單 */
const activeMsgs = computed(() => (activeId.value ? (messagesMap.value[activeId.value] ?? []) : []))

/** 打開某一列（必要時建立 DM）；readUpTo 為中線：左50 + 右50；記住未讀第一則 id；抓對方已讀 */
async function openFromRow(row: FriendInboxRow) {
  // 1) 確保有 conversationId（若沒有就建立）
  let cid = row.conversationId ?? null
  if (!cid) {
    const r = await ensureDmByUid(row.friendUid)
    cid = r.conversationId
    row.conversationId = cid
  }

  // 2) 設定目前會話
  activeRow.value = row
  activeId.value = cid!

  // 3) 以 readUpTo 為中線
  const R = row.readUpToMessageId ?? 0

  // 已讀（包含 readUpTo，那就 beforeId = R + 1）
  const older: MessageItem[] = R > 0 ? await listMessagesBefore(cid!, R + 1, 50) : []

  // 未讀（afterId = R，不含 R 本身）
  const newer: MessageItem[] = await listMessages(cid!, R || undefined, 50)

  // 4) 轉為 UI 陣列（舊→新）
  const olderMsgs: Msg[] = older.map((m) => ({
    id: m.id,
    kind: m.kind as ChatKind,
    text: m.text,
    time: fmtChatTime(m.time),
    mine: m.mine,
  }))
  const newerMsgs: Msg[] = newer.map((m) => ({
    id: m.id,
    kind: m.kind as ChatKind,
    text: m.text,
    time: fmtChatTime(m.time),
    mine: m.mine,
  }))
  messagesMap.value[cid!] = [...olderMsgs, ...newerMsgs]

  // 5) 設定「未讀第一則 id」（給 template 插分隔線）
  unreadFirstIdMap.value[cid!] = newerMsgs.length ? newerMsgs[0].id : null

  // 6) 進入即已讀：把游標推到此次載入最後一封（有未讀用未讀最後一封，否則用已讀最後一封）
  const tail = newer.length
    ? newer[newer.length - 1]
    : older.length
      ? older[older.length - 1]
      : undefined
  if (tail) {
    await setReadUpTo(cid!, tail.id)
    row.unreadCount = 0
    row.readUpToMessageId = tail.id
    row.lastMessage = {
      id: tail.id,
      kind: tail.kind as ChatKind,
      text: tail.text,
      time: new Date().toISOString(),
      mine: tail.mine,
    }
    // 置頂
    const i = inbox.value.findIndex((x) => x.friendId === row.friendId)
    if (i >= 0) {
      const [picked] = inbox.value.splice(i, 1)
      inbox.value.unshift(picked)
    }
  } else {
    messagesMap.value[cid!] = []
  }

  // 7) 抓一次「對方已讀游標」
  try {
    const pr = await getDmPeerRead(cid!)
    peerReadUpToMap.value[cid!] = pr.readUpToMessageId ?? null
  } catch {
    peerReadUpToMap.value[cid!] = null
  }

  // 8) 捲到分隔線（若存在），否則捲到底
  await nextTick()
  const sepEl = document.getElementById('unread-sep')
  if (sepEl && scroller.value) {
    scroller.value.scrollTop = sepEl.offsetTop - 40
  } else {
    scroller.value?.scrollTo({ top: scroller.value.scrollHeight, behavior: 'smooth' })
  }
}

/** 把某列移到最上並更新預覽（本地送訊息時用；真實送出時也可共用） */
function liftAndPreview(row: FriendInboxRow, msg: Msg) {
  const cid = activeId.value!
  const list = messagesMap.value[cid] ?? (messagesMap.value[cid] = [])
  list.push(msg)

  row.lastMessage = {
    id: msg.id,
    kind: msg.kind as ChatKind,
    ...(msg.kind === 'text' ? { text: msg.text } : {}),
    time: new Date().toISOString(),
    mine: true,
  }

  // 自己送訊息後，未讀分隔線（如果原本有）通常應該消失
  unreadFirstIdMap.value[cid] = null

  // 置頂
  const i = inbox.value.findIndex((x) => x.friendId === row.friendId)
  if (i >= 0) {
    const [picked] = inbox.value.splice(i, 1)
    inbox.value.unshift(picked)
  }
}

/** 送文字：樂觀預覽 → 打 API → 覆蓋 temp → 更新預覽與游標 */
async function sendText() {
  if (!draft.value.trim() || activeId.value === null || !activeRow.value) return

  const text = draft.value.trim()
  const tempId = Date.now()

  const tempMsg: Msg = {
    id: tempId,
    kind: 'text',
    text,
    time: new Date().toLocaleString(),
    mine: true,
  }
  liftAndPreview(activeRow.value, tempMsg)
  draft.value = ''

  try {
    const saved = await sendTextMessage(activeId.value, text)
    const cid = activeId.value
    const list = messagesMap.value[cid] ?? (messagesMap.value[cid] = [])
    const i = list.findIndex((m) => m.id === tempId)

    const realMsg: Msg = {
      id: saved.id,
      kind: saved.kind as ChatKind,
      text: saved.text,
      time: fmtChatTime(saved.time),
      mine: true,
    }
    if (i >= 0) list[i] = realMsg
    else list.push(realMsg)

    activeRow.value.lastMessage = {
      id: saved.id,
      kind: saved.kind as ChatKind,
      text: saved.text,
      time: new Date(saved.time).toISOString(),
      mine: true,
    }
    activeRow.value.readUpToMessageId = saved.id
    activeRow.value.unreadCount = 0

    const idx = inbox.value.findIndex((x) => x.friendId === activeRow.value!.friendId)
    if (idx >= 0) {
      const [picked] = inbox.value.splice(idx, 1)
      inbox.value.unshift(picked)
    }

    // 送出後順便刷新一次「對方已讀游標」（可選）
    try {
      const pr = await getDmPeerRead(cid!)
      peerReadUpToMap.value[cid!] = pr.readUpToMessageId ?? null
    } catch (e) {
      // 明確使用變數避免 no-unused-vars
      void e
      // 忽略：查詢對方已讀失敗不影響送訊息流程
    }
  } catch (err: any) {
    const cid = activeId.value!
    const list = messagesMap.value[cid] ?? (messagesMap.value[cid] = [])
    const i = list.findIndex((m) => m.id === tempId)
    if (i >= 0) list.splice(i, 1)
    console.error(err)
    alert(err?.response?.data?.message ?? '傳送失敗，請稍後再試')
  }
}

/** 送貼圖（暫以本地預覽） */
function sendSticker() {
  if (activeId.value === null || !activeRow.value) return
  const msg: Msg = {
    id: Date.now(),
    kind: 'sticker',
    time: new Date().toLocaleString(),
    mine: true,
  }
  liftAndPreview(activeRow.value, msg)
}

/** 捲到最底（新訊息時）；開啟時會由 openFromRow 控制到分隔線 */
const scroller = ref<HTMLDivElement | null>(null)
watch(activeMsgs, async () => {
  await nextTick()
  scroller.value?.scrollTo({ top: scroller.value.scrollHeight, behavior: 'smooth' })
})
</script>

<template>
  <div class="h-[85vh] md:h-[90vh] max-w-7xl mx-auto p-3 md:p-4 bg-[#f7f3ef] text-[#5c4033]">
    <!-- 加 min-h-0 讓內層可捲動 -->
    <div class="grid h-full min-h-0 gap-3 md:grid-cols-[320px_1fr]">

      <!-- 左側清單（好友 + 最後訊息 + 未讀） -->
      <aside
        class="flex min-h-0 flex-col rounded-xl border border-[#e0d6c8] bg-[#faf6f2]"
        :class="{ 'hidden md:flex': activeId !== null }"
      >
        <div class="shrink-0 p-3 border-b border-[#e0d6c8] flex items-center gap-2">
          <div class="relative flex-1">
            <i
              class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-[#a1887f] text-sm"
            ></i>
            <input
              v-model="q"
              type="text"
              placeholder="搜尋聊天或聯絡人"
              class="w-full rounded-lg border border-[#e0d6c8] bg-white px-9 py-2 text-sm text-[#5c4033] placeholder:text-[#8b6f61]/70 focus:ring-2 focus:ring-[#d4a373]/40 outline-none"
            />
          </div>
          <button
            class="inline-flex h-9 w-9 items-center justify-center rounded-full border border-[#e0d6c8] hover:bg-[#f0e5da]"
            :title="loadingList ? '載入中…' : '重新整理'"
            :disabled="loadingList"
            @click="loadInbox"
          >
            <i :class="['pi', loadingList ? 'pi-spinner pi-spin' : 'pi-refresh']"></i>
          </button>
        </div>

        <!-- 這層是清單的捲動容器 -->
        <div class="flex-1 min-h-0 overflow-y-auto">
          <div v-if="loadingList" class="p-4 text-sm text-[#8b6f61] flex items-center gap-2">
            <i class="pi pi-spinner pi-spin"></i> 載入清單…
          </div>
          <div v-else-if="errorList" class="p-4 text-sm text-red-600">
            {{ errorList }}
          </div>

          <button
            v-for="row in listForView"
            v-else
            :key="row.friendId"
            class="w-full flex items-center gap-3 px-3 py-2 hover:bg-[#f0e5da] text-left"
            :class="{ 'bg-[#f0e5da]': row.conversationId === activeId }"
            @click="openFromRow(row)"
          >
            <div class="h-11 w-11 rounded-full bg-[#e8d8c7] grid place-items-center shrink-0">
              <span class="text-sm text-[#5c4033] font-semibold">
                {{ (row.friendName || row.friendUid).slice(0, 1).toUpperCase() }}
              </span>
            </div>

            <div class="min-w-0 flex-1">
              <div class="flex items-center justify-between gap-2">
                <div class="truncate font-medium">
                  {{ row.friendName || row.friendUid }}
                  <span class="text-xs text-[#8b6f61]">@{{ row.friendUid }}</span>
                </div>
                <div class="text-[11px] text-[#8b6f61] shrink-0">
                  {{ row.lastMessage ? new Date(row.lastMessage.time).toLocaleString() : '' }}
                </div>
              </div>
              <div class="truncate text-xs text-[#8b6f61]">
                <template v-if="row.lastMessage">
                  {{ row.lastMessage.kind === 'sticker' ? '傳送貼圖' : (row.lastMessage.text || '傳送訊息') }}
                </template>
                <template v-else>（尚無訊息）</template>
              </div>
            </div>

            <span
              v-if="row.unreadCount"
              class="ml-1 h-5 min-w-5 px-1.5 grid place-items-center rounded-full bg-[#b7791f] text-white text-[10px]"
            >
              {{ row.unreadCount }}
            </span>
          </button>

          <div
            v-if="!loadingList && !errorList && !listForView.length"
            class="p-4 text-sm text-[#8b6f61]"
          >
            尚無清單或搜尋無結果。
          </div>
        </div>
      </aside>

      <!-- 右側聊天室 -->
      <section
        class="flex min-h-0 flex-col rounded-xl border border-[#e0d6c8] bg-[#faf6f2]"
        :class="{ 'hidden md:flex': activeId === null }"
      >
        <!-- Header 固定高度 -->
        <div class="shrink-0 p-3 border-b border-[#e0d6c8] flex items-center justify-between">
          <div class="flex items-center gap-3">
            <button
              class="md:hidden inline-flex h-9 w-9 items-center justify-center rounded-full border border-[#e0d6c8] hover:bg-[#f0e5da]"
              title="返回清單"
              @click="activeId = null"
            >
              <i class="pi pi-arrow-left text-[#5c4033]"></i>
            </button>
            <div class="h-9 w-9 rounded-full bg-[#e8d8c7] grid place-items-center">
              <span class="text-xs text-[#5c4033] font-semibold">
                {{ (activeRow?.friendName || activeRow?.friendUid || '?').slice(0, 1).toUpperCase() }}
              </span>
            </div>
            <div>
              <div class="font-semibold leading-tight">
                {{ activeRow?.friendName || activeRow?.friendUid || '選擇一個會話' }}
                <span v-if="activeRow" class="ml-1 text-xs text-[#8b6f61]">@{{ activeRow.friendUid }}</span>
              </div>
              <div class="text-[11px] text-[#8b6f61]">已連線</div>
            </div>
          </div>

          <div class="flex items-center gap-2">
            <button
              class="inline-flex h-9 w-9 items-center justify-center rounded-full border border-[#e0d6c8] hover:bg-[#f0e5da]"
              title="更多"
            >
              <i class="pi pi-ellipsis-v text-[#5c4033]"></i>
            </button>
          </div>
        </div>

        <!-- Messages：真正的捲動區 -->
        <div
          ref="scroller"
          class="flex-1 min-h-0 overflow-y-auto px-3 py-4 space-y-3 bg-[rgba(247,243,239,0.6)]"
        >
          <div v-if="!activeMsgs.length" class="h-full grid place-items-center text-sm text-[#8b6f61]">
            選擇左側一個對話開始聊天
          </div>

          <template v-else>
            <!-- 用 template 當 wrapper，一次輸出「分隔線 row + 訊息 row」兩個同層節點 -->
            <template v-for="m in activeMsgs" :key="m.id">
              <!-- 分隔線：獨立一整排 -->
              <div
                v-if="currentUnreadFirstId && m.id === currentUnreadFirstId"
                id="unread-sep"
                class="w-full my-2"
              >
                <div class="flex items-center gap-2 text-[11px] text-[#8b6f61]">
                  <div class="flex-1 border-t border-[#e0d6c8]"></div>
                  <span class="px-2">未讀訊息</span>
                  <div class="flex-1 border-t border-[#e0d6c8]"></div>
                </div>
              </div>

              <!-- 訊息 row -->
              <div class="flex" :class="m.mine ? 'justify-end' : 'justify-start'">
                <!-- 外層：氣泡 + 狀態／時間；自己訊息反轉順序，讓狀態在左側 -->
                <div class="max-w-[80%] flex items-end gap-2" :class="m.mine ? 'flex-row-reverse' : ''">
                  <!-- 氣泡 -->
                  <div>
                    <div
                      v-if="m.kind === 'text'"
                      class="rounded-2xl px-3 py-2 text-sm"
                      :class="m.mine
                        ? 'bg-[#b7791f] text-white rounded-br-md'    /* 高對比色 */
                        : 'bg-white border border-[#e0d6c8] text-[#5c4033] rounded-bl-md'"
                    >
                      {{ m.text }}
                    </div>

                    <!-- 貼圖：笑臉 emoji -->
                    <div
                      v-else
                      class="rounded-2xl p-2"
                      :class="m.mine
                        ? 'bg-[#f3e6d9] border border-[#e8d8c7] rounded-br-md'
                        : 'bg-white border border-[#e0d6c8] rounded-bl-md'"
                    >
                      <div class="text-5xl leading-none text-center select-none" aria-label="貼圖">
                        {{ STICKER_EMOJI }}
                      </div>
                    </div>
                  </div>

                  <!-- 狀態 / 時間（自己在左、對方在右） -->
                  <div class="text-[11px] text-[#5c4033] whitespace-nowrap shrink-0">
                    {{ m.time }}
                    <span v-if="m.mine"> · {{ isReadByPeer(m.id) ? '已讀' : '已送出' }}</span>
                  </div>
                </div>
              </div>
            </template>
          </template>
        </div>

        <!-- Composer 固定高度 -->
        <div class="shrink-0 p-3 border-t border-[#e0d6c8]">
          <div class="flex items-center gap-2">
            <input
              v-model="draft"
              type="text"
              placeholder="輸入訊息…"
              class="flex-1 rounded-full border border-[#e0d6c8] bg-white px-4 py-2 text-sm text-[#5c4033] placeholder:text-[#8b6f61]/70 focus:ring-2 focus:ring-[#d4a373]/40 outline-none"
              @keydown.enter.prevent="sendText"
            />
            <button
              class="rounded-full bg-[#b7791f] hover:bg-[#9a651a] text-white px-4 py-2 text-sm disabled:opacity-50"
              title="傳送訊息"
              :disabled="!draft.trim() || activeId === null"
              @click="sendText"
            >
              傳送訊息
            </button>
            <button
              class="rounded-full border border-[#e0d6c8] px-4 py-2 text-sm disabled:opacity-50 hover:bg-[#f0e5da] text-[#5c4033]"
              title="傳送貼圖"
              :disabled="activeId === null"
              @click="sendSticker"
            >
              貼圖
            </button>
          </div>
        </div>
      </section>

    </div>
  </div>
</template>
