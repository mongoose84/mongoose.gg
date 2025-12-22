<template>
  <div class="popup-overlay">
    <div class="popup-content">
      <h2>Create new dashboard</h2>

      <!-- Dashboard name -->
      <div class="dashboard-input-container">
        <input
          v-model="dashboardName"
          type="text"
          placeholder="Enter dashboard name…"
          required
          class="dashboard-input"
        />
      </div>

      <!-- Dashboard Type selector -->
      <div class="dashboardtype-container">
        <label class="dashboardtype-label">Dashboard Type</label>
        <div class="dashboardtype-buttons">
          <button
            v-for="ut in dashboardTypes"
            :key="ut.value"
            type="button"
            class="dashboardtype-btn"
            :class="{ active: dashboardType === ut.value }"
            @click="dashboardType = ut.value"
          >
            {{ ut.label }}
          </button>
        </div>
      </div>

      <!-- Multiple gameName/tagLine pairs -->
      <div class="username-fields">
        <div v-for="row in summoners" :key="row.id" class="username-row">
          <section class="search">
            <form class="search-form" @submit.prevent>
              <input
                v-model="row.gameName"
                type="text"
                placeholder="Search for your champion name…"
                required
                class="search-input"
              />
              <h1>#</h1>
              
              <!-- Custom combo box -->
              <div class="combo-box">
                <input 
                  v-model="row.tagLine" 
                  type="text"
                  placeholder="Tag"
                  class="tagLine-input"
                  @focus="row.showDropdown = true"
                  @blur="handleBlur(row)"
                />
                <button 
                  type="button"
                  class="dropdown-arrow"
                  @click="row.showDropdown = !row.showDropdown"
                >
                  ▼
                </button>
                <div v-if="row.showDropdown" class="dropdown-menu">
                  <div 
                    v-for="opt in options" 
                    :key="opt.value"
                    class="dropdown-item"
                    @click="selectOption(row, opt.value)"
                  >
                    {{ opt.label }}
                  </div>
                </div>
              </div>
            </form>
          </section>
        </div>
      </div>

      <button class="add-button" @click="handleAddRow">+</button>

      <div class="action-buttons">
        <button @click="handleCreate" :disabled="busy">Create</button>
        <button @click="onClose" :disabled="busy">Cancel</button>
      </div>

      <p v-if="error" class="error">{{ error }}</p>
      <p v-if="success" class="success">{{ success }}</p>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref } from 'vue'

interface SummonerField {
  id: number;
  gameName: string;
  tagLine: string;
  showDropdown?: boolean;
}

export default defineComponent({
  props: {
    onClose: { type: Function, required: true },
    onCreate: { type: Function, required: true }
  },
  setup(props) {
    const options = [
      { value: 'NA', label: 'NA' },  { value: 'EUW', label: 'EUW' },
      { value: 'EUNE', label: 'EUNE' }, { value: 'KR', label: 'KR' },
      { value: 'JP', label: 'JP' },  { value: 'LAN', label: 'LAN' },
      { value: 'LAS', label: 'LAS' },{ value: 'OCE', label: 'OCE' },
      { value: 'RU', label: 'RU' },  { value: 'TR', label: 'TR' },
    ]

    const dashboardTypes = [
      { value: 'Solo', label: 'Solo' },
      { value: 'Duo', label: 'Duo' },
      { value: 'Team', label: 'Team' },
    ]

    const dashboardName = ref<string>('')
    const dashboardType = ref<string>('Solo')
    const summoners = ref<SummonerField[]>([
      { id: 1, gameName: '', tagLine: 'EUNE', showDropdown: false }
    ])
    const nextId = ref(2)
    const busy = ref(false)
    const error = ref<string | null>(null)
    const success = ref<string | null>(null)

    const handleAddRow = () => {
      summoners.value.push({ id: nextId.value, gameName: '', tagLine: 'EUNE', showDropdown: false })
      nextId.value++
    }

        const handleBlur = (row: SummonerField) => {
      setTimeout(() => {
        row.showDropdown = false
      }, 200)
    }

    const selectOption = (row: SummonerField, value: string) => {
      row.tagLine = value
      row.showDropdown = false
    }

    const handleCreate = async () => {
      if (busy.value) return
      error.value = null
      success.value = null
      busy.value = true

      try {
        const name = dashboardName.value.trim()
        const accounts = summoners.value
          .map(s => ({ gameName: s.gameName.trim(), tagLine: s.tagLine.trim() }))
          .filter(s => s.gameName && s.tagLine)

        if (!name) {
          error.value = 'Please enter a dashboard name.'
          return
        }

        if (!accounts.length) {
          error.value = 'Please enter at least one valid game name and tag.'
          return
        }

        if (typeof props.onCreate === 'function') {
          await Promise.resolve(props.onCreate({ username: name, userType: dashboardType.value, accounts }))
        }

        success.value = 'Accounts created successfully.'
        // Close popup after success
        if (typeof props.onClose === 'function') props.onClose()
      } catch (e: any) {
        error.value = e?.message || 'Failed to create user(s).'
      } finally {
        busy.value = false
      }
    }

    return { options, dashboardTypes, dashboardName, dashboardType, summoners, handleAddRow, handleBlur, selectOption, handleCreate, busy, error, success }
  }
})
</script>

