<script setup lang="ts">
// import { ref, onMounted } from 'vue'

</script>


<!-- <template>
  <header class="border-b p-4 flex gap-4">
    <RouterLink to="/" class="hover:underline">Home</RouterLink>
    <RouterLink to="/users" class="hover:underline">Users</RouterLink>
  </header>
  <RouterView />
  
</template> -->

<!-- <template>
  <div class="min-h-screen flex flex-col">
    <header class="border-b p-4 flex gap-4">
      <RouterLink to="/" class="hover:underline">Home</RouterLink>
      <RouterLink to="/users" class="hover:underline">Users</RouterLink>
    </header>

    <main class="flex-1 p-4">
      <RouterView />
    </main>

    <footer class="border-t p-4 text-center">
      © 2025 SocialApp
    </footer>
  </div>
</template> -->
<template>
  <div class="min-h-screen flex flex-col">
    <!-- Top Nav -->
    <header class="border-b px-4 py-3 flex items-center justify-between">
      <nav class="flex items-center gap-4">
        <RouterLink to="/" class="hover:underline">Home</RouterLink>
        <RouterLink to="/users" class="hover:underline">Users</RouterLink>
        <RouterLink to="/login" class="hover:underline">Login</RouterLink>
      </nav>
      <div class="flex items-center gap-2">
        <InputText placeholder="Search..." class="w-40 md:w-64" />
        <PvButton label="New" icon="pi pi-plus" />
      </div>
    </header>

    <!-- Main content -->
    <main class="flex-1 p-4 md:p-6">
      <RouterView />
    </main>

    <!-- Footer -->
    <footer class="border-t px-4 py-3 text-center text-sm text-gray-500">
      © 2025 SocialApp
    </footer>
  </div>
</template>
