// src/services/friends.ts
import api from './http'

export type UserLite = { id: number; userId: string; username: string; avatar?: string ;alreadyFriend: boolean}

// 既有搜尋
export async function searchUsers(q: string): Promise<UserLite[]> {
  const { data } = await api.get<UserLite[]>('/users/search', { params: { q } })
  return data
}

// ✅ 送出好友邀請
export type FriendRequestDto = {
  id: number
  toUserId: string
  status: 'Pending'
  createdAt: string
  alreadyFriend: boolean
}

export async function sendFriendInvite(
  toUserId: string,
  message?: string
): Promise<FriendRequestDto> {
  const { data } = await api.post<FriendRequestDto>('friend/friend-requests', { toUserId, message })
  return data
}

export type FriendRequestItem = {
  requestId: number
  fromUserId: string // ⬅️ 加上（字串）
  toUserId: string // ⬅️ 字串
  message?: string
  createdAt: string
  status: 'Pending' // 這支只回 pending 就用單一字面型別
}

// ✅ 取得邀請清單：box = 'incoming'（我收到的）或 'outgoing'（我送出的），status 用小寫
export async function listFriendRequests(take = 20): Promise<FriendRequestItem[]> {
  const { data } = await api.get<FriendRequestItem[]>('/friend/friend-requests', {
    params: { take },
  })
  return data
}

// ✅ 取消我送出的 pending 邀請
export async function cancelFriendRequest(requestId: number): Promise<void> {
  await api.post(`/friend/friend-requests/${requestId}`)
}

// ✅ 接受我送出的 pending 邀請
export async function acceptFriendRequest(requestId: number): Promise<void> {
  await api.post(`/friend/friend-requests/${requestId}/accept`)
}

// ✅ 拒絕我送出的 pending 邀請
export async function rejectFriendRequest(requestId: number): Promise<void> {
  await api.post(`/friend/friend-requests/${requestId}/reject`)
}

export type FriendListItem = {
  id: number
  userId: string // ⬅️ 加上（字串）
  username: string // ⬅️ 字串
  createdAt: string

}
// 撈好友清單
export async function listFriends(take = 50): Promise<FriendListItem[]> {
  const { data } = await api.get('/friend/friends', {
    params: { take },
  })
  return data
}

// 之後要做好友邀請時再打開：
// export async function sendFriendInvite(toUserId: string) {
//   return api.post('/friend-requests', { toUserId })
// }
// export async function cancelFriendInvite(requestId: number) {
//   return api.delete(`/friend-requests/${requestId}`)
// }
