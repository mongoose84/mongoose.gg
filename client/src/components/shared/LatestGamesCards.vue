<template>
  <div v-if="latestGames.length > 0" class="latest-games-section">
    <h3 class="section-title">Latest Games</h3>
    <div class="latest-games-grid">
      <div
        v-for="item in latestGames"
        :key="item.gamer.puuid || item.gamer.Puuid"
        class="latest-game-card"
        :class="{ win: item.game.win ?? item.game.Win }"
      >
        <img
          :src="getChampionImageUrl(item.game.championName || item.game.ChampionName)"
          :alt="item.game.championName || item.game.ChampionName"
          class="champion-icon"
        />
        <div class="game-details">
          <div class="game-result">
            <span class="result-badge" :class="{ win: item.game.win ?? item.game.Win }">
              {{ (item.game.win ?? item.game.Win) ? 'Victory' : 'Defeat' }}
            </span>
            <span class="game-time">{{ formatTimeAgo(item.date) }}</span>
          </div>
          <div class="game-info">
            <span class="champion-name">{{ item.game.championName || item.game.ChampionName }}</span>
            <span class="role">{{ formatRole(item.game.role || item.game.Role) }}</span>
            <span class="kda">
              {{ item.game.kills ?? item.game.Kills }}/{{ item.game.deaths ?? item.game.Deaths }}/{{ item.game.assists ?? item.game.Assists }}
            </span>
          </div>
          <div class="gamer-name">{{ item.gamer.gamerName || item.gamer.GamerName }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  gamers: {
    type: Array,
    required: true,
  },
});

// Extract latest games from all gamers
const latestGames = computed(() => {
  if (!props.gamers || props.gamers.length === 0) return [];

  return props.gamers
    .map(gamer => {
      const game = gamer.latestGame || gamer.LatestGame;
      if (!game) return null;

      const timestamp = game.gameEndTimestamp || game.GameEndTimestamp;
      if (!timestamp || timestamp === '0001-01-01T00:00:00') return null;

      return {
        gamer,
        game,
        date: new Date(timestamp),
      };
    })
    .filter(item => item !== null)
    .sort((a, b) => b.date - a.date); // Most recent first
});

// Format role for display (e.g., "TOP" -> "Top")
function formatRole(role) {
  if (!role || role === 'UNKNOWN') return 'Unknown';
  return role.charAt(0).toUpperCase() + role.slice(1).toLowerCase();
}

// Get champion image URL
function getChampionImageUrl(championName) {
  return `https://ddragon.leagueoflegends.com/cdn/14.24.1/img/champion/${championName}.png`;
}

// Format time ago
function formatTimeAgo(date) {
  if (!date) return '';

  const now = new Date();
  const diff = now - date;
  const days = Math.floor(diff / (1000 * 60 * 60 * 24));

  if (days === 0) {
    const hours = Math.floor(diff / (1000 * 60 * 60));
    if (hours === 0) {
      const minutes = Math.floor(diff / (1000 * 60));
      return minutes <= 1 ? 'just now' : `${minutes}m ago`;
    }
    return `${hours}h ago`;
  }
  if (days === 1) return 'yesterday';
  if (days < 7) return `${days}d ago`;

  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
  });
}
</script>

<style scoped>
.latest-games-section {
  margin-top: 1.5rem;
}

.section-title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-text-muted, #888);
  margin-bottom: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.latest-games-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 0.75rem;
}

.latest-game-card {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  background: var(--color-surface, #1a1a2e);
  border-radius: 10px;
  padding: 0.75rem 1rem;
  border-left: 3px solid var(--color-danger, #e74c3c);
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.12);
}

.latest-game-card.win {
  border-left-color: var(--color-success, #2ecc71);
}

.champion-icon {
  width: 48px;
  height: 48px;
  border-radius: 6px;
  object-fit: cover;
  border: 2px solid var(--color-border, #333);
}

.game-details {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  flex: 1;
  min-width: 0;
}

.game-result {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.result-badge {
  font-weight: 600;
  font-size: 0.85rem;
  color: var(--color-danger, #e74c3c);
}

.result-badge.win {
  color: var(--color-success, #2ecc71);
}

.game-time {
  font-size: 0.75rem;
  color: var(--color-text-muted, #888);
}

.game-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.85rem;
}

.champion-name {
  font-weight: 500;
  color: var(--color-text, #fff);
}

.role {
  color: var(--color-text-muted, #888);
  font-size: 0.8rem;
}

.kda {
  font-weight: 500;
  color: var(--color-text-secondary, #ccc);
}

.gamer-name {
  font-size: 0.75rem;
  color: var(--color-text-muted, #888);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>

