# 04 — Domain modelling: the work-order state machine

**Week 3.** The Maintenance module: a maintenance request becomes a work order, which moves through an explicit
state machine. Each step is done by the right person, versioned and audited. Technicians execute a checklist and
record findings with photos from a phone-sized page.

## Concepts you will meet

*(Written before the week's code, as CLAUDE.md requires.)*

- **A state machine as a table, not as `if` statements.** All legal moves (`Requested → Approved`, `Scheduled →
  On hold`, …) live in one transition table. Anything not in the table is illegal by default, and one test walks
  the whole table.
- **Aggregates and invariants.** `WorkOrder` is the only way to change a work order's state, and it refuses to be
  put into an invalid one: no closing without a disposition and a resolved checklist, no cancelling without a
  reason. The rules live in the domain object, not in the HTTP endpoint.
- **Two kinds of authorisation.**
  - *Role-based*: only a planner may approve. Checked by a policy on the route.
  - *Resource-based*: only **the technician assigned to this work order** may start it. Checked against the data
    itself.
- **Versioned edits.** Every change raises a revision number and writes a history entry (who, when, from which
  state, why). Optimistic concurrency (`xmin`, from week 2) means two people acting on the same work order cannot
  silently overwrite each other.
- **History versus audit.** The module's own history table drives the timeline in the UI. The audit trail is the
  tamper-evident record for compliance. Why keep both, and why they are written in the same transaction.
- **Talking to another module without reaching into it.** Maintenance needs to know that an asset exists in the
  caller's tenant. It asks through a small query contract (`IAssetDirectory`) that the Assets module implements,
  never through the `assets` schema.
- **Secure file upload.**
  - Never trust the file name or the declared content type. Check the **magic bytes** (the first bytes of the
    file) against a short allow-list.
  - Enforce size limits before reading everything into memory.
  - Store files under generated names, outside the web root.
  - Serve them back with `nosniff` and as downloads.
- **Ports and adapters for storage.** An `IAttachmentStore` interface with a local-disk adapter now and an Azure
  Blob adapter in week 9. What "the domain does not know where bytes live" buys you.
- **Table-driven tests.** One test that enumerates every (state, action) pair and asserts allowed or refused.
  When someone adds a state, the test tells them which transitions they forgot to decide on.
- **Responsive design for a real constraint.** The technician page has to work at 360 px wide (a small phone):
  single column, large touch targets, no tables that scroll sideways.

---

## 1. The lifecycle is a table

[WorkOrderStateMachine.cs](../../src/platform-api/Modules/SmartFactory.Modules.Maintenance/Domain/WorkOrderStateMachine.cs)
lists every legal move as a row: *from*, *action*, *to*, *who*. `Cancel` is generated for every open state, and
re-scheduling while already scheduled is a row too (same state, new technician or date). Anything not in the table
throws `illegal-transition`.

| From | Action | To | Who |
|---|---|---|---|
| Requested | Approve / Reject | Approved / Rejected | planner, reliability manager, admin |
| Approved, Scheduled | Schedule | Scheduled | planner, admin |
| Scheduled | Start | In progress | **the assigned technician** |
| Scheduled, In progress | Hold | On hold | planner or assigned technician |
| On hold | Resume | back to where it was held from | planner or assigned technician |
| In progress | Complete | Completed | **the assigned technician** |
| Completed | Close | Closed | planner, admin |
| any open state | Cancel | Cancelled | planner, admin |

The same table drives the UI. The API returns `availableActions` for the caller, so the technician page never
guesses which buttons to show. It shows what the server would allow.

**Why not a state-machine library?** Nine states and fifteen rows fit on one screen, can be tested exhaustively, and
need no new dependency. A library pays off with timers, nested states or persisted workflows, and we have none of
those.

## 2. The aggregate guards its invariants, in a fixed order

[WorkOrder.cs](../../src/platform-api/Modules/SmartFactory.Modules.Maintenance/Domain/WorkOrder.cs) is the only way to
change a work order. Each method does three things, always in this order:

1. **Authorise**: is this move in the table for the current state, and is this caller allowed to make it?
2. **Check invariants**: a reason for Reject, Hold or Cancel; every checklist item resolved before Complete; a
   disposition (Repaired, No fault found, Deferred) and a resolved checklist before Close.
3. **Apply**: change the state, raise `Revision` and append a history entry.

The order matters. The exhaustive test caught the first version checking the checklist *before* the state, so
"complete a work order that is only scheduled" answered "checklist incomplete" instead of "illegal transition".
The right answer to a wrong question is "you can't do that now", not a complaint about a missing detail.

## 3. Two kinds of authorisation

- **Role-based**, on the route: `/approve` needs the approver policy, `/schedule` needs the planner policy. This is
  cheap and runs before any database work. A technician calling `/approve` gets 403 without loading anything.
- **Resource-based**, in the domain: `/start` is open to the technician *role*, but only **the technician assigned to
  this work order** may use it. That depends on the row, so no role policy can express it. The domain answers 403
  (`not-assigned`).
- **Visibility.** A technician who is not also a planner sees only their own work orders. Another technician's id
  answers **404**, like another tenant's id, so the API doesn't confirm that someone else's job exists.

Assignment is by username (`preferred_username`). There is no user directory API yet, and a planner types the name.
A real deployment would pick from Keycloak's users through an admin API; that is out of scope for week 3.

## 4. Versioned edits: revision, history, audit, `xmin`

Every change touches four things in one transaction:

| What | Where | For whom |
|---|---|---|
| `Revision` +1 | `work_orders.revision` | People: "revision 7" is something you can say on the phone |
| History entry | `maintenance.work_order_history` | The timeline on the page |
| Audit record | `audit.audit_events` (hash-chained, week 2) | Compliance and tamper evidence |
| `xmin` changes | PostgreSQL system column | Concurrency: a second writer with the old version gets **409** |

**Why history *and* audit?** They answer different questions for different readers. The history belongs to the
Maintenance module and is shaped for its UI. The audit trail is the module-independent, tamper-evident record, and
it is readable only by managers and admins. Both are written in the same transaction through
[MaintenanceUnitOfWork.cs](../../src/platform-api/Modules/SmartFactory.Modules.Maintenance/Persistence/MaintenanceUnitOfWork.cs),
so they cannot disagree.

**Least privilege in the database.** The migration grants the application role only `SELECT, INSERT` on history,
findings and attachments, so evidence cannot be rewritten even by a bug. It grants no `DELETE` on work orders:
cancelled is a state, not a deletion. Tests run raw SQL as `sf_app` to prove it
([RowLevelSecurityTests.cs](../../src/platform-api/tests/SmartFactory.Platform.Api.Tests/Tenancy/RowLevelSecurityTests.cs)).

## 5. Asking another module, not reading its tables

Raising a request needs an asset that exists in the caller's tenant. Maintenance asks
[`IAssetDirectory`](../../src/building-blocks/SmartFactory.BuildingBlocks/Contracts/IAssetDirectory.cs), a two-line
contract in BuildingBlocks that the Assets module implements. Maintenance has no reference to the Assets assembly
(the architecture test still passes) and no SQL against `assets.*`. If Assets became its own service, only the
implementation behind the interface would change: an HTTP call instead of a query.

The work order keeps a *snapshot* of the asset code. Lists then need no call to Assets at all, and the code shown on
an old work order is the one that was true when it was raised.

## 6. Uploads: trust the bytes, nothing else

[MaintenanceEndpoints.cs](../../src/platform-api/Modules/SmartFactory.Modules.Maintenance/Endpoints/MaintenanceEndpoints.cs)
(`UploadAttachment`) and [FileSignatures.cs](../../src/platform-api/Modules/SmartFactory.Modules.Maintenance/Domain/FileSignatures.cs):

1. **Size first.** A request size limit on the route, then `file.Length` ≤ 5 MB, before reading anything.
2. **Magic bytes.** The first bytes must be JPEG (`FF D8 FF`), PNG (`89 50 4E 47 …`) or PDF (`%PDF-`). The file name
   and the declared `Content-Type` are ignored. A test uploads an executable named `invoice.pdf` with type
   `application/pdf` and gets **415**. Another uploads a real PDF declared as `text/html` and gets it stored as
   `application/pdf`.
3. **Generated storage keys** (`tenant/yyyy/MM/<guid>.pdf`). The user's file name is kept only for display, stripped
   of directories (`../../etc/passwd.pdf` becomes `passwd.pdf`). The local-disk adapter validates the key again and
   refuses anything that would leave its root.
4. **Served as a download** with the detected type, `Content-Disposition: attachment` and
   `X-Content-Type-Options: nosniff`, so a browser never reinterprets an upload as a page.

Bytes are written before the database transaction. If the transaction fails, the file is deleted again; the
reverse order would leave rows pointing at missing files.

`IAttachmentStore` is a **port**. [LocalDiskAttachmentStore](../../src/platform-api/Modules/SmartFactory.Modules.Maintenance/Storage/AttachmentStore.cs)
is the adapter for Compose and tests (a named volume keeps the files). Week 9 adds a Blob adapter, and nothing above
the interface changes.

## 7. Table-driven tests

[WorkOrderStateMachineTests.cs](../../src/platform-api/tests/SmartFactory.Platform.Api.Tests/Maintenance/WorkOrderStateMachineTests.cs)
writes the expected lifecycle out *independently* of the implementation. It then walks all 9 × 9 (state, action)
pairs: allowed pairs must land in the expected state, all others must throw `illegal-transition` and change
nothing. A second test asserts that the implementation's table and the expected table are identical. Adding a
state without deciding its transitions fails both. These are plain unit tests with no database, and they run in
milliseconds.

## 8. 360 px is a requirement with a test

The technician pages ([my-work.ts](../../src/web/src/app/features/work/my-work.ts),
[work-order.ts](../../src/web/src/app/features/work/work-order.ts)) are single-column:
- cards instead of tables;
- checklist outcomes as three equal buttons of at least 44 px;
- the file input opens the camera on a phone (`capture="environment"`).

The shell switches its navigation to an overlay below 720 px. Before that change, a 240 px side menu left a phone
120 px for content.

"Works at 360 px" is checked, not eyeballed. [`scripts/technician-360-check.cjs`](../../scripts/technician-360-check.cjs)
runs in CI against the real stack: the planner schedules a job through the API; the technician signs in through
Keycloak in a 360×740 Chromium and does the whole job. It fails if any page is wider than the viewport or a control
is under 44 px. Screenshots are kept as build artefacts.

## 9. Pitfalls met this week

- **Order of checks** (section 2): found by the exhaustive test on its first run.
- **Public test methods cannot take internal enums.** The theory data passes enum *names* as strings and parses
  them inside the test.
- **The OpenAPI document did not list 403, 409, 413 or 415.** A `ProblemHttpResult` does not advertise status codes
  the way typed results do. Without `.ProducesProblem(...)`, a client generated from the contract would not know
  that these errors exist.
- **Minimal APIs require an anti-forgery token for form uploads.** Anti-forgery protects cookie authentication;
  bearer tokens are never sent automatically by a browser, so the upload route disables it, with the reason written
  next to it.
- **Roles were in the access token but not the ID token.** The API reads the access token and worked. The web app
  reads the ID token to decide which menu items to show, so it saw no roles and hid "My work" from technicians.
  Unit tests mock the claims and could not notice; the 360 px end-to-end check could. Two tokens, two audiences:
  decide deliberately what goes into each.
- **`curl … | grep -q` under `set -o pipefail` is a race.** `grep -q` exits at the first match, curl then fails
  writing the rest, and pipefail turns that into a failure. It worked in week 2 and broke when `en.json` grew past
  one pipe write. Read first, then search.
- **The architecture tests listed DbContexts by hand.** A new module would have been silently unchecked. They now
  discover every module's DbContext.

## Interview answer (two sentences)

"Work orders follow an explicit transition table that the aggregate enforces: legality and the caller's right to act
first, then invariants such as 'closing needs a disposition', and every change bumps a revision and writes history
and a hash-chained audit record in the same transaction, with optimistic concurrency on top. Authorisation is
two-layered: role policies on routes and resource-based checks in the domain (only the assigned technician may
start the job), and uploads are trusted by their magic bytes, not by their name or declared type."

## Check yourself (answer at the end of the week)

1. Where does the rule "closing requires a disposition" live, and why not in the endpoint?
2. A planner and a technician press buttons on the same work order at the same moment. What happens, and which
   mechanism decides it?
3. Why is "only the assigned technician may start this work order" not something a role policy can express?
4. A user uploads `invoice.pdf` that is really an executable. Which check stops it, and what would happen if the
   server trusted the `Content-Type` header instead?
5. Why does Maintenance validate the asset through `IAssetDirectory` instead of a SQL join to `assets.assets`?
6. Why does another technician get 404, not 403, for a work order assigned to someone else?
7. What would break if the file were written *after* the database commit instead of before?

## What surprised me

*(Owner: add your own paragraph after the week.)*
