<template>
  <div v-if="latestGame && latestGame.hasGame" class="latest-game-together">
    <h3 class="section-title">Latest Game Together</h3>
    <div class="game-header" :class="{ win: latestGame.win }">
      <span class="result-badge" :class="{ win: latestGame.win }">
        {{ latestGame.win ? 'Victory' : 'Defeat' }}
      </span>
      <span class="game-time">{{ formatTimeAgo(latestGame.gameEndTimestamp) }}</span>
    </div>
    <div class="players-grid">
      <div
        v-for="player in latestGame.players"
        :key="player.puuid"
        class="player-card"
      >
        <img
          :src="getChampionImageUrl(player.championName)"
          :alt="player.championName"
          class="champion-icon"
        />
        <div class="player-details">
          <div class="player-name">{{ player.gamerName }}</div>
          <div class="player-info">
            <span class="champion-name">{{ player.championName }}</span>
            <span class="role">{{ formatRole(player.role) }}</span>
          </div>
          <div class="kda">{{ player.kills }}/{{ player.deaths }}/{{ player.assists }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  latestGame: {
    type: Object,
    default: null,
  },
});

function formatRole(role) {
  if (!role || role === 'UNKNOWN') return '';
  return role.charAt(0).toUpperCase() + role.slice(1).toLowerCase();
}

function getChampionImageUrl(championName) {
  return `https://ddragon.leagueoflegends.com/cdn/14.24.1/img/champion/${championName}.png`;
}

function formatTimeAgo(timestamp) {
  if (!timestamp) return '';
  const date = new Date(timestamp);
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
.latest-game-together {
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

.game-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  background: var(--color-surface, #1a1a2e);
  border-radius: 10px 10px 0 0;
  border-left: 3px solid var(--color-danger, #e74c3c);
}

.game-header.win {
  border-left-color: var(--color-success, #2ecc71);
}

.result-badge {
  font-weight: 600;
  font-size: 0.9rem;
  color: var(--color-danger, #e74c3c);
}

.result-badge.win {
  color: var(--color-success, #2ecc71);
}

.game-time {
  font-size: 0.8rem;
  color: var(--color-text-muted, #888);
}

.players-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  background: var(--color-surface-alt, #16162a);
  padding: 0.75rem;
  border-radius: 0 0 10px 10px;
}

.player-card {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  background: var(--color-surface, #1a1a2e);
  border-radius: 8px;
  padding: 0.5rem 0.75rem;
  flex: 1 1 auto;
  min-width: 180px;
  max-width: 280px;
}

.champion-icon {
  width: 40px;
  height: 40px;
  border-radius: 6px;
  object-fit: cover;
  border: 2px solid var(--color-border, #333);
}

.player-details {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
  flex: 1;
  min-width: 0;
}

.player-name {
  font-weight: 500;
  font-size: 0.85rem;
  color: var(--color-text, #fff);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.player-info {
  display: flex;
  gap: 0.5rem;
  font-size: 0.75rem;
  color: var(--color-text-muted, #888);
}

.champion-name {
  color: var(--color-primary, #6366f1);
}

.role {
  opacity: 0.7;
}

.kda {
  font-size: 0.8rem;
  font-weight: 500;
  color: var(--color-text, #fff);
}
</style>

