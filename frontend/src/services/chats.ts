// src/services/chats.ts
import api from './http'

export type LastMessage = {
  id: number
  kind: 'text' | 'sticker' | 'file'
  text?: string
  time: string               // ISO 字串
  mine: boolean              // 是否我送的
}

export type FriendInboxRow = {
  friendId: number
  friendUid: string
  friendName: string
  conversationId: number 
  lastMessage?: LastMessage | null
  readUpToMessageId?: number | null
  unreadCount: number
}

/** 左側清單：好友 + 最後訊息 + 未讀數（由後端彙整） */
export async function listFriendsInbox(take = 100): Promise<FriendInboxRow[]> {
  const { data } = await api.get('/chat/friends-inbox', { params: { take } })
  return data
}

/** DM 保證存在（沒有就建立） */
export async function ensureDmByUid(peerUid: string): Promise<{ conversationId: number }> {
  const { data } = await api.post(`/chat/dm/by-userid/${encodeURIComponent(peerUid)}`)
  return data
}

export type MessageItem = {
  id: number
  kind: 'text' | 'sticker' | 'file'
  text?: string
  time: string
  mine: boolean
}

/** 取訊息列表 */
type MessageQueryParams = {
  take: number
  afterId?: number
  beforeId?: number
}

export async function listMessages(
  conversationId: number,
  afterId?: number,
  take = 50,
  opts?: { beforeId?: number }
): Promise<MessageItem[]> {
  const params: MessageQueryParams = { take }
  if (afterId !== undefined) params.afterId = afterId
  if (opts?.beforeId !== undefined) params.beforeId = opts.beforeId

  const { data } = await api.get(`/chat/conversations/${conversationId}/messages`, { params })
  return data
}

// services/chats.ts
export async function listMessagesBefore(
  conversationId: number,
  beforeId: number,
  take = 50
): Promise<MessageItem[]> {
  const { data } = await api.get(`/chat/conversations/${conversationId}/messages`, {
    params: { beforeId, take },
  })
  return data
}

/** 進入即已讀：把游標推到已載入的最後一封 */
export async function setReadUpTo(conversationId: number, messageId: number): Promise<void> {
  await api.post(`/chat/conversations/${conversationId}/read-up-to`, { messageId })
}

/* ========= 可選：真正送訊息時改用下列 API（現在頁面先本地模擬） ========= */
export async function sendTextMessage(
  conversationId: number,
  text: string
): Promise<MessageItem> {
  const { data } = await api.post(`/chat/conversations/${conversationId}/messages`, {
    kind: 'text',
    text,
  })
  return data
}

export async function sendStickerMessage(
  conversationId: number,
  stickerId: string
): Promise<MessageItem> {
  const { data } = await api.post(`/chat/conversations/${conversationId}/messages`, {
    kind: 'sticker',
    stickerId,
  })
  return data
}


// 取 DM 對方的已讀游標
export type DmPeerRead = {
  peerUserId: number | null
  readUpToMessageId: number | null
  lastReadAt: string | null
  serverNow: string
}

export async function getDmPeerRead(
  conversationId: number
): Promise<DmPeerRead> {
  const { data } = await api.get(
    `/chat/conversations/${conversationId}/dm-peer-read`
  )
  // 做一次保底正規化，避免缺欄位時出錯
  return {
    peerUserId: data?.peerUserId ?? null,
    readUpToMessageId: data?.readUpToMessageId ?? null,
    lastReadAt: data?.lastReadAt ?? null,
    serverNow: data?.serverNow ?? new Date().toISOString(),
  }
}

