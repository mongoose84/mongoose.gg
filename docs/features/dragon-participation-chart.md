# Feature: Dragon Participation Chart

## Problem Statement
First Dragon has a 70.69% win correlation according to academic research, significantly higher than First Blood (59.78%), yet players have no visibility into their objective participation trends. Dragon control is a key team skill that individual players can improve through better map awareness and positioning.

## Proposed Solution
Add a "Dragon Participation" chart showing percentage of dragon attempts where the player was present/participated, with breakdown by dragon types and outcomes (secured vs stolen vs lost). This helps players track their objective game improvement, which has massive win correlation impact.

## User Stories
### Primary User Story
As a player wanting to improve my macro game, I want to see my dragon participation trends so I can track my improvement on high-impact team objectives.

### Additional User Stories
- As a team-focused player, I want to see which dragons I participate in vs miss so I can identify my macro awareness gaps
- As a competitive player, I want to see my team's dragon success rate when I participate vs when I don't to understand my impact
- As an improving player, I want to track my progress toward the 70%+ participation rate target from research

## Requirements

### Functional Requirements
1. Display dragon participation percentage over time as main metric
2. Show breakdown by dragon types (Mountain, Ocean, Infernal, Cloud, Elder, Baron)
3. Color-code participation rate: green for ≥70%, yellow for 50-70%, red for <50%
4. Include outcome data: secured dragons vs lost dragons when participating
5. Default to last 20 games with expand option for full season
6. Show trend line and target line at 70% participation
7. Include hover details with specific dragon types and outcomes

### Non-Functional Requirements
- **Performance**: Efficient calculation of objective participation from timeline data
- **Security**: Use existing match timeline data
- **Accessibility**: Clear visual distinction between participation rates
- **Compatibility**: Responsive design maintains readability

## Technical Approach

### Backend Changes
**Language**: C#
**Components**:
- [ ] Data models: `server/Application/DTOs/Solo/DragonParticipationDto.cs`
- [ ] Business logic: `server/Application/Services/ObjectiveAnalyticsService.cs` - new service
- [ ] API endpoints: `server/Application/Endpoints/Solo/GetDragonParticipation.cs`
- [ ] Database queries: Complex timeline analysis for objective events and player positions

### Frontend Changes
**Framework**: Vue 3

**Components**:
- [ ] UI components: `client/src/components/solo/DragonParticipationChart.vue`
- [ ] Chart integration: Chart.js line chart with percentage scaling
- [ ] State management: Add to SoloPage data management
- [ ] Styles: Follow established TrendChartCard patterns

### Database Changes
**Database**: MySQL

**Schema Changes**:
- [ ] Uses existing `timeline_events` table for ELITE_MONSTER_KILL events
- [ ] May need additional indexing on event types for performance
- [ ] Uses existing `match_participants` for player positioning data

### API Contracts
#### Get Dragon Participation Trend
```
GET /api/solo/dragon-participation?queue={queue}&timeRange={range}
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
      "dragonsAttempted": 3,
      "dragonsParticipated": 2,
      "participationRate": 66.7,
      "dragonsSecured": 2,
      "dragonsLost": 1,
      "dragonTypes": ["MOUNTAIN", "OCEAN", "INFERNAL"]
    }
  ],
  "summary": {
    "averageParticipation": 68.5,
    "targetParticipation": 70.0,
    "totalDragons": 45,
    "participatedIn": 31,
    "successRate": 74.2
  }
}
```

## Testing Strategy

### Unit Tests
**Frameworks**: xUnit (backend), Vitest (frontend)

- [ ] Test dragon participation calculation accuracy
- [ ] Test timeline event parsing for objective events
- [ ] Test participation rate color coding logic
- [ ] Test handling of games with no dragons (very short games)

### Integration Tests
- [ ] Test API endpoint with various match scenarios
- [ ] Test chart rendering with different participation rates
- [ ] Test error handling for incomplete timeline data

### Manual Testing Scenarios
1. **High Participation**: Verify ≥70% shows in green with positive messaging
2. **Medium Participation**: Verify 50-70% shows in yellow with improvement tips
3. **Low Participation**: Verify <50% shows in red with clear improvement guidance
4. **No Dragons**: Verify handling of games where no dragons spawned
5. **Target Line**: Verify 70% target line is visible and appropriately styled

## Validation Criteria
Feature is considered complete when:
- [ ] Chart accurately calculates dragon participation from timeline events
- [ ] Color coding clearly indicates performance relative to 70% research target
- [ ] Breakdown by dragon types provides actionable insight
- [ ] Success rate when participating shows player's team impact
- [ ] Chart fits well into Solo dashboard layout
- [ ] Performance is acceptable with complex timeline calculations
- [ ] Educational messaging helps players understand objective importance

## Design Notes
- Position in Zone 3 of AnalysisLayout as a key macro metric
- Use percentage scale (0-100%) with target line at 70%
- Consider mini-chart showing breakdown by dragon type in tooltip
- Research emphasizes First Dragon > First Blood - highlight this insight
- Include subtle educational text about objective importance
- Color scheme should align with objective control importance (green = strong macro)
- Consider showing team's dragon success rate as context for player participation value