# Restaurant AQA framework

End-to-end test automation for **Green & Tasty**, a restaurant web application. Customers book tables, pre-order dishes and leave feedback, and waiters manage the bookings at their location.

The framework checks the application through its web UI and its REST API. It's built on:

- **Reqnroll** (Gherkin) for scenarios written in business language
- the **Screenplay pattern**: actors perform tasks and ask questions
- **Playwright for .NET** for the browser and HTTP calls
- **TUnit** as the test runner

## What is covered

| Area | Feature files |
|---|---|
| Smoke check of the main pages | `Features/Smoke.feature` |
| Signing in and registration | `Features/Auth.feature`, `Features/Registration.feature` |
| Locations and their menus | `Features/Locations.feature`, `Features/LocationMenus.feature`, `Features/MenuCatalog.feature` |
| Setting up locations, dishes and tables | `Features/Seeding.feature` |
| Reservations | `Features/Reservations.feature` |
| Reports | `Features/Reports.feature` |
| Group pre-order: participants and totals | `Features/GroupPreOrderParticipants.feature` |
| Group pre-order: confirmation and what the waiter receives | `Features/GroupPreOrderConfirmation.feature` |
| Group pre-order: removing participants | `Features/GroupPreOrderRemoval.feature` |

UI scenarios (`@ui`) follow the main user journeys in the browser. API scenarios (`@api`) cover authorisation, validation and edge cases, and prepare test data.

### Group pre-order

The owner of a booking for 2 or more guests can share an invite link. Guests join without an account, choose dishes and confirm their cart. The owner then:

- watches every participant's dishes and totals
- removes participants while the group is open
- confirms the group, which combines the confirmed carts into the booking's pre-order for the waiter

The behaviour the suite expects:

| Case | Expected behaviour |
|---|---|
| Group nobody has joined | `200`, `participants: []`, `total: 0` |
| The owner in the participants list | Listed (type `Owner`) once the owner's own cart has dishes |
| Totals | Per participant and for the whole group, over confirmed and unconfirmed carts |
| Not signed in, or a guest token instead of an account | `401` |
| Signed in but not the owner (another customer, a waiter) | `403` |
| Unknown group, reservation or participant | `404` |
| No confirmed cart | `400` `There are no submitted guest sessions to confirm.` |
| No pre-order for the booking | `400` `Pre-order for this reservation was not found.` |
| Participant of another group, or the owner's own session | `400` |
| Confirming twice, or removing after confirmation | `409` |
| Removing the last participant | Allowed, returns an empty group |
| Confirm button on the page | Enabled while the group is open; a refusal is shown on the page |

## Requirements

