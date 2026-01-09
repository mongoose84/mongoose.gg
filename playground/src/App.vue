<template>
  <div class="playground-wrapper" :data-theme="currentTheme">
    <div class="pg-bg-layer"></div>
    <div class="pg-container">
      <!-- Theme Switcher -->
      <div class="pg-header">
        <h1 class="pg-main-title">Design System Playground</h1>
        <p class="pg-subtitle">Professional themes inspired by industry leaders</p>
        
        <div class="pg-theme-grid">
          <button 
            v-for="theme in themes" 
            :key="theme.id"
            @click="currentTheme = theme.id"
            :class="['pg-theme-card', { 'active': currentTheme === theme.id }]"
          >
            <div class="pg-theme-icon">{{ theme.name.split(' ')[0] }}</div>
            <div class="pg-theme-name">{{ theme.name }}</div>
            <div class="pg-theme-desc">{{ theme.desc }}</div>
          </button>
        </div>
      </div>

      <!-- Typography Showcase -->
      <section class="pg-section">
        <h2 class="pg-section-title">Typography & Weight</h2>
        <div class="pg-card">
          <h1 class="pg-display">Display Heading (H1)</h1>
          <h2 class="pg-title">Title Heading (H2)</h2>
          <h3 class="pg-heading">Section Heading (H3)</h3>
          <p class="pg-body">Body text with normal weight. This is how regular paragraphs will look across the application.</p>
          <p class="pg-body-emphasis">Emphasized body text for important information.</p>
        </div>
      </section>

      <!-- Spacing & Layout -->
      <section class="pg-section">
        <h2 class="pg-section-title">Spacing & Layout</h2>
        <div class="pg-card">
          <div class="pg-stack">
            <div class="pg-box">Extra Small Spacing</div>
            <div class="pg-box">Small Spacing</div>
            <div class="pg-box">Medium Spacing</div>
            <div class="pg-box">Large Spacing</div>
          </div>
        </div>
      </section>

      <!-- Buttons & Interactions -->
      <section class="pg-section">
        <h2 class="pg-section-title">Buttons & Borders</h2>
        <div class="pg-card">
          <div class="pg-button-group">
            <button class="pg-btn pg-btn-primary">Primary Action</button>
            <button class="pg-btn pg-btn-secondary">Secondary Action</button>
            <button class="pg-btn pg-btn-ghost">Ghost Button</button>
          </div>
        </div>
      </section>

      <!-- Cards & Shadows -->
      <section class="pg-section">
        <h2 class="pg-section-title">Cards & Shadows</h2>
        <div class="pg-cards-grid">
          <div class="pg-sample-card pg-elevation-sm">
            <h3 class="pg-card-title">Small Shadow</h3>
            <p>Subtle depth effect</p>
          </div>
          <div class="pg-sample-card pg-elevation-md">
            <h3 class="pg-card-title">Medium Shadow</h3>
            <p>Standard card effect</p>
          </div>
          <div class="pg-sample-card pg-elevation-lg">
            <h3 class="pg-card-title">Large Shadow</h3>
            <p>Prominent depth</p>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';

const themes = [
  { id: 'riot', name: '🎮 Riot Games', desc: 'Epic gaming with hero imagery' },
  { id: 'apple', name: '🍎 Apple Minimal', desc: 'Premium refined elegance' },
  { id: 'stripe', name: '💳 Stripe SaaS', desc: 'Modern with glassmorphism' },
  { id: 'behance', name: '🎨 Behance Creative', desc: 'Bold artistic portfolio' },
  { id: 'tesla', name: '🚗 Tesla Premium', desc: 'Luxurious high-tech' },
  { id: 'discord', name: '💬 Discord Community', desc: 'Friendly social gaming' },
  { id: 'notion', name: '📝 Notion Productivity', desc: 'Warm organized calm' },
  { id: 'vercel', name: '▲ Vercel Developer', desc: 'Technical cutting-edge' },
  { id: 'github', name: '⚡ GitHub Clean', desc: 'Clear documentation focus' },
  { id: 'linear', name: '🎯 Linear Modern', desc: 'Smooth productivity flow' },
  { id: 'nvidia', name: '🟢 Nvidia Tech', desc: 'AI-powered gaming future' },
  { id: 'pharma', name: '💜 Pharma Pro', desc: 'Corporate trust precision' }
];

const currentTheme = ref('riot');
</script>

