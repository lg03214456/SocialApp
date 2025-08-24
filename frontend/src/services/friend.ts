// src/services/friend.ts
import api from './http'

export async function getForecast() {
  const { data } = await api.get('/weatherforecast')
  return data
}

export async function getFriends() {
  const { data } = await api.get('/friends')
  return data
}

export async function addFriend(name: string) {
  const { data } = await api.post('/friends', { name })
  return data
}
