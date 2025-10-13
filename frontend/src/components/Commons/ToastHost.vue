<!-- src/components/ToastHost.vue -->
<script setup lang="ts">
import { useToast } from '../../stores/toast'
const { toasts } = useToast()

const kindClass = (k: string) => ({
  success: 'bg-emerald-600 text-white',
  error: 'bg-rose-600 text-white',
  info: 'bg-sky-600 text-white',
  warning: 'bg-amber-500 text-white',
}[k] || 'bg-gray-700 text-white')

const kindIcon = (k: string) => ({
  success: '✅',
  error: '✖️',
  info: 'ℹ️',
  warning: '⚠️',
}[k] || '🔔')
</script>

<template>
  <div class="fixed right-4 top-4 z-[1000] pointer-events-none" aria-live="polite">
    <!-- ★ TransitionGroup 讓新進/移除時有位移動畫；垂直堆疊 -->
    <TransitionGroup name="toast-stk" tag="div" class="space-y-2">
      <div
        v-for="t in toasts"
        :key="t.id"
        class="pointer-events-auto rounded-lg shadow-lg px-3 py-2 text-sm flex items-start gap-2"
        :class="kindClass(t.kind)"
        role="status"
      >
        <span class="select-none leading-tight mt-0.5">{{ kindIcon(t.kind) }}</span>
        <span class="leading-tight">{{ t.text }}</span>
        <!-- 沒有關閉按鈕 -->
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
/* 進出場與堆疊位移動畫 */
.toast-stk-enter-from,
.toast-stk-leave-to { opacity: 0; transform: translateY(-6px); }
.toast-stk-enter-active,
.toast-stk-leave-active { transition: all .18s ease; }
.toast-stk-move { transition: transform .18s ease; }
</style>
