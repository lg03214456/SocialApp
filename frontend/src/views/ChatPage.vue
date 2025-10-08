<script setup lang="ts">
/* ============================================================
 * Imports
 * ============================================================ */
import { fmtChatTimeZh12, fmtChatDateLabel } from '../utils/date'
import { ref, computed, watch, nextTick, onMounted } from 'vue'
import {
  listFriendsInbox,
  ensureDmByUid,
  listMessages, // afterId（未讀/較新）
  listMessagesBefore, // beforeId（已讀/較舊）
  setReadUpTo,
  sendTextMessage,
  getDmPeerRead, // 取得對方已讀游標
  type FriendInboxRow,
  type MessageItem,
} from '../services/chats'

/* ============================================================
 * Types
 * ============================================================ */
type ChatKind = 'text' | 'sticker' | 'file'

type Msg = {
  id: number
  kind: ChatKind
  text?: string
  time: string // UI 顯示時間
  date: string // UI 日期標籤
  mine: boolean
}

/** 最愛以「訊息」為單位 */
type Favorite = {
  cid: number // conversationId
  mid: number // messageId
  preview: string // 當下存的摘要（避免未載入該訊息時沒東西可顯示）
  timeIso: string // 該訊息的 ISO 時間（顯示用）
}

/* ============================================================
 * Constants
 * ============================================================ */
const STICKER_EMOJI = '😊'

/* ============================================================
 * Reactive State
 * ============================================================ */
const inbox = ref<FriendInboxRow[]>([])
const loadingList = ref(false)
const errorList = ref<string | null>(null)

const activeRow = ref<FriendInboxRow | null>(null)
const activeId = ref<number | null>(null)
const q = ref('')
const draft = ref('')
const scroller = ref<HTMLDivElement | null>(null)

const messagesMap = ref<Record<number, Msg[]>>({})
const unreadFirstIdMap = ref<Record<number, number | null>>({})
const peerReadUpToMap = ref<Record<number, number | null>>({})

type Tab = 'all' | 'bookmarks'
const tab = ref<Tab>('all')

/** ⭐ 以訊息為單位的最愛清單（先用前端；之後可換 API） */
const favorites = ref<Favorite[]>([])

/** [ADDED] 在定位流程中抑制自動捲到底 */
const suppressAutoScroll = ref(false)

/** [ADDED] 置中捲動後短暫高亮的訊息 id */
const highlightMid = ref<number | null>(null)

/* ============================================================
 * Derived
 * ============================================================ */
const listForView = computed(() => {
  const kw = q.value.toLowerCase()
  return inbox.value.filter((r) => {
    const name = (r.friendName ?? '').toLowerCase()
    const uid = (r.friendUid ?? '').toLowerCase()
    const lm = (r.lastMessage?.text ?? r.lastMessage?.kind ?? '').toLowerCase()
    return !kw || name.includes(kw) || uid.includes(kw) || lm.includes(kw)
  })
})

const activeMsgs = computed(() => (activeId.value ? (messagesMap.value[activeId.value] ?? []) : []))

const currentUnreadFirstId = computed(() =>
  activeId.value ? (unreadFirstIdMap.value[activeId.value] ?? null) : null
)

/** 將 favorites join 左側清單資訊，供最愛頁籤渲染 */
const byCid = computed(() => new Map(inbox.value.map((r) => [r.conversationId ?? -1, r])))
const favoriteView = computed(() => {
  return favorites.value
    .map((f) => {
      const row = byCid.value.get(f.cid)
      return row ? { f, row } : null
    })
    .filter((x): x is { f: Favorite; row: FriendInboxRow } => !!x)
})

/** 右鍵選單的「加入/移除最愛」文字（依目前右鍵所指訊息判斷） */
const favoriteMenuText = computed(() => {
  const cid = activeId.value
  const mid = ctx.value.msgId
  if (!cid || !mid) return '加入最愛'
  return favorites.value.some((x) => x.cid === cid && x.mid === mid) ? '移除最愛' : '加入最愛'
})

