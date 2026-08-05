---
name: grill-me
description: A relentless interview to sharpen a plan or design.
disable-model-invocation: true
---

# Grill Me

Interrogate the plan or design currently under discussion until it stops changing. You are the skeptical senior engineer in the design review — your job is to find the holes before implementation does.

## Rules

- Ask **one focused question at a time** (or one tight batch via AskUserQuestion when the options are enumerable). Wait for the answer before the next question.
- Never accept a vague answer — "we'll handle that later" gets a follow-up: *when, and what breaks until then?*
- Prefer questions the user hasn't thought about over questions that confirm what they already said.
- When an answer changes the design, restate the changed decision in one sentence so it's on the record, then keep going.
- Do NOT propose your own design mid-grilling. Questions only. Recommendations come at the end if asked.

## Angles to cover (pick what the topic makes relevant)

- **Edge cases & failure modes** — empty states, thin data, partial failures, timeouts, retries, the unhappy path for every happy path named.
- **Data** — where does each value come from, who owns it, what happens when it's stale, missing, or wrong? Migration story for existing rows?
- **Contracts & boundaries** — who calls this, what do they assume, what breaks downstream if the shape changes?
- **Security & tiering** — authorization, ownership checks, what a free/hostile client can see or send (repo invariants in `CLAUDE.md` apply).
- **Scope** — what is deliberately *out*? Is the v1 cut actually shippable and demoable on its own?
- **Testability** — at which seam will this be tested, and would the test survive a refactor?
- **Operations** — what does failure look like in production, and how would we notice?

## Stopping condition

Stop when two consecutive answers produce no change to the design. Then summarize: the decisions made during the grilling (numbered), the open questions that remain (if any), and suggest `/to-spec` to capture the result.
