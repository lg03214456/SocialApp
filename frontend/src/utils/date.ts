// src/utils/date.ts
export function fmtChatTime(iso: string) {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  const hh = String(d.getHours()).padStart(2, '0')
  const mi = String(d.getMinutes()).padStart(2, '0')
  return `${mm}/${dd} ${hh}:${mi}`
}
/** 時間格式化（與 DateTimeOffset ISO 相容） */
export function fmt(iso: string) {
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleString()
}

// utils/date.ts
/** 中文 12 小時制：上午/下午 + 時:分 */
export function fmtChatTimeZh12(iso: string) {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  const str = d.toLocaleTimeString('zh-TW', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  })
  // 例：上午9:05 → 上午 9:05（加空格可讀性更好）
  return str.replace(/(上午|下午)/, '$1 ')
}
// utils/date.ts
export function fmtChatDateLabel(iso: string) {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const day = new Date(d)
  day.setHours(0, 0, 0, 0)
  const diff = Math.floor((today.getTime() - day.getTime()) / 86400000)
  if (diff === 0) return '今天'
  if (diff === 1) return '昨天'
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${y}/${m}/${dd}`
}

export function fmtInboxTime(iso: string) {
  const date = fmtChatDateLabel(iso)        // 今天 / 昨天 / 2025/10/06
  const time = fmtChatTimeZh12(iso)         // 上午 9:05 / 下午 3:27
  if (!date) return time
  if (date === '今天') return time
  if (date === '昨天') return `昨天 ${time}`
  return `${date} ${time}`
}