/* ============================================================
 * Helpers
 * ============================================================ */
function isReadByPeer(msgId: number): boolean {
  const cid = activeId.value
  if (!cid) return false
  const peerUpTo = peerReadUpToMap.value[cid]
  return peerUpTo != null && msgId <= peerUpTo
}

/** 將 MessageItem 轉 UI Msg */
const toMsg = (m: MessageItem): Msg => ({
  id: m.id,
  kind: m.kind as ChatKind,
  text: m.text,
  time: fmtChatTimeZh12(m.time),
  date: fmtChatDateLabel(m.time),
  mine: m.mine,
})

/** [CHANGED] 嘗試捲到指定訊息 id；支援置中（center） */
function scrollToMsg(
  mid: number,
  opts: { smooth?: boolean; center?: boolean } = { smooth: true, center: false }
): boolean {
  const el = document.getElementById(`msg-${mid}`)
  const box = scroller.value
  if (el && box) {
    const smooth = opts.smooth ?? true
    const center = opts.center ?? false

    let top = el.offsetTop - 40
    if (center) {
      // [ADDED] 置中演算法
      const targetTop = el.offsetTop - (box.clientHeight - el.clientHeight) / 2
      top = Math.max(0, Math.min(targetTop, box.scrollHeight - box.clientHeight))
    }
    box.scrollTo({ top, behavior: smooth ? 'smooth' : 'auto' })

    const label = (el as HTMLDivElement).dataset.date || ''
    if (label) showDateHintOnce(label)
    return true
  }
  return false
}

/** [ADDED] 置中並短暫高亮目標訊息 */
async function centerAndHighlight(mid: number) {
  const ok = scrollToMsg(mid, { smooth: true, center: true })
  if (ok) {
    highlightMid.value = mid
    window.setTimeout(() => {
      if (highlightMid.value === mid) highlightMid.value = null
    }, 1200)
  }
  return ok
}

/** [CHANGED→NEW] 雙向載入直到包含特定訊息 id 為止；期間抑制自動捲動 */
async function ensureMessageLoaded(
  cid: number,
  targetMid: number,
  maxRounds = 10
): Promise<boolean> {
  suppressAutoScroll.value = true // [ADDED] 避免 watch(activeMsgs) 把定位覆蓋
  try {
    let list = messagesMap.value[cid] ?? []
    if (scrollToMsg(targetMid, { smooth: false, center: true })) return true

    for (let round = 0; round < maxRounds; round++) {
      list = messagesMap.value[cid] ?? []
      if (!list.length) break

      const earliest = list[0]?.id ?? Number.MAX_SAFE_INTEGER
      const latest = list[list.length - 1]?.id ?? -1

      // 1) 目標比目前最舊還舊 → 往更舊載
      if (targetMid < earliest) {
        const older = await listMessagesBefore(cid, earliest, 100)
        if (!older.length) break
        messagesMap.value[cid] = [...older.map(toMsg), ...list]
        await nextTick()
        if (scrollToMsg(targetMid, { smooth: false, center: true })) return true
        continue
      }

      // 2) 目標比目前最新還新 → 往更新載
      if (targetMid > latest) {
        const newer = await listMessages(cid, latest, 100) // afterId=latest
        if (!newer.length) break
        messagesMap.value[cid] = [...list, ...newer.map(toMsg)]
        await nextTick()
        if (scrollToMsg(targetMid, { smooth: false, center: true })) return true
        continue
      }

      // 3) 已在範圍內但可能還沒渲染好 → 再試一次
      await nextTick()
      if (scrollToMsg(targetMid, { smooth: false, center: true })) return true
      break
    }

    await nextTick()
    return scrollToMsg(targetMid, { smooth: false, center: true })
  } finally {
    suppressAutoScroll.value = false // [ADDED] 恢復自動捲動
  }
}

/* ============================================================
 * Data I/O
 * ============================================================ */
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