<style scoped>
.popup-overlay {
  position: fixed; inset: 0;
  background-color: rgba(43, 11, 58, 0.55); /* theme-tinted, more see-through */
  display: flex; justify-content: center; align-items: center; z-index: 1000;
}
.popup-content {
  background: var(--color-bg-elev); color: var(--color-text); padding: 2rem; border-radius: 8px;
  max-width: 560px; width: 92%;
}
.popup-content h2 { margin-bottom: 1rem; }
.dashboard-input-container { margin-bottom: 1rem; }
.dashboard-input {
  width: 100%; padding: 0.5rem; font-size: 1rem;
  border: 1px solid var(--color-border); border-radius: 4px; background: var(--color-bg); color: var(--color-text);
}
.dashboard-input::placeholder { color: var(--color-text-muted); }

.dashboardtype-container {
  margin-bottom: 1rem;
}

.dashboardtype-label {
  display: block;
  margin-bottom: 0.5rem;
  font-size: 0.9rem;
  opacity: 0.85;
}

.dashboardtype-buttons {
  display: flex;
  gap: 0.5rem;
}

.dashboardtype-btn {
  flex: 1;
  padding: 0.5rem 1rem;
  font-size: 0.9rem;
  cursor: pointer;
  border: 1px solid var(--color-border);
  border-radius: 4px;
  background: var(--color-bg);
  color: var(--color-text);
  transition: background 0.15s ease, border-color 0.15s ease;
}

.dashboardtype-btn:hover {
  background: var(--color-bg-hover);
}

.dashboardtype-btn.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
}

.username-fields { margin: 1rem 0; }
.username-row { margin-bottom: 1rem; padding: 1rem; border: 1px solid var(--color-border); border-radius: 4px; background: var(--color-bg-elev); }

.search-form { display: flex; gap: 0.25rem; align-items: center; }
.search-form h1 { color: var(--color-text); margin: 0 0.25rem; }

.search-input {
  background: var(--color-bg); color: var(--color-text); border: 1px solid var(--color-border); border-radius: 4px;
  padding: 0.5rem; font-size: 1rem;
  flex: 1; min-width: 0;
}

.combo-box {
  position: relative;
  display: flex;
  width: 120px;
}

.tagLine-input {
  background: var(--color-bg); color: var(--color-text); border: 1px solid var(--color-border); 
  border-right: none;
  border-radius: 4px 0 0 4px;
  padding: 0.5rem; font-size: 1rem;
  flex: 1;
  min-width: 0;
}

.dropdown-arrow {
  background: var(--color-bg);
  color: var(--color-text);
  border: 1px solid var(--color-border);
  border-radius: 0 4px 4px 0;
  padding: 0 0.5rem;
  cursor: pointer;
  font-size: 0.7rem;
}

.dropdown-arrow:hover {
  background: var(--color-bg-hover);
}

.dropdown-menu {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  background: var(--color-bg-elev);
  border: 1px solid var(--color-border);
  border-top: none;
  border-radius: 0 0 4px 4px;
  max-height: 200px;
  overflow-y: auto;
  z-index: 1000;
  margin-top: 1px;
}

.dropdown-item {
  padding: 0.5rem;
  cursor: pointer;
  color: var(--color-text);
}

.dropdown-item:hover {
  background: var(--color-bg-hover);
}

.add-button {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: var(--color-primary);
  color: var(--color-text);
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  margin: 0.5rem 0 1rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  line-height: 1;
  padding: 0;
  box-sizing: border-box;
}
.add-button:hover { background: var(--color-primary-hover); }

.action-buttons { display: flex; gap: 1rem; justify-content: center; }
.action-buttons button {
  padding: 0.5rem 1rem; font-size: 1rem; cursor: pointer; border: none; border-radius: 4px;
}
.action-buttons button:first-child { background: var(--color-primary); color: var(--color-text); }
.action-buttons button:first-child:hover { background: var(--color-primary-hover); }
.action-buttons button:last-child { background: var(--color-bg-elev); color: var(--color-text); border: 1px solid var(--color-border); }
.action-buttons button:last-child:hover { background: var(--color-bg-hover); }

.error { color: #ff6b6b; margin-top: 0.5rem; }
.success { color: #28a745; margin-top: 0.5rem; }
</style>