---
name: implement
description: "Implement a piece of work based on a spec or set of tickets."
disable-model-invocation: true
---

Implement the work described by the user in the spec or tickets.

Work test-first where possible, at the pre-agreed seams: write a failing test that pins the behavior, make it pass with the simplest change, then refactor with the test green.

Build regularly (`dotnet build` for backend work), run single test files regularly, and run the full test suite once at the end (see the run-all-tests skill).

Once done, use the two-axis-review skill (`/two-axis-review`) to review the work.

Commit your work to the current branch.
