# Statistical Research: League of Legends Win Prediction Metrics

> **Research Date**: February 2026  
> **Purpose**: Identify metrics for Mongoose.gg user improvement goals  
> **Sources**: Academic papers, statistical analyses, Riot API data studies

---

## Executive Summary

This document synthesizes research from multiple academic papers and statistical studies to identify:
1. Which metrics correlate most strongly with winning
2. Which metrics are most actionable for individual players
3. Which metrics are easiest to improve

**Key Finding**: Death reduction is the single most impactful AND easiest metric for players to improve.

---

## 1. Gold Differential - The #1 Predictor

**Source**: SIDO Performance Model (arXiv:2403.04873v1)

| Game Phase | Win Correlation with Gold Lead |
|------------|-------------------------------|
| 0-7 minutes (Early) | **69%** |
| 7-15 minutes (Mid) | **79%** |
| 15-25 minutes (Late) | **83%** |

Gold differential becomes increasingly predictive as the game progresses. By mid-game, the team with the gold lead wins ~80% of games.

---

## 2. First Objective Correlations

**Source**: LeagueMath.com Statistical Analysis

| Objective | Win Rate Correlation | Priority |
|-----------|---------------------|----------|
| **First Dragon** | **70.69%** | ⭐ Highest Early |
| First Tower | 65.42% | High |
| First Blood | 59.78% | Medium |
| First Inhibitor | 79.28% | Late-game |
| First Baron | 50.06% | Low (surprising) |

**Key Insight**: First Dragon has significantly higher win correlation than First Blood.

---

## 3. Damage Dealt Correlation

**Source**: SIDO Performance Model

| Game Phase | Win Correlation |
|------------|----------------|
| 0-7 minutes | 63% |
| 7-15 minutes | 73% |
| 15-25 minutes | 72% |

---

## 4. Metric Tiers by Actionability

### Tier 1: Highest Impact + Easiest to Improve

| Metric | Why It Matters | Why It's Easy |
|--------|---------------|---------------|
| **Deaths/Game** | Cascading negative effects | Pure decision-making |
| **Vision Score** | Supports objective control | Simple habit change |
| **Dragon Participation** | 70.69% win correlation | Map awareness |

### Tier 2: High Impact + Moderate Difficulty

| Metric | Why It Matters | Improvement Path |
|--------|---------------|------------------|
| CS/min | Direct gold income | Mechanical practice |
| Gold @ 15 | Mid-game predictor | Laning fundamentals |
| Tower Participation | 65.42% correlation | Team coordination |

### Tier 3: Context-Dependent

| Metric | Notes |
|--------|-------|
| KDA Ratio | Depends on team performance |
| Damage/Gold | Role and champion dependent |
| Kill Participation | Team-dependent |

---

## 5. Why Deaths Are the #1 Improvement Target

Each death causes:
- ~300 gold given to enemies
- 20-60 seconds of map pressure lost
- Potential objective loss
- Snowball effect on gold differential

**Critical**: Deaths are **entirely preventable through player decisions**.

Average KDA across all players: ~2.7  
KDA Formula: (Kills + Assists) / max(1, Deaths)

---

## 6. Session Performance Deterioration

**Source**: PMC - Individual Performance in Team-Based Online Games (2018)

| Finding | Impact |
|---------|--------|
| Win rate declines | **10%+** from first to last match in session |
| KDA declines | **8%+** from first to last match in session |
| Experience helps | Veterans show less deterioration than novices |

**Recommendation**: Track session performance and suggest breaks after decline detected.

---

## 7. Recommended Goals for Mongoose.gg Users

### Primary Goals (Week 1-2 Focus)
1. **Reduce Deaths**: Target -1 death/game vs current average
2. **Dragon Participation**: Be present for 70%+ of dragon attempts
3. **Gold @ 15**: Achieve +500g vs lane opponent

### Secondary Goals (Month 1 Focus)
1. **CS/min**: Improve by +0.5 vs current average
2. **Vision Score**: Maintain 1.0+ vision score per minute
3. **First Tower**: Participate in first tower 60%+ of games

---

## 8. Implementation Recommendations

### Metrics to Track Prominently
1. Deaths/game with trend analysis
2. Gold @ 10 and @ 15 differentials
3. First dragon/tower participation rate
4. Session performance trend

### Goal System Design
- Set specific, measurable targets
- Compare to role/rank averages
- Track improvement over time periods (1w, 1m, 3m)
- Celebrate milestone achievements

### Session Tracking
- Monitor performance within session
- Alert after 3+ consecutive games
- Suggest breaks when performance declines

---

## 9. Academic Sources

1. **SIDO Performance Model** - Zhang & Naidu (2024), arXiv:2403.04873v1
2. **Individual Performance in Team-Based Online Games** - Sapienza et al. (2018), Royal Society Open Science
3. **LeagueMath Statistical Analysis** - First objective correlations
4. **LoL Esports Community Statistical Studies** - Various corroborating data

---

## 10. Key Takeaways

1. **Gold is the primary win predictor** - Focus on gold-generating activities
2. **First Dragon > First Blood** - Objective control matters more than kills
3. **Deaths are controllable** - The easiest high-impact metric to improve
4. **Fatigue is real** - 8-10% performance decline over extended sessions
5. **Context matters** - Simple averages without game state are insufficient