/** [CHANGED] 支援 skipScroll，避免干擾我的最愛精準定位 */
async function openFromRow(row: FriendInboxRow, opts?: { skipScroll?: boolean }) {
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

  // 3) 以 readUpTo 為中線載入
  const R = row.readUpToMessageId ?? 0
  const older: MessageItem[] = R > 0 ? await listMessagesBefore(cid!, R + 1, 50) : []
  const newer: MessageItem[] = await listMessages(cid!, R || undefined, 50)

  messagesMap.value[cid!] = [...older.map(toMsg), ...newer.map(toMsg)]
  unreadFirstIdMap.value[cid!] = newer.length ? newer[0].id : null

  // 4) 進入即已讀
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

  // 5) 對方已讀游標
  try {
    const pr = await getDmPeerRead(cid!)
    peerReadUpToMap.value[cid!] = pr.readUpToMessageId ?? null
  } catch {
    peerReadUpToMap.value[cid!] = null
  }

  // 6) 捲到分隔線或底（可跳過）
  await nextTick()
  if (!opts?.skipScroll) {
    // [ADDED]
    const sepEl = document.getElementById('unread-sep')
    if (sepEl && scroller.value) scroller.value.scrollTop = sepEl.offsetTop - 40
    else scroller.value?.scrollTo({ top: scroller.value.scrollHeight, behavior: 'smooth' })
  }
}

/** 本地加入訊息（送訊息時用） */
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
  unreadFirstIdMap.value[cid] = null

  const i = inbox.value.findIndex((x) => x.friendId === row.friendId)
  if (i >= 0) {
    const [picked] = inbox.value.splice(i, 1)
    inbox.value.unshift(picked)
  }
}

