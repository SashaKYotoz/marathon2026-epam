# Restaurant AQA framework

**Chosen direction:** Direction 1 — Group pre-order (own Backend + Frontend, deployed to AWS).
**Approach:** BDD (Reqnroll + Gherkin) with the Screenplay pattern, Playwright for .NET, TUnit runner — the framework's existing architecture, extended with group pre-order tasks, questions, page objects and API clients.

## Group pre-order suite (homework)

| Level | Feature file | UI scenarios | API scenarios |
|---|---|---|---|
| L1 — participants | `Features/GroupPreOrderParticipants.feature` | owner sees participants, dishes, quantities and totals; total follows a guest's cart; guest without dishes; empty group | totals add up, cart change, empty group, access by everyone but the owner, unknown group |
| L2 — confirmation | `Features/GroupPreOrderConfirmation.feature` | combined + unconfirmed left out; owner's own dishes; waiter sees the order; refusal message on the page | combined order reaches the restaurant, owner's own cart, every refusal condition, second confirmation, non-owners |
| L3 — removal | `Features/GroupPreOrderRemoval.feature` | remove one of several; full journey guests → owner → waiter | returned snapshot, remove a confirmed participant, remove the last one, removed dishes not confirmed, participant of another group, unknown participant, non-owners |

Test pyramid: UI scenarios cover the main user journeys, API scenarios cover authorisation, validation and data preparation (guests joining, their carts).

### Run a subset

```bash
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=group-preorder]"   # the whole homework suite
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=level1]"           # one level (level1|level2|level3)
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=ui]"               # only UI scenarios
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=negative]"         # only refusals
```

In CI: Actions → AQA Tests → Run workflow → scope `group-preorder`, `level1`, `level2`, `level3`, `ui` or `api`.

Reports land in `TestResults/`: TUnit HTML/TRX, `TestResults/reqnroll/reqnroll_report.html`, and Playwright traces of failed UI scenarios in `TestResults/traces/` (`npx playwright show-trace <file>.zip`). CI uploads all of them as artifacts; a failed test does not stop the pipeline.

### Test data

