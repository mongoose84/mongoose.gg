<template>
  <div class="heatmap-card">
    <header class="header">
      <h2 class="title">Match Activity</h2>
      <p class="subtitle">Your gaming activity over the past 6 months</p>
    </header>

    <div v-if="hasData" class="heatmap-container">
      <!-- Month labels -->
      <div class="month-labels">
        <span v-for="month in monthLabels" :key="month.key" class="month-label" :style="{ left: month.offset + 'px' }">
          {{ month.name }}
        </span>
      </div>
      
      <!-- Day labels (Mon, Wed, Fri) -->
      <div class="day-labels">
        <span class="day-label"></span>
        <span class="day-label">Mon</span>
        <span class="day-label"></span>
        <span class="day-label">Wed</span>
        <span class="day-label"></span>
        <span class="day-label">Fri</span>
        <span class="day-label"></span>
      </div>
      
      <!-- Heatmap grid -->
      <div class="heatmap-grid">
        <div v-for="week in weeks" :key="week.weekIndex" class="heatmap-week">
          <div
            v-for="day in week.days"
            :key="day.date"
            class="heatmap-cell"
            :class="getCellClass(day.count)"
            :title="getTooltip(day)"
            @mouseenter="showTooltip(day, $event)"
            @mouseleave="hideTooltip"
          ></div>
        </div>
      </div>
      
      <!-- Legend -->
      <div class="legend">
        <span class="legend-label">Less</span>
        <div class="legend-cells">
          <div class="legend-cell level-0" title="0 matches"></div>
          <div class="legend-cell level-1" title="1-2 matches"></div>
          <div class="legend-cell level-2" title="3-5 matches"></div>
          <div class="legend-cell level-3" title="6+ matches"></div>
        </div>
        <span class="legend-label">More</span>
      </div>
      
      <!-- Summary -->
      <div class="summary">
        <span class="summary-text">{{ totalMatches }} matches in the last 6 months</span>
      </div>
    </div>
    
    <div v-else class="empty-state">
      <p>No match activity data available yet. Play some games to see your activity!</p>
    </div>
    
    <!-- Custom tooltip -->
    <Teleport to="body">
      <div 
        v-if="tooltipVisible" 
        class="heatmap-tooltip"
        :style="{ top: tooltipY + 'px', left: tooltipX + 'px' }"
      >
        <div class="tooltip-date">{{ tooltipData?.formattedDate }}</div>
        <div class="tooltip-count">{{ tooltipData?.count }} {{ tooltipData?.count === 1 ? 'match' : 'matches' }}</div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'

const props = defineProps({
  dailyMatchCounts: {
    type: Object,
    default: () => ({})
  },
  startDate: {
    type: String,
    default: ''
  },
  endDate: {
    type: String,
    default: ''
  },
  totalMatches: {
    type: Number,
    default: 0
  }
})

// Tooltip state
const tooltipVisible = ref(false)
const tooltipX = ref(0)
const tooltipY = ref(0)
const tooltipData = ref(null)

// UTC date helpers to avoid timezone issues
function parseUTCDate(dateStr) {
  // Parse 'YYYY-MM-DD' as UTC midnight
  const [year, month, day] = dateStr.split('-').map(Number)
  return new Date(Date.UTC(year, month - 1, day))
}

