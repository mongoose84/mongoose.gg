import { ref, computed, onMounted, watch } from 'vue';
import getGamers from '@/assets/getGamers.js';

/**
 * Composable for loading and managing gamers data.
 * @param {() => { userName: string, userId: string | number }} getProps - Function returning reactive props
 */
export function useGamers(getProps) {
  const loading = ref(false);
  const error = ref(null);
  const gamers = ref([]);

  const hasUser = computed(() => {
    const { userName, userId } = getProps();
    return !!userName && userId !== undefined && userId !== '';
  });

  async function load() {
    if (!hasUser.value) return;
    loading.value = true;
    error.value = null;
    try {
      const { userId } = getProps();
      const list = await getGamers(userId);
      gamers.value = Array.isArray(list) ? list : [];
    } catch (e) {
      error.value = e?.message || 'Failed to load gamers.';
      gamers.value = [];
    } finally {
      loading.value = false;
    }
  }

  onMounted(() => {
    load();
  });

  watch(
    () => {
      const { userName, userId } = getProps();
      return [userName, userId];
    },
    () => {
      load();
    }
  );

  return {
    loading,
    error,
    gamers,
    hasUser,
    load,
  };
}