Every scenario builds its own world through the API and removes it afterwards: an `AQA-Sample-` location with its own menu (dish names and prices come from the feature's table), a freshly registered account hired as the location's waiter (so every booking there is assigned to it), a freshly registered owner, a table sized for the booking, the booking itself (two days ahead, generated from the current date), and guests who join through the real invite link. Clean-up runs in reverse order: guests are removed from the group, the pre-order and the booking are cancelled, the waiter is released, then dishes, table and location are deleted. Registered accounts cannot be deleted through the API, so they use unique e-mail addresses. Only `WAITER_USERNAME`/`WAITER_PASSWORD` (the standing waiter, used for deleting sample data) and the URLs are required.

### Behaviour chosen where the assignment leaves it open

| Case | Chosen behaviour |
|---|---|
| Group nobody joined | `200`, `participants: []`, `total: 0` |
| The owner in the participants list | Listed (type `Owner`, first name) only once the owner's own cart has dishes |
| Totals | Per participant and for the group, over submitted and not yet submitted carts |
| Not signed in, or a guest session token instead of an account | `401` from the API Gateway authorizer |
| Signed in but not the owner (another customer, the waiter) | `403` |
| Unknown group / reservation | `404` |
| Confirming a second time, or a closed group | `409` `Group reservation was submitted.` / `Group reservation is expired.` |
| No submitted cart | `400` `There are no submitted guest sessions to confirm.` |
| Submitted carts without dishes | `400` `Submitted guest sessions do not contain any dishes.` — not reachable through the public API because an empty cart cannot be submitted (`The cart is empty`), so the suite checks that, and the backend unit tests cover the guard |
| No pre-order for the reservation | `400` `Pre-order for this reservation was not found.` — the group's dishes are added to the reservation's existing pre-order |
| Owner's own cart | `GET /reservations/{id}/guest-session-id` creates it on first call; the owner submits it like any guest, only submitted carts are combined |
| Participant of another group | `400` `Guest session does not belong to this group reservation.` |
| Unknown participant | `404` |
| Removing the owner's own session | `400` |
| Removing after the group was confirmed | `409` |
| Removing the last participant | Allowed, returns `participants: []`, `total: 0` |
| UI confirm button | Always enabled while the group is open; the page shows the backend's reason when confirming is refused |


End-to-end tests for the Green & Tasty restaurant application: Reqnroll (Gherkin) + TUnit + Playwright for .NET, with the Screenplay pattern.

Guidance for extending the suite (writing Gherkin, steps, Screenplay, test data, working with an AI assistant) is in [CLAUDE.md](CLAUDE.md).

## Run the tests

```bash
cp .env.example .env        # then fill in your own values (see "Configuration")
dotnet build
dotnet run --no-build -- --report-trx                          # everything
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=smoke]"   # by tag
```

Everything a run produces lands in `TestResults/`: the TUnit report and TRX, the Reqnroll report in `reqnroll/`, and Playwright traces in `traces/` (open one with `npx playwright show-trace <file>.zip`).

Some scenarios are expected to fail (`@known-defect`, bugs in the application) or are skipped (`@wip`, no admin account configured). That is intentional.

## Quality gates

| Where | What runs |
|---|---|
| Git hooks (local) | `pre-commit`: `dotnet format` check and Gherkin lint on staged files · `commit-msg`: Conventional Commits · `pre-push`: build with analyzers |
| `aqa-quality.yml` | on every push and pull request: no generated files tracked, format, build (warnings are errors), Gherkin lint, test discovery |
| `aqa-tests.yml` | the scenarios themselves, nightly and on demand (Actions → AQA Tests → Run workflow) |

Enable the hooks once per clone, from the repository root:

```bash
dotnet tool restore --tool-manifest aqa/restaurant-aqa-framework/.config/dotnet-tools.json
git config core.hooksPath aqa/restaurant-aqa-framework/.husky
```

The hooks only act on commits and pushes that touch `aqa/restaurant-aqa-framework`. Gherkin lint needs Node.js.

## Configuration

There is no shared environment and no committed `.env`: copy `.env.example` to `.env` and fill it in with the data of **your own** deployment (see "Testing your own deployment"). `.env` is git-ignored; never commit it.

Settings come from `.env` or real environment variables (variables win):

| Setting | Meaning |
|---|---|
| `UI_URL` | where the frontend is served |
| `API_URL` | base URL of the backend API, including the stage (for example `…/develop`) |
| `WAITER_USERNAME`, `WAITER_PASSWORD` | a waiter account that already exists in the environment under test |
| `TESTTABLE_ID`, `TESTTABLE_WAITER_*` | a table and the waiter assigned to it (only for scenarios that need them) |
| `ADMIN_USERNAME`, `ADMIN_PASSWORD` | an admin account; without it the admin scenarios are skipped |

The full list, with defaults, is in `.env.example`.

## Testing your own deployment

### If you deployed the backend / frontend / DevOps homework

1. `cp .env.example .env` and set `UI_URL` and `API_URL` to your deployment (API URL includes the stage, for example `…/develop`).
2. Waiters cannot be created by signing up, so create a waiter (and, for the admin scenarios, an admin) in *your* environment and put its credentials in `WAITER_*` / `ADMIN_*`. Optionally add a table and its assigned waiter (`TESTTABLE_*`).
3. Run `dotnet build` and a small slice first (`--treenode-filter "/*/*/*/*[Category=smoke]"`).

### If you have not deployed your own homework

If you deployed the original application, test that: set `UI_URL` / `API_URL` as above. Otherwise point the framework at a comparable public site (Booking, Airbnb, OpenTable, …) and write all the layers for it yourself: Gherkin features, step definitions, Screenplay tasks and questions, page objects with locators, and API clients where the site has a usable API. Use only flows you are allowed to automate (no purchases, no real bookings, respect the site's terms), and set `UI_URL` to that site.

1. **Generate a skeleton with Playwright codegen** (see "Presetup instructions"): record one flow, for example search → open a result → check details. Save the generated code to a scratch file, it is only raw material.
2. **Split it across the layers**: locators and small actions go to `Ui/` page objects, the flow becomes a task in `Screenplay/Tasks/`, the read-backs become questions in `Screenplay/Questions/`, the business wording goes to `Features/` and thin glue to `StepDefinitions/`.
3. **Replace weak locators.** Codegen often emits positional or text-fragile selectors. Prefer role + accessible name, label, test id, then CSS (see [CLAUDE.md](CLAUDE.md)); check them in the Playwright inspector.
4. Tag scenarios `@ui` / `@api`, keep them independent, and run `npx gherkin-lint -c .gherkin-lintrc "Features/*.feature"`.

### CI on your own public repository

Publish your work in your own public Git repository (the link is the deliverable). Workflows are disabled in a fresh fork until you enable them. Add the same values under Settings → Secrets and variables → Actions:

- **Variables:** `AQA_UI_URL`, `AQA_API_URL`, `AQA_TESTTABLE_ID`
- **Secrets:** `AQA_WAITER_USERNAME`, `AQA_WAITER_PASSWORD`, `AQA_ADMIN_USERNAME`, `AQA_ADMIN_PASSWORD`, `AQA_TESTTABLE_WAITER_USERNAME`, `AQA_TESTTABLE_WAITER_PASSWORD`

`aqa-quality.yml` runs on every push and pull request; `aqa-tests.yml` (nightly and on demand) tests whatever those variables point at. Never put credentials in the repository, in `.env.example` or in test code.

## Presetup instructions

This framework contains PlaywriteInstaller script so you don't need to setup it manually.
For full BDD highlighting in your IDE you may need to install Reqnroll extension https://docs.reqnroll.net/latest/ide-integrations/index.html
To start the Playwright code generator open Powershell in the framework directory and enter the line:
```bash
bin/Debug/net10.0/playwright.ps1 codegen "<your UI_URL or the public site you test>"
```
Build first so `playwright.ps1` exists. The URL is optional.
