import { createApp } from 'vue'
import App from './App.vue'
import router from './router'

import './style.css'

import PrimeVue from 'primevue/config'
import Aura from '@primevue/themes/aura'
import 'primeicons/primeicons.css'

import PvButton from 'primevue/button'
import InputText from 'primevue/inputtext'


const app = createApp(App)

app.use(PrimeVue, { theme: { preset: Aura } })
app.component('PvButton', PvButton)   // ✅ 多字、非保留名
app.component('InputText', InputText)
app.use(router)
app.mount('#app')