async function sendText() {
  if (!draft.value.trim() || activeId.value === null || !activeRow.value) return
  const text = draft.value.trim()
  const tempId = Date.now()

  const nowIso = new Date().toISOString()
  const tempMsg: Msg = {
    id: tempId,
    kind: 'text',
    text,
    time: fmtChatTimeZh12(nowIso),
    date: fmtChatDateLabel(nowIso),
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
      time: fmtChatTimeZh12(saved.time),
      date: fmtChatDateLabel(saved.time),
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

    try {
      const pr = await getDmPeerRead(cid!)
      peerReadUpToMap.value[cid!] = pr.readUpToMessageId ?? null
    } catch (e) {
      void e
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

function sendSticker() {
  if (activeId.value === null || !activeRow.value) return
  const nowIso = new Date().toISOString()
  const msg: Msg = {
    id: Date.now(),
    kind: 'sticker',
    time: fmtChatTimeZh12(nowIso),
    date: fmtChatDateLabel(nowIso),
    mine: true,
  }
  liftAndPreview(activeRow.value, msg)
}

/* ============================================================
 * Context Menu
 * ============================================================ */
type CtxState = {
  show: boolean
  x: number
  y: number
  mine: boolean
  kind: ChatKind
  msgId: number | null
}
const ctx = ref<CtxState>({ show: false, x: 0, y: 0, mine: false, kind: 'text', msgId: null })

function closeCtxMenu() {
  ctx.value.show = false
}

function onMsgContextMenu(m: Msg, e: MouseEvent) {
  e.preventDefault()
  const vw = window.innerWidth,
    vh = window.innerHeight
  const P = 8,
    MENU_W = 224,
    MENU_H = 36 * 7
  let x = e.clientX,
    y = e.clientY
  if (x + MENU_W > vw - P) x = Math.max(P, vw - MENU_W - P)
  if (y + MENU_H > vh - P) y = Math.max(P, vh - MENU_H - P)
  ctx.value = { show: true, x, y, mine: m.mine, kind: m.kind, msgId: m.id }
}

/** 以目前右鍵選到的訊息，切換加入/移除最愛 */
function toggleFavoriteFromCtx() {
  const cid = activeId.value
  const mid = ctx.value.msgId
  if (!cid || !mid) return

  const exists = favorites.value.findIndex((x) => x.cid === cid && x.mid === mid)
  if (exists >= 0) {
    favorites.value.splice(exists, 1)
    return
  }

  // 準備 preview（優先用當前 messagesMap 找到的那則）
  const list = messagesMap.value[cid] ?? []
  const msg = list.find((m) => m.id === mid)
  const preview = msg
    ? msg.kind === 'text'
      ? msg.text || '(空白訊息)'
      : msg.kind === 'sticker'
        ? '貼圖'
        : '檔案'
    : '訊息'
  const timeIso = msg ? new Date().toISOString() : new Date().toISOString()

  favorites.value.unshift({ cid, mid, preview, timeIso })
}

/** [CHANGED] 點擊最愛 → 先切到 all、載入、置中並高亮，不覆蓋定位 */
async function openFavorite(f: Favorite) {
  suppressAutoScroll.value = true

  // [ADDED] 先切到「全部」確保聊天 DOM 掛載
  if (tab.value !== 'all') {
    tab.value = 'all'
    await nextTick()
  }

  const row = inbox.value.find((r) => r.conversationId === f.cid)
  if (!row) {
    // [FIX] 早退前恢復旗標
    suppressAutoScroll.value = false
    return
  }

  await openFromRow(row, { skipScroll: true }) // [ADDED] 避免 openFromRow 自動捲動干擾
  await nextTick()

  // 先嘗試現有範圍中置中定位
  let ok = scrollToMsg(f.mid, { center: true })
  if (!ok) {
    // 雙向載到包含為止
    ok = await ensureMessageLoaded(f.cid, f.mid)
    await nextTick()
    scrollToMsg(f.mid, { center: true })
  }

  // 高亮提示
  centerAndHighlight(f.mid)

  await nextTick()
  suppressAutoScroll.value = false
}

function onMenu(action: string) {
  closeCtxMenu()
  switch (action) {
    case 'reply':
      /* TODO */ break
    case 'copy':
      /* TODO */ break
    case 'forward':
      /* TODO */ break
    case 'favorite':
      toggleFavoriteFromCtx()
      break
    case 'edit':
      /* TODO */ break
    case 'delete':
      /* TODO */ break
    case 'report':
      /* TODO */ break
    case 'block':
      /* TODO */ break
  }
}

/* ============================================================
 * Scrolling & Date Hint
 * ============================================================ */
const visibleDateHint = ref<string | null>(null)
let dateHideTimer: number | undefined
function showDateHintOnce(s: string) {
  if (!s) return
  visibleDateHint.value = s
  if (dateHideTimer) window.clearTimeout(dateHideTimer)
  dateHideTimer = window.setTimeout(() => (visibleDateHint.value = null), 2000)
}

function handleScrollDateHint() {
  const el = scroller.value
  if (!el) return
  const bottom = el.scrollTop + el.clientHeight
  const rows = Array.from(el.querySelectorAll<HTMLDivElement>('.msg-row'))
  let picked: HTMLDivElement | null = null
  for (const r of rows) {
    const top = r.offsetTop
    if (top <= bottom) picked = r
    else break
  }
  if (picked) showDateHintOnce(picked.dataset.date || '')
}

/* ============================================================
 * Lifecycle & Watchers
 * ============================================================ */
onMounted(async () => {
  await loadInbox()
  window.addEventListener('keydown', (e: KeyboardEvent) => {
    if (e.key === 'Escape') closeCtxMenu()
  })
  scroller.value?.addEventListener('scroll', handleScrollDateHint, { passive: true })
  scroller.value?.addEventListener('scroll', closeCtxMenu, { passive: true })
})

/** [CHANGED] 若定位流程中，避免自動捲到底覆蓋置中 */
watch(activeMsgs, async () => {
  await nextTick()
  if (suppressAutoScroll.value) return
  scroller.value?.scrollTo({ top: scroller.value.scrollHeight, behavior: 'smooth' })
  handleScrollDateHint()
})

watch(tab, (t) => {
  if (t === 'bookmarks') closeCtxMenu()
})
</script>

<template>
  <div class="h-[85vh] md:h-[90vh] max-w-7xl mx-auto p-3 md:p-4 bg-[#f7f3ef] text-[#5c4033]">
    <div class="grid h-full min-h-0 gap-3 md:grid-cols-[320px_1fr]">
      <!-- 左側清單 -->
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

        <div class="flex-1 min-h-0 overflow-y-auto">
          <div v-if="loadingList" class="p-4 text-sm text-[#8b6f61] flex items-center gap-2">
            <i class="pi pi-spinner pi-spin"></i> 載入清單…
          </div>
          <div v-else-if="errorList" class="p-4 text-sm text-red-600">{{ errorList }}</div>

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
                  {{ row.lastMessage ? fmtChatTimeZh12(row.lastMessage.time) : '' }}
                </div>
              </div>
              <div class="truncate text-xs text-[#8b6f61]">
                <template v-if="row.lastMessage">
                  {{
                    row.lastMessage.kind === 'sticker'
                      ? '傳送貼圖'
                      : row.lastMessage.text || '傳送訊息'
                  }}
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

      <!-- 右側主視窗 -->
      <section
        class="relative flex min-h-0 flex-col rounded-xl border border-[#e0d6c8] bg-[#faf6f2]"
        :class="{ 'hidden md:flex': activeId === null }"
      >
        <!-- 日期提示 -->
        <div
          v-if="visibleDateHint"
          class="pointer-events-none absolute top-16 left-1/2 -translate-x-1/2 bg-[#5c4033]/80 text-white text-xs px-3 py-1 rounded-full shadow transition-opacity"
        >
          {{ visibleDateHint }}
        </div>

        <!-- Header -->
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
                {{
                  (activeRow?.friendName || activeRow?.friendUid || '?').slice(0, 1).toUpperCase()
                }}
              </span>
            </div>
            <div>
              <div class="font-semibold leading-tight">
                {{ activeRow?.friendName || activeRow?.friendUid || '選擇一個會話' }}
                <span v-if="activeRow" class="ml-1 text-xs text-[#8b6f61]"
                  >@{{ activeRow.friendUid }}</span
                >
              </div>
              <div class="text-[11px] text-[#8b6f61]">已連線</div>
            </div>
          </div>

          <div class="flex items-center gap-2">
            <!-- 頁籤：全部 / 最愛 -->
            <div
              class="hidden md:flex items-center rounded-full border border-[#e0d6c8] overflow-hidden"
            >
              <button
                class="px-3 py-1 text-xs"
                :class="tab === 'all' ? 'bg-[#f0e5da] text-[#5c4033]' : 'text-[#8b6f61]'"
                title="顯示全部對話"
                @click="tab = 'all'"
              >
                全部
              </button>
              <button
                class="px-3 py-1 text-xs"
                :class="tab === 'bookmarks' ? 'bg-[#f0e5da] text-[#5c4033]' : 'text-[#8b6f61]'"
                title="只看最愛"
                @click="tab = 'bookmarks'"
              >
                最愛
              </button>
            </div>

            <!-- 更多（之後要加 header 選單也可以放這裡） -->
            <button
              class="inline-flex h-9 w-9 items-center justify-center rounded-full border border-[#e0d6c8] hover:bg-[#f0e5da]"
              title="更多"
            >
              <i class="pi pi-ellipsis-v text-[#5c4033]"></i>
            </button>
          </div>
        </div>

        <!-- A) 全部：聊天室 -->
        <div
          v-if="tab === 'all'"
          ref="scroller"
          class="flex-1 min-h-0 overflow-y-auto px-3 py-4 space-y-3 bg-[rgba(247,243,239,0.6)]"
        >
          <div
            v-if="!activeMsgs.length"
            class="h-full grid place-items-center text-sm text-[#8b6f61]"
          >
            選擇左側一個對話開始聊天
          </div>

          <template v-else>
            <template v-for="m in activeMsgs" :key="m.id">
              <!-- 未讀分隔線 -->
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

              <!-- 訊息 row（加上 id 供捲動定位） -->
              <div
                :id="`msg-${m.id}`"
                :data-date="m.date"
                class="flex msg-row"
                :class="m.mine ? 'justify-end' : 'justify-start'"
                @contextmenu="onMsgContextMenu(m, $event)"
              >
                <div
                  class="max-w-[80%] flex items-end gap-2"
                  :class="m.mine ? 'flex-row-reverse' : ''"
                >
                  <div>
                    <!-- [CHANGED] 高亮：當 highlightMid 命中時加上 ring -->
                    <div
                      v-if="m.kind === 'text'"
                      class="rounded-2xl px-3 py-2 text-sm"
                      :class="[
                        m.mine
                          ? 'bg-[#b7791f] text-white rounded-br-md'
                          : 'bg-white border border-[#e0d6c8] text-[#5c4033] rounded-bl-md',
                        highlightMid === m.id
                          ? 'ring-2 ring-[#d4a373] ring-offset-2 ring-offset-[rgba(247,243,239,0.6)] animate-pulse'
                          : '',
                      ]"
                    >
                      {{ m.text }}
                    </div>
                    <div
                      v-else
                      class="rounded-2xl p-2"
                      :class="[
                        m.mine
                          ? 'bg-[#f3e6d9] border border-[#e8d8c7] rounded-br-md'
                          : 'bg-white border border-[#e0d6c8] rounded-bl-md',
                        highlightMid === m.id ? 'ring-2 ring-[#d4a373]' : '',
                      ]"
                    >
                      <div class="text-5xl leading-none text-center select-none" aria-label="貼圖">
                        {{ STICKER_EMOJI }}
                      </div>
                    </div>
                  </div>

                  <div class="text-[11px] text-[#5c4033] whitespace-nowrap shrink-0">
                    {{ m.time }}
                    <span v-if="m.mine"> · {{ isReadByPeer(m.id) ? '已讀' : '已送出' }}</span>
                  </div>
                </div>
              </div>
            </template>
          </template>
        </div>

        <!-- B) 最愛 -->
        <div
          v-else
          class="flex-1 min-h-0 overflow-y-auto px-3 py-4 space-y-2 bg-[rgba(247,243,239,0.6)]"
        >
          <div class="flex items-center justify-between mb-2">
            <div class="text-xs text-[#8b6f61]">我的最愛（{{ favoriteView.length }}）</div>
            <button
              class="px-2 py-1 text-xs rounded-full border border-[#e0d6c8] text-[#5c4033] hover:bg-[#f0e5da]"
              @click="tab = 'all'"
              title="回到聊天"
            >
              回到聊天
            </button>
          </div>

          <div
            v-if="!favoriteView.length"
            class="h-full grid place-items-center text-sm text-[#8b6f61]"
          >
            尚未加入最愛。對訊息按右鍵 → 「加入最愛」即可加入。
          </div>

          <button
            v-for="item in favoriteView"
            :key="`fav-${item.f.cid}-${item.f.mid}`"
            class="w-full flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-[#f0e5da] text-left border border-transparent"
            @click="openFavorite(item.f)"
          >
            <div class="h-10 w-10 rounded-full bg-[#e8d8c7] grid place-items-center shrink-0">
              <span class="text-xs text-[#5c4033] font-semibold">
                {{ (item.row.friendName || item.row.friendUid).slice(0, 1).toUpperCase() }}
              </span>
            </div>

            <div class="min-w-0 flex-1">
              <div class="flex items-center justify-between gap-2">
                <div class="truncate font-medium">
                  {{ item.row.friendName || item.row.friendUid }}
                  <span class="text-xs text-[#8b6f61]">@{{ item.row.friendUid }}</span>
                </div>
                <div class="text-[11px] text-[#8b6f61] shrink-0">
                  {{ fmtChatTimeZh12(item.f.timeIso) }}
                </div>
              </div>
              <div class="truncate text-xs text-[#8b6f61]">
                {{ item.f.preview }}
              </div>
            </div>

            <i class="pi pi-star-fill text-[#b7791f] text-xs ml-1" title="已加入最愛"></i>

            <span
              v-if="item.row.unreadCount"
              class="ml-1 h-5 min-w-5 px-1.5 grid place-items-center rounded-full bg-[#b7791f] text-white text-[10px]"
            >
              {{ item.row.unreadCount }}
            </span>
          </button>
        </div>

        <!-- 右鍵選單 -->
        <div v-if="ctx.show" data-ctx-menu class="fixed inset-0 z-[60]">
          <div class="absolute inset-0" @click="closeCtxMenu"></div>
          <div
            class="menu absolute z-[61] w-56 rounded-xl shadow-xl ring-1 ring-black/10 bg-white !text-gray-800 isolate"
            :style="{ left: ctx.x + 'px', top: ctx.y + 'px' }"
            @click.stop
          >
            <ul class="py-2 m-0 list-none">
              <li>
                <button
                  class="menu-item w-full text-left px-3 py-2 hover:bg-black/5"
                  @click="onMenu('reply')"
                >
                  ↩ 回覆
                </button>
              </li>
              <li>
                <button
                  class="menu-item w-full text-left px-3 py-2 hover:bg-black/5"
                  :disabled="ctx.kind !== 'text'"
                  @click="onMenu('copy')"
                >
                  📋 複製文字
                </button>
              </li>
              <li>
                <button
                  class="menu-item w-full text-left px-3 py-2 hover:bg-black/5"
                  @click="onMenu('forward')"
                >
                  ↗ 轉傳…
                </button>
              </li>
              <li>
                <button
                  class="menu-item w-full text-left px-3 py-2 hover:bg-black/5"
                  @click="onMenu('favorite')"
                >
                  {{ favoriteMenuText }}
                </button>
              </li>

              <li v-if="ctx.mine" class="h-px my-1 mx-2 bg-black/10"></li>
              <li v-if="ctx.mine">
                <button
                  class="menu-item w-full text-left px-3 py-2 hover:bg-black/5"
                  @click="onMenu('edit')"
                >
                  ✏ 編輯訊息
                </button>
              </li>
              <li v-if="ctx.mine">
                <button
                  class="menu-item w-full text-left px-3 py-2 hover:bg-red-500/10"
                  @click="onMenu('delete')"
                >
                  🗑 刪除訊息…
                </button>
              </li>

              <li v-if="!ctx.mine" class="h-px my-1 mx-2 bg-black/10"></li>
              <li v-if="!ctx.mine">
                <button
                  class="menu-item w-full text-left px-3 py-2 hover:bg-black/5"
                  @click="onMenu('report')"
                >
                  ⚠ 檢舉…
                </button>
              </li>
              <li v-if="!ctx.mine">
                <button
                  class="menu-item w-full text-left px-3 py-2 hover:bg-black/5"
                  @click="onMenu('block')"
                >
                  ⛔ 封鎖
                </button>
              </li>
            </ul>
          </div>
        </div>

        <!-- Composer -->
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
              class="inline-flex items-center justify-center whitespace-nowrap font-medium rounded-full px-4 py-2 text-sm bg-[#b7791f] text-white hover:bg-[#9a651a] disabled:bg-[#e8d8c7] disabled:text-[#5c4033] disabled:opacity-100 disabled:cursor-not-allowed"
              title="傳送訊息"
              :disabled="!draft.trim() || activeId === null"
              @click="sendText"
            >
              傳送訊息
            </button>
            <button
              class="inline-flex items-center justify-center whitespace-nowrap font-medium rounded-full px-4 py-2 text-sm border border-[#e0d6c8] text-[#5c4033] hover:bg-[#f0e5da] disabled:text-[#8b6f61] disabled:border-[#e0d6c8] disabled:opacity-80 disabled:cursor-not-allowed"
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