- .NET 10 SDK
- Node.js (for Gherkin lint and the git hooks)
- A running deployment of the application: its frontend URL and its API URL
- A **waiter account** in that deployment (waiters can't sign up)

The Playwright browsers are installed automatically on the first run.

## Set up

1. Copy the configuration template:

```bash
cd aqa/restaurant-aqa-framework
cp .env.example .env
```

2. Fill in `.env`:

| Setting | Meaning |
|---|---|
| `UI_URL` | where the frontend is served, for example `http://localhost:4200` or your CloudFront URL |
| `API_URL` | base URL of the backend API, including the stage, for example `https://<api-id>.execute-api.us-west-2.amazonaws.com/develop` |
| `WAITER_USERNAME`, `WAITER_PASSWORD` | an existing waiter account, used to remove the test data afterwards |
| `ADMIN_USERNAME`, `ADMIN_PASSWORD` | optional; without them the admin scenarios are skipped |
| `TESTTABLE_ID`, `TESTTABLE_WAITER_*` | optional; a fixed table and its waiter, for the scenarios that need them |

Real environment variables override `.env`. The full list with defaults is in `.env.example`. `.env` is git-ignored, so never commit it.

3. Create the waiter account if your deployment doesn't have one. Register a user and hire them at a location:

```bash
curl -s -X POST "$API_URL/register" -H "Content-Type: application/json" \
  -d '{"firstName":"Standing","lastName":"Waiter","email":"waiter.aqa@example.com","password":"Waiter-Pass1!","confirmPassword":"Waiter-Pass1!"}'

curl -s -X POST "$API_URL/waiters" -H "Content-Type: application/json" \
  -d "{\"email\":\"waiter.aqa@example.com\",\"locationId\":\"<location id>\"}"
```

4. Build:

```bash
dotnet build
```

5. If you test a frontend on your machine, start it in a second terminal (`npm start` in `frontend/restaurant-frontend-app`) and keep it running. When `UI_URL` can't be reached, every UI scenario stops at once with a clear message.

## Run the tests

```bash
dotnet run --no-build -- --report-trx
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=group-preorder]"
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=level1]"
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=ui]"
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=api]"
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=negative]"
```

The first command runs everything; the others filter by tag. The group pre-order levels are `level1` (participants), `level2` (confirmation) and `level3` (removal).

Some scenarios are expected to fail or are skipped on purpose:

- `@known-defect` documents a bug in the application
- `@wip` is unfinished or needs an admin account

### Reports

Everything a run produces goes to `TestResults/`:

- the TUnit HTML report and TRX
- the Reqnroll report in `TestResults/reqnroll/reqnroll_report.html`
- Playwright traces of failed UI scenarios in `TestResults/traces/`

Open a trace with:

```bash
npx playwright show-trace TestResults/traces/<file>.zip
```

## Test data

Every scenario builds what it needs through the API and removes it afterwards, so scenarios are independent and can run in parallel. A group pre-order scenario creates:

- an `AQA-Sample-` location with its own menu
- a newly registered waiter hired at that location
- a newly registered owner, a table and a booking two days ahead
- guests who join through the real invite link

Clean-up runs in reverse order. Registered accounts can't be deleted through the API, so each one uses a unique e-mail address. Before each run, leftover `AQA-Sample-` dishes and locations from interrupted runs are removed.

## Project structure

| Folder | Contents |
|---|---|
| `Features/` | Gherkin scenarios |
| `StepDefinitions/` | thin glue from steps to Screenplay tasks and questions |
| `Screenplay/Tasks/`, `Screenplay/Questions/` | what actors do and what they check |
| `Ui/` | page objects and components with their locators |
| `Api/` | API clients, DTOs and test-data factories |
| `Hooks/` | browser start-up, per-scenario set-up, traces and clean-up |
| `Configuration/` | reading `.env` and environment variables |

Guidance for extending the suite is in [CLAUDE.md](CLAUDE.md). It covers writing Gherkin, steps, Screenplay code and test data, and working with an AI assistant.

## Quality gates

| Where | What runs |
|---|---|
| Git hooks (local) | `pre-commit`: `dotnet format` check and Gherkin lint on staged files · `commit-msg`: Conventional Commits · `pre-push`: build with analyzers |
| `aqa-quality.yml` | on every push and pull request: format, build (warnings are errors), Gherkin lint, test discovery |
| `aqa-tests.yml` | the scenarios, nightly and on demand |

Enable the hooks once per clone, from the repository root:

```bash
dotnet tool restore --tool-manifest aqa/restaurant-aqa-framework/.config/dotnet-tools.json
git config core.hooksPath aqa/restaurant-aqa-framework/.husky
```

## CI

To run the suite from GitHub Actions, add these under **Settings → Secrets and variables → Actions**:

- **Variables:** `AQA_UI_URL`, `AQA_API_URL`, `AQA_TESTTABLE_ID`
- **Secrets:** `AQA_WAITER_USERNAME`, `AQA_WAITER_PASSWORD`, `AQA_ADMIN_USERNAME`, `AQA_ADMIN_PASSWORD`, `AQA_TESTTABLE_WAITER_USERNAME`, `AQA_TESTTABLE_WAITER_PASSWORD`

Then run **Actions → AQA Tests → Run workflow** and pick a scope: `group-preorder`, `level1`, `level2`, `level3`, `ui` or `api`. Reports and traces are attached to the run as artifacts. A failed scenario doesn't stop the pipeline.

Never put credentials in the repository, in `.env.example` or in test code.

## Recording a new flow

Playwright's code generator records browser actions as code you can split into page objects and tasks. Build first so the script exists, then run in PowerShell from this folder:

```powershell
bin/Debug/net10.0/playwright.ps1 codegen "<UI_URL>"
```

Treat the output as raw material: move locators into `Ui/`, the flow into a task, the checks into questions. Prefer role and accessible name, label or test id over positional selectors. For Gherkin highlighting in your IDE, install the [Reqnroll extension](https://docs.reqnroll.net/latest/ide-integrations/index.html).