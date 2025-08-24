import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import HomePage from '../views/HomePage.vue'   // ← 先用靜態匯入
import UsersPage from '../views/UsersPage.vue'
import Login from '../views/Login.vue'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: HomePage},
  { path: '/users', name: 'users', component: UsersPage},
   { path: '/login',name: 'login', component: Login }, // 🔑 新增
]

const router = createRouter({
  history: createWebHistory(), // URL 不帶 #，可配合後端設定
  routes,
})

export default router
