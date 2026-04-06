# Task 4: Frontend — Enable View Analysis Button

> **Parent**: [match-details-kpi-redesign.md](match-details-kpi-redesign.md)
> **Type**: Frontend
> **Dependencies**: None (can be done in parallel with Tasks 2–3)
> **File**: `client/src/components/matches/MatchActions.vue`

---

## Objective

Enable the "View Analysis" button to navigate to the Solo Stats dashboard.

## Changes

### Script

```js
import { useRouter } from 'vue-router'

const router = useRouter()

function viewAnalysis() {
  router.push({ name: 'app-solo' })
}
```

### Template

Change the primary button from:

```html
<button class="action-btn primary" disabled title="Coming soon">
```

To:

```html
<button class="action-btn primary" @click="viewAnalysis" aria-label="View analysis on Solo Dashboard">
```

### No Changes

- "View Goal Impact" button stays `disabled` with `title="Coming soon"`
- All existing styles remain unchanged

## Acceptance Criteria

- [ ] "View Analysis" button is enabled and clickable
- [ ] Clicking navigates to `/app/solo` (route name `app-solo`)
- [ ] "View Goal Impact" button remains disabled
- [ ] Button has `aria-label` for accessibility
