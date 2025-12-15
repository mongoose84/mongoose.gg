<template>
  <section class="home">
    <!-- Hero Header -->
    <header class="hero" aria-labelledby="app-title">
      <div class="brand">
        <!-- Simple stats/crest logo -->
        <span class="logo" aria-hidden="true">
          <svg viewBox="0 0 64 64" fill="none" xmlns="http://www.w3.org/2000/svg">
            <defs>
              <linearGradient id="grad" x1="0" y1="0" x2="64" y2="64" gradientUnits="userSpaceOnUse">
                <stop offset="0"/>
                <stop offset="1" stop-opacity="0.85"/>
              </linearGradient>
            </defs>
            <!-- Crest outline -->
            <path d="M32 6c8.8 0 20 3.3 20 12.5V34c0 8.7-7.9 15.9-20 24-12.1-8.1-20-15.3-20-24V18.5C12 9.3 23.2 6 32 6Z" stroke="currentColor" stroke-width="2.5" fill="url(#grad)"/>
            <!-- Bars for statistics -->
            <rect x="20" y="28" width="6" height="18" rx="1.5" fill="currentColor"/>
            <rect x="29" y="22" width="6" height="24" rx="1.5" fill="currentColor" opacity="0.85"/>
            <rect x="38" y="32" width="6" height="14" rx="1.5" fill="currentColor" opacity="0.7"/>
            <!-- Subtle divider -->
            <path d="M18 18h28" stroke="currentColor" stroke-opacity="0.4" stroke-width="2"/>
          </svg>
        </span>
        <div class="titles">
          <h1 id="app-title" class="title">Do End</h1>
          <p class="subtitle">Cross Account LoL Statistics</p>
        </div>
      </div>
    </header>

    <!-- Users Section (always visible) -->
    <div class="users-list">
      <div class="users-header">
        <h3>Users ({{ users ? users.length : 0 }})</h3>
        <button @click="showUserForm = !showUserForm" class="toggle-user-btn">
          {{ showUserForm ? 'Cancel' : 'Create User' }}
        </button>
      </div>

      <CreateUserPopup
        v-if="showUserForm"
        :onClose="() => (showUserForm = false)"
        :onCreate="handleCreateUser"
      />

      <template v-if="users && users.length">
        <ul>
          <li
            v-for="u in users"
            :key="u.userId || u.UserId"
            class="user-item"
            @click="goToUserView(u)"
          >
            {{ u.userName || u.UserName }}
          </li>
        </ul>
      </template>
      <div v-else class="empty-state">
        <p>No users yet. Create the first one to get started.</p>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import CreateUserPopup from './CreateUserPopup.vue' // Import the CreateUserPopup component
import createUser  from '@/assets/createUser.js'
import getUsers from '@/assets/getUsers.js'
// ----- Options for the dropdown ---------------------------------------
const options = [
  { value: 'NA', label: 'NA' },
  { value: 'EUW', label: 'EUW' },
  { value: 'EUNE', label: 'EUNE' },
  { value: 'KR', label: 'KR' },
  { value: 'JP', label: 'JP' },
  { value: 'LAN', label: 'LAN' },
  { value: 'LAS', label: 'LAS' },
  { value: 'OCE', label: 'OCE' },
  { value: 'RU', label: 'RU' },
  { value: 'TR', label: 'TR' },
]

// ----- Reactive state -------------------------------------------------
const query = ref('')
const tagLine = ref('EUNE')
const router = useRouter()

// User creation state
const showUserForm = ref(false)
const users = ref([])

// ----- Methods --------------------------------------------------------
function goToUserView(user) {
  const id = user?.userId ?? user?.UserId;
  const name = (user?.userName ?? user?.UserName ?? '').trim();
  if (!id || !name) return;

  const queryParams = { userId: id, userName: name };
  router.push({ name: 'UserView', query: queryParams });
}

async function handleCreateUser(payload) {
  // Expect payload: { username, accounts: [{ gameName, tagLine }] }
  try {
    const username = (payload?.username || '').trim();
    const accounts = Array.isArray(payload?.accounts) ? payload.accounts : [];
    if (!username || !accounts.length) return;

    await createUser(username, accounts);
    console.log(`User "${username}" created successfully!`);
    await loadUsers();
  } catch (err) {
    console.error(`Failed to create user "${payload?.username}":`, err?.message || err);
  }
}

async function loadUsers() {
  try {
    users.value = await getUsers();
    console.log(`Loaded ${users.value.length} users`);
  } catch (e) {
    console.error('Failed to fetch users:', e?.message || e);
  }
}

onMounted(() => {
  loadUsers();
})
</script>

<style scoped>

.home {
  display: flex;
  flex-direction: column;
  align-items: center;
  min-height: 100vh;
  padding: 2rem 1rem; /* Provide breathing room from the top */
  background-color: var(--color-bg);
  color: var(--color-text);
}

.hero {
  width: 100%;
  max-width: 900px;
  margin: 0 auto 1.25rem;
}

.brand {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  width: 100%;
}

.logo {
  display: inline-flex;
  width: 64px;
  height: 64px;
  color: var(--color-primary);
}

.logo svg {
  width: 100%;
  height: 100%;
}

.titles {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  text-align: left;
}

.title {
  margin: 0;
  font-size: clamp(2rem, 4vw, 3rem);
  line-height: 1.1;
  letter-spacing: 0.02em;
}

.subtitle {
  margin: 0.125rem 0 0;
  font-size: clamp(0.95rem, 1.6vw, 1.1rem);
  opacity: 0.85;
}

.home-form {  
  display: flex;
  gap: 0.1rem;
  margin-top: 1rem;
  align-items: center;
  justify-content: center;
}

.tagLine-select {
  padding: 0.5rem;
  font-size: 1rem;
  width: 90px;
}

.home-input {
  flex: 1;
  min-width: 0;
  width: 300px;
  padding: 0.5rem;
  font-size: 1rem;
}

.user-creation {
  margin-top: 2rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.toggle-user-btn,
.create-user-btn {
  padding: 0.5rem 1rem;
  font-size: 1rem;
  cursor: pointer;
  background-color: var(--color-primary);
  color: var(--color-text);
  border: none;
  border-radius: 6px;
}

.toggle-user-btn:hover,
.create-user-btn:hover {
  background-color: var(--color-primary-hover);
}

.user-form {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  align-items: center;
}

.error {
  color: #dc3545;
  font-size: 0.9rem;
}

.success {
  color: #28a745;
  font-size: 0.9rem;
}

.users-list {
  margin: 2rem auto 0; /* center horizontally and add top spacing */
  width: 100%;
  max-width: 600px;
}

.users-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 0.75rem;
}

.users-list ul {
  list-style: none;
  margin: 0;
  padding: 0;
}

.empty-state {
  margin-top: 0.5rem;
  padding: 1rem;
  border: 1px dashed var(--color-border);
  border-radius: 6px;
  background-color: var(--color-bg-elev);
  color: var(--color-text);
  opacity: 0.9;
}

.user-item {
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  margin-bottom: 0.5rem;
  cursor: pointer;
  transition: background-color 0.15s ease, transform 0.05s ease;
  background-color: var(--color-bg-elev);
  color: var(--color-text);
}

.user-item:hover {
  background-color: var(--color-bg-hover);
}

.user-item:active {
  transform: scale(0.99);
}
</style>