<style scoped>
/* Playground wrapper with background image support */
.playground-wrapper {
  min-height: 100vh;
  position: relative;
  background: var(--color-bg);
}

.pg-bg-layer {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-image: var(--bg-image);
  background-size: cover;
  background-position: center;
  background-attachment: fixed;
  z-index: 0;
}

.pg-bg-layer::after {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: var(--bg-overlay);
}

/* Container */
.pg-container {
  position: relative;
  z-index: 1;
  padding: var(--spacing-xl);
  max-width: 1200px;
  margin: 0 auto;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}

/* Header */
.pg-header {
  text-align: center;
  margin-bottom: var(--spacing-2xl);
}

.pg-main-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-sm);
  color: var(--color-text);
}

.pg-subtitle {
  font-size: var(--font-size-md);
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-xl);
}

/* Theme Grid */
.pg-theme-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: var(--spacing-md);
  max-width: 1000px;
  margin: 0 auto;
}

.pg-theme-card {
  padding: var(--spacing-md);
  border-radius: var(--radius-lg);
  border: 2px solid var(--color-border);
  background: var(--color-surface);
  backdrop-filter: blur(10px);
  color: var(--color-text);
  cursor: pointer;
  transition: all 0.3s;
  text-align: center;
}

.pg-theme-card:hover {
  border-color: var(--color-primary);
  background: var(--color-elevated);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.pg-theme-card.active {
  background: var(--color-primary);
  color: white;
  border-color: var(--color-primary);
  box-shadow: var(--shadow-lg);
  transform: translateY(-4px);
}

.pg-theme-icon {
  font-size: 2rem;
  margin-bottom: var(--spacing-xs);
}

.pg-theme-name {
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-sm);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-xs);
}

.pg-theme-desc {
  font-size: var(--font-size-xs);
  opacity: 0.8;
}

/* Sections */
.pg-section {
  margin-bottom: var(--spacing-2xl);
}

.pg-section-title {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-md);
  color: var(--color-text);
}

/* Cards */
.pg-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  box-shadow: var(--shadow-sm);
  backdrop-filter: blur(10px);
}

/* Typography Styles */
.pg-display {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-md);
  color: var(--color-text);
}

.pg-title {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-sm);
  color: var(--color-text);
}

.pg-heading {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-sm);
  color: var(--color-text);
}

.pg-body {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-normal);
  line-height: 1.6;
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-sm);
}

.pg-body-emphasis {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-semibold);
  line-height: 1.6;
  color: var(--color-text);
}

/* Spacing Stack */
.pg-stack {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.pg-box {
  background: var(--color-primary-soft);
  border: 2px solid var(--color-primary);
  border-radius: var(--radius-md);
  padding: var(--spacing-md);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
}

/* Buttons */
.pg-button-group {
  display: flex;
  gap: var(--spacing-md);
  flex-wrap: wrap;
}

.pg-btn {
  padding: var(--spacing-sm) var(--spacing-lg);
  border-radius: var(--radius-md);
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-md);
  letter-spacing: var(--letter-spacing);
  cursor: pointer;
  transition: all 0.2s;
  border: 2px solid transparent;
}

.pg-btn-primary {
  background: var(--color-primary);
  color: white;
  border-color: var(--color-primary);
  box-shadow: var(--shadow-md);
}

.pg-btn-primary:hover {
  box-shadow: var(--shadow-lg);
  transform: translateY(-1px);
}

.pg-btn-secondary {
  background: var(--color-surface);
  color: var(--color-text);
  border-color: var(--color-border);
  backdrop-filter: blur(10px);
}

.pg-btn-secondary:hover {
  border-color: var(--color-primary);
  background: var(--color-primary-soft);
}

.pg-btn-ghost {
  background: transparent;
  color: var(--color-primary);
  border-color: transparent;
}

.pg-btn-ghost:hover {
  background: var(--color-primary-soft);
}

/* Sample Cards Grid */
.pg-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: var(--spacing-lg);
}

.pg-sample-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  transition: all 0.3s;
  backdrop-filter: blur(10px);
}

.pg-sample-card:hover {
  transform: translateY(-2px);
}

.pg-elevation-sm {
  box-shadow: var(--shadow-sm);
}

.pg-elevation-md {
  box-shadow: var(--shadow-md);
}

.pg-elevation-lg {
  box-shadow: var(--shadow-lg);
}

.pg-card-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-xs);
  color: var(--color-text);
}

.pg-sample-card p {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}
</style>