function formatUTCDate(date) {
  // Format as 'YYYY-MM-DD' from UTC components
  const year = date.getUTCFullYear()
  const month = String(date.getUTCMonth() + 1).padStart(2, '0')
  const day = String(date.getUTCDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function formatDisplayDate(date) {
  // Format for user display using UTC components
  const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
  const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']
  return `${days[date.getUTCDay()]}, ${months[date.getUTCMonth()]} ${date.getUTCDate()}, ${date.getUTCFullYear()}`
}

const hasData = computed(() => {
  return props.startDate && props.endDate
})

// Generate weeks array for the heatmap grid (GitHub-style: columns are weeks, rows are days)
const weeks = computed(() => {
  if (!props.startDate || !props.endDate) return []

  const result = []
  const start = parseUTCDate(props.startDate)
  const end = parseUTCDate(props.endDate)

  // Align to start of week (Sunday) using UTC
  const current = new Date(start.getTime())
  current.setUTCDate(current.getUTCDate() - current.getUTCDay())

  let weekIndex = 0
  while (current <= end) {
    const weekDays = []
    for (let dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++) {
      const dateStr = formatUTCDate(current)
      const count = props.dailyMatchCounts[dateStr] || 0
      const isInRange = current >= start && current <= end

      weekDays.push({
        date: dateStr,
        count: isInRange ? count : -1, // -1 means outside range
        dayOfWeek
      })
      current.setUTCDate(current.getUTCDate() + 1)
    }
    result.push({ weekIndex: weekIndex++, days: weekDays })
  }

  return result
})

// Generate month labels with position offsets
const monthLabels = computed(() => {
  if (!weeks.value.length) return []

  const labels = []
  const monthsSeen = new Set()
  const cellSize = 12 // cell width + gap
  const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

  weeks.value.forEach((week, weekIdx) => {
    // Check the first day of each week
    const firstValidDay = week.days.find(d => d.count >= 0)
    if (!firstValidDay) return

    const date = parseUTCDate(firstValidDay.date)
    const monthKey = `${date.getUTCFullYear()}-${date.getUTCMonth()}`

    if (!monthsSeen.has(monthKey)) {
      monthsSeen.add(monthKey)
      labels.push({
        key: monthKey,
        name: months[date.getUTCMonth()],
        offset: weekIdx * cellSize + 24 // 24px for day labels
      })
    }
  })

  return labels
})

function getCellClass(count) {
  if (count < 0) return 'level-hidden'
  if (count === 0) return 'level-0'
  if (count <= 2) return 'level-1'
  if (count <= 5) return 'level-2'
  return 'level-3'
}

function getTooltip(day) {
  if (day.count < 0) return ''
  const date = parseUTCDate(day.date)
  const formatted = formatDisplayDate(date)
  return `${day.count} ${day.count === 1 ? 'match' : 'matches'} on ${formatted}`
}

function showTooltip(day, event) {
  if (day.count < 0) return

  const rect = event.target.getBoundingClientRect()
  tooltipX.value = rect.left + rect.width / 2
  tooltipY.value = rect.top - 8

  const date = parseUTCDate(day.date)
  tooltipData.value = {
    date: day.date,
    count: day.count,
    formattedDate: formatDisplayDate(date)
  }
  tooltipVisible.value = true
}

function hideTooltip() {
  tooltipVisible.value = false
}
</script>

<style scoped>
.heatmap-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
}

.header {
  margin-bottom: var(--spacing-lg);
}

.title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin-bottom: var(--spacing-xs);
}

.subtitle {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.heatmap-container {
  position: relative;
}

.month-labels {
  position: relative;
  height: 20px;
  margin-bottom: var(--spacing-xs);
  margin-left: 24px;
}

.month-label {
  position: absolute;
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.day-labels {
  position: absolute;
  left: 0;
  top: 28px;
  display: flex;
  flex-direction: column;
  gap: 2px;
  width: 20px;
}

.day-label {
  font-size: 9px;
  color: var(--color-text-secondary);
  height: 10px;
  line-height: 10px;
}

.heatmap-grid {
  display: flex;
  gap: 2px;
  margin-left: 24px;
  overflow-x: auto;
  padding-bottom: var(--spacing-sm);
}

.heatmap-week {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.heatmap-cell {
  width: 10px;
  height: 10px;
  border-radius: 2px;
  cursor: pointer;
  transition: transform 0.1s ease;
}

.heatmap-cell:hover {
  transform: scale(1.2);
}

.level-hidden {
  background: transparent;
  cursor: default;
}

.level-hidden:hover {
  transform: none;
}

.level-0 {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.1);
}

.level-1 {
  background: rgba(109, 40, 217, 0.3);
}

.level-2 {
  background: rgba(109, 40, 217, 0.6);
}

.level-3 {
  background: #6d28d9;
}

.legend {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  margin-top: var(--spacing-md);
  justify-content: flex-end;
}

.legend-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.legend-cells {
  display: flex;
  gap: 2px;
}

.legend-cell {
  width: 10px;
  height: 10px;
  border-radius: 2px;
}

.summary {
  margin-top: var(--spacing-md);
  text-align: center;
}

.summary-text {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.empty-state {
  text-align: center;
  padding: var(--spacing-xl);
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}
</style>

<style>
/* Global tooltip styles (not scoped) */
.heatmap-tooltip {
  position: fixed;
  transform: translateX(-50%) translateY(-100%);
  background: var(--color-elevated, rgba(255, 255, 255, 0.1));
  border: 1px solid var(--color-border, rgba(109, 40, 217, 0.15));
  border-radius: var(--radius-md, 0.5rem);
  padding: 6px 10px;
  font-size: 12px;
  color: var(--color-text, #ffffff);
  z-index: 9999;
  pointer-events: none;
  white-space: nowrap;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  backdrop-filter: blur(8px);
}

.tooltip-date {
  font-weight: 500;
  margin-bottom: 2px;
}

.tooltip-count {
  color: var(--color-primary, #6d28d9);
  font-weight: 600;
}
</style>

