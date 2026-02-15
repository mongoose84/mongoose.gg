# Feature: Enhanced Summary Stats Card with Ranked Display

## Problem Statement
The current SummaryStatsCard shows generic stats for all queues without contextual ranked information. Players need to see their rank progression and context-appropriate statistics based on their selected queue to better understand their competitive standing and improvement areas.

## Proposed Solution
Enhance the SummaryStatsCard to dynamically display ranked information based on queue selection:
- **All Queues**: Show both Solo/Duo and Flex ranks side by side
- **Solo/Duo**: Show only Solo/Duo rank and LP
- **Ranked Flex**: Show only Flex rank and LP  
- **Normal/ARAM**: No rank display

## User Stories
### Primary User Story
As a ranked player, I want to see my current rank and LP alongside my performance stats so that I can understand my competitive progress in the context of my recent games.

### Additional User Stories
- As a player filtering by Solo/Duo queue, I want to see only my Solo/Duo rank so that the display is focused and relevant
- As a player viewing All Queues, I want to see both my Solo/Duo and Flex ranks so I can compare my performance across ranked modes
- As a player viewing Normal/ARAM games, I don't want to see ranked information that would be irrelevant to casual play

## Requirements

### Functional Requirements
1. Display rank information contextually based on selected queue filter
2. Show current tier, division, and LP for relevant ranked queues
3. Include small rank badge/icon for visual recognition
4. Maintain existing stats display (Games, Winrate, K/D/A breakdown)
5. Handle cases where players have no rank in a queue gracefully

### Non-Functional Requirements
- **Performance**: No additional API calls - use existing user profile data
- **Security**: Display publicly available rank information only
- **Accessibility**: Ensure rank badges have appropriate alt text and color contrast
- **Compatibility**: Consistent across all supported browsers

## Technical Approach

### Backend Changes
**Language**: C#
**Components**:
- [ ] Data models: `server/Application/DTOs/UserProfileDto.cs` (extend existing)
- [ ] Business logic: Use existing rank data from profile endpoint
- [ ] API endpoints: No new endpoints needed - enhance existing dashboard endpoint response
- [ ] Database migrations: None required (uses existing summoner data)

### Frontend Changes
**Framework**: Vue 3

**Components**:
- [ ] UI components: `client/src/components/solo/SummaryStatsCard.vue` (enhance existing)
- [ ] UI components: `client/src/components/base/BaseRankBadge.vue` (new reusable component)
- [ ] State management: Use existing queue filter state from SoloPage
- [ ] Styles: Extend existing card styles with rank display sections

### Database Changes
**Database**: MySQL

**Schema Changes**:
- [ ] No new tables required
- [ ] Uses existing `summoners` table with `solo_duo_rank`, `flex_rank` columns
- [ ] No new indexes needed

### API Contracts
#### Enhanced Dashboard Response
```
GET /api/solo/dashboard?queue={queue}&timeRange={range}
```
**Response Enhancement**:
```json
{
  "gamesPlayed": 42,
  "winRate": 65.8,
  // ... existing fields ...
  "rankInfo": {
    "soloDuoRank": {
      "tier": "GOLD",
      "division": "II", 
      "lp": 78,
      "hasRank": true
    },
    "flexRank": {
      "tier": "SILVER", 
      "division": "I",
      "lp": 45,
      "hasRank": true
    }
  }
}
```

## Testing Strategy

### Unit Tests
**Frameworks**: xUnit (backend), Vitest (frontend)

- [ ] Test rank display logic for each queue filter state
- [ ] Test BaseRankBadge component with various rank inputs
- [ ] Test graceful handling of missing rank data
- [ ] Test SummaryStatsCard integration with rank display

### Integration Tests
- [ ] Test dashboard endpoint includes rank information
- [ ] Test queue filtering affects rank display appropriately
- [ ] Test error scenarios (invalid rank data)

### Manual Testing Scenarios
1. **All Queues View**: Verify both Solo/Duo and Flex ranks appear when both exist
2. **Solo/Duo Filter**: Verify only Solo/Duo rank appears, Flex rank hidden
3. **Flex Filter**: Verify only Flex rank appears, Solo/Duo rank hidden  
4. **Normal/ARAM Filter**: Verify no rank information displayed
5. **Unranked Players**: Verify graceful handling when ranks don't exist
6. **Edge Cases**: Test with provisional ranks, high tiers (Master+)

## Validation Criteria
Feature is considered complete when:
- [ ] Rank display changes appropriately based on queue filter selection
- [ ] Rank badges display correct tier/division/LP information
- [ ] No rank information shown for Normal/ARAM queues
- [ ] Both ranks shown side-by-side for "All Queues" filter
- [ ] Existing SummaryStatsCard functionality preserved
- [ ] All tests pass
- [ ] Visual design matches UI/UX specification standards

## Design Notes
- Rank badges should be compact and positioned above or beside the main stats
- Use official League of Legends rank icons/colors for immediate recognition
- Maintain the card's scannable, at-a-glance design principles
- Consider responsive behavior for smaller screens where two ranks are shown