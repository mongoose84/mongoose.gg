# Feature: Gold at 15 Chart

## Problem Statement
Gold differential is the #1 predictor of winning games according to academic research, with 79% win correlation by mid-game (7-15 minutes). However, players currently have no visibility into their gold economy performance over time, missing the most predictive metric for game outcomes and improvement.

## Proposed Solution
Add a "Gold at 15" chart showing players' gold accumulation at the 15-minute mark across games, with comparison to opponents in their lane and rank-appropriate benchmarks. This reveals the most predictive metric for winning and helps players focus on economy improvement.

## User Stories
### Primary User Story
As a competitive player, I want to see my gold at 15 minutes over time so that I can track my performance on the metric that best predicts winning games.

### Additional User Stories
- As a laning-focused player, I want to see how my gold at 15 compares to my lane opponent to identify my laning strength/weakness trends
- As an improvement-focused player, I want to see my gold differential trends so I can focus on the metric with highest win correlation
- As a role player, I want to see role-appropriate gold benchmarks so I understand realistic targets for my position

## Requirements

### Functional Requirements
1. Display player's gold at 15 minutes as a line chart over time 
2. Show opponent's gold at 15 for comparison (differential visualization)
3. Include role-appropriate benchmark lines (e.g., ADC vs Support targets)
4. Color-code based on gold lead/deficit: green for leads, red for deficits
5. Default to last 20 games with expand option for full season
6. Show gold differential as primary metric with absolute gold as secondary
7. Include hover details with match context (champion, role, opponent)

### Non-Functional Requirements
- **Performance**: Smooth rendering with large datasets 
- **Security**: Use existing authenticated match data
- **Accessibility**: Clear legends, color-blind safe palette
- **Compatibility**: Responsive across screen sizes

## Technical Approach

### Backend Changes
**Language**: C#
**Components**:
- [ ] Data models: `server/Application/DTOs/Solo/GoldTrendDto.cs`
- [ ] Business logic: `server/Application/Services/SoloAnalyticsService.cs` - gold at 15 calculation
- [ ] API endpoints: `server/Application/Endpoints/Solo/GetGoldTrend.cs`
- [ ] Database queries: Extract timeline data at 15-minute mark from match timelines

### Frontend Changes
**Framework**: Vue 3

**Components**:
- [ ] UI components: `client/src/components/solo/GoldChart.vue`
- [ ] Chart integration: Chart.js with dual-axis support (differential + absolute)
- [ ] State management: Integrate with existing SoloPage data flow
- [ ] Styles: Match TrendChartCard design patterns

### Database Changes
**Database**: MySQL

**Schema Changes**:
- [ ] Uses existing `match_timelines` and `timeline_events` tables
- [ ] May require index optimization for timeline queries at specific timestamps
- [ ] No new tables needed

### API Contracts
#### Get Gold at 15 Trend
```
GET /api/solo/gold-trend?queue={queue}&timeRange={range}
```
**Response**:
```json
{
  "success": true,
  "data": [
    {
      "matchId": "NA1_4567890123",
      "timestamp": "2026-02-14T20:30:00Z",
      "gameIndex": 1,
      "playerGold": 6750,
      "opponentGold": 6200,
      "goldDifferential": 550,
      "championName": "Jinx",
      "role": "ADC",
      "opponentChampion": "Caitlyn"
    }
  ],
  "summary": {
    "averageGoldAt15": 6890,
    "averageDifferential": 245,
    "roleTarget": 7200,
    "trend": "improving"
  }
}
```

## Testing Strategy

### Unit Tests
**Frameworks**: xUnit (backend), Vitest (frontend)

- [ ] Test gold differential calculation accuracy
- [ ] Test role-based target calculation
- [ ] Test chart color coding for leads/deficits
- [ ] Test timeline data extraction at 15-minute mark

### Integration Tests
- [ ] Test API endpoint with different roles/champions
- [ ] Test chart rendering with various gold differential ranges
- [ ] Test error handling for games shorter than 15 minutes

### Manual Testing Scenarios
1. **Gold Lead Games**: Verify positive differential shows in green
2. **Gold Deficit Games**: Verify negative differential shows in red
3. **Role Targets**: Verify appropriate benchmarks for ADC vs Support vs etc
4. **Short Games**: Verify handling of games that end before 15 minutes
5. **Extreme Values**: Test with very large gold leads/deficits

## Validation Criteria
Feature is considered complete when:
- [ ] Chart accurately displays gold at 15 with opponent comparison
- [ ] Gold differential is the primary visual focus
- [ ] Color coding clearly indicates performance (green leads, red deficits)
- [ ] Role-appropriate targets provide meaningful improvement goals
- [ ] Chart integrates into Solo dashboard layout without crowding
- [ ] Performance acceptable with full season of data
- [ ] Tooltip information is comprehensive and helpful

## Design Notes
- Position in Zone 3 of AnalysisLayout alongside other performance charts
- Use dual-line chart: player gold (solid) vs opponent gold (dashed)
- Alternative: Show gold differential as primary line with absolute values in tooltip
- Research shows gold differential becomes 79% predictive by mid-game - emphasize this in UI
- Include subtle reference line at 0 differential for easy visual parsing
- Consider role-specific color coding: ADCs should have higher targets than supports