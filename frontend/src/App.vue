<!-- <script setup lang="ts">
import HelloWorld from './components/HelloWorld.vue'
</script>

<template>
  <div>
    <a href="https://vite.dev" target="_blank">
      <img src="/vite.svg" class="logo" alt="Vite logo" />
    </a>
    <a href="https://vuejs.org/" target="_blank">
      <img src="./assets/vue.svg" class="logo vue" alt="Vue logo" />
    </a>
  </div>
  <HelloWorld msg="Vite + Vue" />
</template>

<style scoped>
.logo {
  height: 6em;
  padding: 1.5em;
  will-change: filter;
  transition: filter 300ms;
}
.logo:hover {
  filter: drop-shadow(0 0 2em #646cffaa);
}
.logo.vue:hover {
  filter: drop-shadow(0 0 2em #42b883aa);
}
</style> -->

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getForecast } from './services/http'

const data = ref<any>(null)
const err = ref<string | null>(null)

onMounted(async () => {
  try {
    data.value = await getForecast()
  } catch (e: any) {
    err.value = e?.message ?? String(e)
  }
})
</script>

<template>
  <main style="padding: 24px">
    <h1>Vue 3 + .NET 8 API</h1>
    <p v-if="err">Error: {{ err }}</p>
    <pre v-else>{{ data }}</pre>
  </main>
</template>
