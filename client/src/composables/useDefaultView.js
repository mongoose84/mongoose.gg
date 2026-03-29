import { ref } from 'vue'

const DEFAULT_VIEW_KEY = 'mongoose_default_view'

// Module-level ref so all consumers share the same reactive state
const defaultView = ref(localStorage.getItem(DEFAULT_VIEW_KEY) || 'overall')

export function useDefaultView() {
  function setDefaultView(value) {
    defaultView.value = value
    localStorage.setItem(DEFAULT_VIEW_KEY, value)
  }

  return { defaultView, setDefaultView }
}