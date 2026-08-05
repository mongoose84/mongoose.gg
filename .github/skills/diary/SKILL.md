---
name: diary
description: 'Project work diary. Use at session start to load context, or at session end to record what was done this session.'
argument-hint: '"read" (default) to surface this week''s log at session start, "log" to append a summary of the current session'
---

# Diary

Manages a weekly work log stored in `.github/diary/`. Files are named by ISO week: `YYYY-Www.md` (e.g., `2026-W23.md`).

## Getting the current week filename

Run this in PowerShell to get the filename:

```powershell
$isoWeek = Get-Date -UFormat "%V"
$isoYear = Get-Date -UFormat "%G"
$filename = ".github/diary/$isoYear-W$isoWeek.md"
```

## Read mode — `/diary` or `/diary read`

Use at the start of a session to load context.

1. Calculate the current week filename
2. Check if the file exists
3. If it exists: read it and surface the contents to the user — what was worked on, any blockers, what was next
4. If it does not exist: create it with just the week header (see File Format below), then tell the user this is a new week with no entries yet

## Log mode — `/diary log`

Use mid-session or at the end of a session to record what was done.

1. Calculate the current week filename
2. If the file does not exist, create the `.github/diary/` directory if needed, then create the file with the week header
3. Write a new dated entry summarizing the current session — you write the summary based on the conversation context
4. Keep it concise: 2–4 sentences. Cover what was worked on, any blockers hit, and what comes next
5. Append the entry at the bottom of the file

## File format

```markdown
# Week WW — YYYY

## YYYY-MM-DD
Summary of what was worked on. Any blockers. What's next.

## YYYY-MM-DD
Another entry.
```
