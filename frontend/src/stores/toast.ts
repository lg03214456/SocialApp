// src/stores/toast.ts
import { ref } from 'vue'

export type ToastKind = 'success' | 'error' | 'info' | 'warning'
export type ToastItem = { id: number; kind: ToastKind; text: string; duration: number }

const toasts = ref<ToastItem[]>([])
const MAX_STACK = 4  // 同時最多顯示幾個（可調）

function push(kind: ToastKind, text: string, duration = 2200) {
  const id = Date.now() + Math.random()
  const item: ToastItem = { id, kind, text, duration }

  // ★ 新的放最上面
  toasts.value.unshift(item)

  // 超過上限就砍掉最下面那個
  if (toasts.value.length > MAX_STACK) {
    const removed = toasts.value.pop()
    // 若想安全一點，可以在移除前清掉它的 timer（這裡 timer 在 setTimeout 中）
    void removed
  }

  // 各自倒數到時間自動移除
  window.setTimeout(() => remove(id), duration)
}

function remove(id: number) {
  const i = toasts.value.findIndex(t => t.id === id)
  if (i >= 0) toasts.value.splice(i, 1)
}

export function useToast() {
  return {
    toasts,
    showSuccess: (text: string, ms?: number) => push('success', text, ms),
    showError:   (text: string, ms?: number) => push('error',   text, ms),
    showInfo:    (text: string, ms?: number) => push('info',    text, ms),
    showWarn:    (text: string, ms?: number) => push('warning', text, ms),
    remove,
  }
}
