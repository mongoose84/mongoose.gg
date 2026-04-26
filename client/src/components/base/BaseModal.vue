<template>
  <TransitionRoot appear :show="isOpen" as="template">
    <Dialog as="div" class="modal-root" @close="handleClose">
      <!-- Backdrop -->
      <TransitionChild
        as="template"
        enter="duration-200 ease-out"
        enter-from="opacity-0"
        enter-to="opacity-100"
        leave="duration-150 ease-in"
        leave-from="opacity-100"
        leave-to="opacity-0"
      >
        <div class="modal-backdrop" aria-hidden="true" />
      </TransitionChild>

      <!-- Modal container -->
      <div class="modal-container">
        <TransitionChild
          as="template"
          enter="duration-200 ease-out"
          enter-from="opacity-0 scale-95"
          enter-to="opacity-100 scale-100"
          leave="duration-150 ease-in"
          leave-from="opacity-100 scale-100"
          leave-to="opacity-0 scale-95"
        >
          <DialogPanel class="modal-panel" :class="sizeClass">
            <!-- Header -->
            <div v-if="title || $slots.header || showCloseButton" class="modal-header">
              <slot name="header">
                <DialogTitle as="h2" class="modal-title">
                  {{ title }}
                </DialogTitle>
              </slot>
              
              <button
                v-if="showCloseButton"
                type="button"
                class="modal-close-btn"
                :disabled="preventClose"
                @click="handleClose"
                aria-label="Close modal"
              >
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="w-5 h-5">
                  <path fill-rule="evenodd" d="M5.47 5.47a.75.75 0 011.06 0L12 10.94l5.47-5.47a.75.75 0 111.06 1.06L13.06 12l5.47 5.47a.75.75 0 11-1.06 1.06L12 13.06l-5.47 5.47a.75.75 0 01-1.06-1.06L10.94 12 5.47 6.53a.75.75 0 010-1.06z" clip-rule="evenodd" />
                </svg>
              </button>
            </div>

            <!-- Body -->
            <div class="modal-body">
              <slot></slot>
            </div>

            <!-- Footer -->
            <div v-if="$slots.footer" class="modal-footer">
              <slot name="footer"></slot>
            </div>
          </DialogPanel>
        </TransitionChild>
      </div>
    </Dialog>
  </TransitionRoot>
</template>

<script setup>
import { computed } from 'vue'
import {
  Dialog,
  DialogPanel,
  DialogTitle,
  TransitionRoot,
  TransitionChild
} from '@headlessui/vue'

const props = defineProps({
  /** Whether the modal is open */
  isOpen: {
    type: Boolean,
    required: true
  },
  /** Modal title */
  title: {
    type: String,
    default: null
  },
  /** Modal size: sm, md, lg, xl, full */
  size: {
    type: String,
    default: 'md',
    validator: (v) => ['sm', 'md', 'lg', 'xl', 'full'].includes(v)
  },
  /** Show close button in header */
  showCloseButton: {
    type: Boolean,
    default: true
  },
  /** Prevent closing (useful during async operations) */
  preventClose: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['close'])

const sizeClass = computed(() => `modal-panel--${props.size}`)

function handleClose() {
  if (!props.preventClose) {
    emit('close')
  }
}
</script>

<style scoped>
.modal-root {
  position: fixed;
  inset: 0;
  z-index: 300;
  overflow-y: auto;
}

.modal-backdrop {
  position: fixed;
  inset: 0;
  z-index: 300;
  background: rgba(0, 0, 0, 0.8);
}

.modal-container {
  position: fixed;
  inset: 0;
  z-index: 301;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-xl);
}

.modal-panel {
  width: 100%;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  backdrop-filter: blur(10px);
  overflow: hidden;
}

/* Sizes */
.modal-panel--sm { max-width: 320px; }
.modal-panel--md { max-width: 400px; }
.modal-panel--lg { max-width: 560px; }
.modal-panel--xl { max-width: 720px; }
.modal-panel--full { max-width: 100%; height: 100%; border-radius: 0; }

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
}

.modal-title {
  margin: 0;
  font-size: var(--font-size-lg);
  font-weight: 600;
  color: var(--color-text);
}

.modal-close-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-xs);
  background: transparent;
  border: none;
  border-radius: var(--radius-sm);
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: all 0.2s ease;
}

.modal-close-btn:hover:not(:disabled) {
  color: var(--color-text);
  background: var(--color-elevated);
}

.modal-close-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.modal-body {
  padding: var(--spacing-lg);
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-sm);
  padding: var(--spacing-lg);
  border-top: 1px solid var(--color-border);
}

/* Transition utilities (used by HeadlessUI) */
.duration-200 { transition-duration: 200ms; }
.duration-150 { transition-duration: 150ms; }
.ease-out { transition-timing-function: ease-out; }
.ease-in { transition-timing-function: ease-in; }
.opacity-0 { opacity: 0; }
.opacity-100 { opacity: 1; }
.scale-95 { transform: scale(0.95); }
.scale-100 { transform: scale(1); }
</style>

