# Feature: Vision Score Chart

## Problem Statement
Vision Score is identified as a Tier 1 improvement metric (highest impact + easiest to improve) in the research findings, supporting objective control and map awareness. However, players currently have no visibility into their vision trends over time, missing an easy path to improvement that directly supports macro play and objective control.

## Proposed Solution
Add a "Vision Score Over Time" chart showing vision score per minute trends with role-appropriate targets and comparison to rank averages. This helps players track improvement on a simple but impactful metric that directly supports the high-value activities like dragon control.

## User Stories
### Primary User Story
As a player looking for easy wins, I want to see my vision score trends over time so I can improve on a simple habit that has high game impact.

### Additional User Stories
- As a support player, I want to see if my vision score meets role expectations and how I'm trending
- As a non-support player, I want to understand my vision contribution relative to other players in my role
- As an improvement-focused player, I want to track my progress toward the 1.0+ vision score per minute target from research

## Requirements

### Functional Requirements
1. Display vision score per minute as a line chart over time
2. Show role-appropriate target lines (Support >2.0, others >1.0 per research)
3. Include rank-based benchmarks for comparison
4. Color-code trend: green when above target, yellow approaching target, red below
5. Default to last 20 games with expand option for full season
6. Show both absolute vision score and per-minute rate
7. Include hover details with ward placement/destruction breakdown

### Non-Functional Requirements
- **Performance**: Smooth chart rendering with vision data from match details
- **Security**: Use existing authenticated match participant data
- **Accessibility**: Clear target lines and role-appropriate messaging
- **Compatibility**: Responsive design for all screen sizes

## Technical Approach

### Backend Changes
**Language**: C#
**Components**:
- [ ] Data models: `server/Application/DTOs/Solo/VisionTrendDto.cs`
- [ ] Business logic: `server/Application/Services/VisionAnalyticsService.cs` - new service
- [ ] API endpoints: `server/Application/Endpoints/Solo/GetVisionTrend.cs`
- [ ] Database queries: Extract vision score and game duration from participants table

### Frontend Changes
**Framework**: Vue 3

**Components**:
- [ ] UI components: `client/src/components/solo/VisionChart.vue`
- [ ] Chart integration: Chart.js line chart with role-specific target lines
- [ ] State management: Add to existing SoloPage data fetching
- [ ] Styles: Match TrendChartCard design system

### Database Changes
**Database**: MySQL

**Schema Changes**:
- [ ] Uses existing `match_participants.vision_score` column
- [ ] Uses existing `matches.game_duration` for per-minute calculation
- [ ] No new tables or indexes required

### API Contracts
#### Get Vision Score Trend
```
GET /api/solo/vision-trend?queue={queue}&timeRange={range}
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
      "visionScore": 45,
      "visionScorePerMinute": 1.2,
      "gameDuration": 2250,
      "role": "ADC",
      "championName": "Jinx",
      "wardsPlaced": 12,
      "wardsDestroyed": 3
    }
  ],
  "summary": {
    "averageVisionPerMinute": 1.35,
    "roleTarget": 1.0,
    "rankAverage": 0.85,
    "trend": "improving"
  }
}
```

## Testing Strategy

### Unit Tests
**Frameworks**: xUnit (backend), Vitest (frontend)

- [ ] Test vision score per minute calculation accuracy
- [ ] Test role-specific target line positioning
- [ ] Test color coding for different performance levels
- [ ] Test chart scaling with various vision score ranges

### Integration Tests
- [ ] Test API endpoint with different roles and queue types
- [ ] Test chart rendering with role-specific targets
- [ ] Test error handling for missing vision data

### Manual Testing Scenarios
1. **Support Role**: Verify high target line (>2.0/min) and appropriate messaging
2. **Non-Support Roles**: Verify standard target line (>1.0/min)
3. **Above Target**: Verify green trend and positive reinforcement
4. **Below Target**: Verify red trend with improvement suggestions
5. **Trend Analysis**: Verify rolling average shows improvement/decline accurately

## Validation Criteria
Feature is considered complete when:
- [ ] Chart accurately displays vision score per minute over time
- [ ] Role-specific target lines provide appropriate benchmarks
- [ ] Color coding clearly indicates performance relative to targets
- [ ] Tooltip information includes ward placement/destruction breakdown
- [ ] Chart integrates seamlessly into Solo dashboard layout
- [ ] Educational messaging helps players understand vision importance
- [ ] Performance is acceptable with large datasets

## Design Notes
- Position in Zone 3 of AnalysisLayout as a foundational macro skill
- Use per-minute scaling to normalize for game length differences
- Target lines: Support 2.0+, other roles 1.0+ based on research
- Color scheme: green for good vision, yellow for improving, red for poor
- Include subtle educational messaging about vision's role in objective control
- Research shows vision supports dragon control (70% win correlation) - connect these concepts
- Consider mini breakdown in tooltip: wards placed vs destroyed vs vision score