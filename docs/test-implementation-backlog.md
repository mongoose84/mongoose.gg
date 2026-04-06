# E2E Test Backlog

**Project**: Mongoose.gg  
**Updated**: 2026-04-06  
**Coverage**: Playwright E2E flows — all backend and frontend unit/integration tests have been implemented.

---

## E2E Flows to Add

### Authentication / onboarding
- [ ] Registration flow end-to-end
- [ ] Email verification flow end-to-end
- [ ] Invalid / expired verification code path

### Riot account linking
- [ ] Link Riot account from UI
- [ ] Duplicate account / invalid Riot ID negative path
- [ ] Sync status feedback shown to the user

### Match history journey
- [ ] Navigate from overview to matches
- [ ] Select a match row
- [ ] Open and validate match details
- [ ] Narrative/error fallback handling

### Session and auth resilience
- [ ] Session expiry redirect behavior
- [ ] Unauthenticated access to protected pages
- [ ] API failure / dashboard error-state handling

### Feedback flow
- [ ] Submit feedback successfully
- [ ] Invalid submission / rate-limited submission path

---

## Definition of Done

A backlog item is complete when:
- [ ] The new tests are added in `client/e2e/`
- [ ] The tests follow repo Playwright conventions and helpers
- [ ] Relevant success + failure states are covered
- [ ] The test suite passes locally against the E2E backend mode
- [ ] The new tests improve confidence in real user journeys, not just mocks
