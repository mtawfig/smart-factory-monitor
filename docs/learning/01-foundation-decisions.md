# 01 — Foundation decisions

**Week 0.** No code yet; only decisions. This note explains the six decisions that shape everything else and
the reasoning a senior engineer would expect you to give for each.

## Concepts you will meet

modular monolith · bounded context · fitness function · LTS vs STS · transactional outbox · at-least-once
delivery · idempotency · Kafka API · key-value vs document NoSQL · OT/IT boundary · architecture decision record

---

## 1. Why a modular monolith plus three services, not six microservices

**The decision (ADR-002).** Business logic lives in one ASP.NET Core process with four internal modules
(Assets, Maintenance, Inventory, Audit/Notifications). Three more processes exist because they are genuinely
different: the Ingest Worker (high-throughput, no HTTP), the ML Service (Python, different release cadence),
the Simulator (fake plant, must be deletable).

**The reasoning.** Microservices buy *independent deployability* and *independent scaling* at the price of
network calls, distributed data, duplicated plumbing and operational load. A solo developer with 240 hours
pays the full price and collects almost none of the benefit: the four business modules deploy together
anyway and scale together anyway. What matters for a future split is not the process boundary but the
**data and contract boundaries**. So we build those now:

- one database **schema per module** (ADR-007), each with its own EF Core `DbContext`;
- modules may call each other only through a small public interface or an event, never through another
  module's tables or internal classes;
- an **architecture test** (a *fitness function*) fails the build if someone breaks the rule.

When a module needs its own process, you move its schema and its contracts; the code barely changes.

**The split criterion.** Extract a module when it has a different scaling profile, a different release
cadence, a different team, or a different runtime. The Ingest Worker and the ML Service satisfy that today;
Inventory does not.

**Interview answer.** "I started with a modular monolith with enforced module boundaries and extracted only the
components with a real reason to be separate. The seams are already in the schema and the contracts, so a
later split is a move, not a rewrite."

**Read more.** Sam Newman, *Monolith to Microservices*; Neal Ford et al., *Building Evolutionary Architectures*
(fitness functions).

---

## 2. Why .NET 10, and why "LTS only" is a policy, not a preference

**The decision (ADR-010).** The blueprint said .NET 8. The question was whether .NET 9 is better. Checking
Microsoft's lifecycle page gave the real answer: .NET 8 (LTS) and .NET 9 (STS) **both** leave support on
10 November 2026; .NET 10 is LTS until November 2028.

**The reasoning.** A runtime out of support receives no security patches. Any security reviewer, auditor or
hiring manager who runs `dotnet --version` on your demo and sees an unsupported runtime has learned something
about your judgement. The version number itself is nearly irrelevant; the **support window relative to your
delivery date** is what matters. Rule of thumb: pick the newest LTS whose support outlives your project by at
least a year, and verify the hosting platform supports it.

**What changes in practice.** `global.json` pins the SDK, `Directory.Build.props` sets `net10.0` once, CI uses
the `10.0.x` channel, and every NuGet package is checked for .NET 10 support in Week 1. Expect C# 14 features
(field-backed properties, extension members) and EF Core 10 (LeftJoin/RightJoin operators, improved JSON
mapping) to appear in the code.

**Interview answer.** "I treat runtime support windows as a security requirement. I target LTS releases and
record the end-of-support date in an ADR so the decision is auditable."

---

## 3. The transactional outbox: how to publish an event without lying

**The problem.** A work order is approved. You write the row to PostgreSQL *and* publish `WorkOrderApproved`
to the bus. If the process dies between the two, either the database says "approved" and nobody heard, or the
world heard and the database rolled back. This is the **dual-write problem**, and no amount of try/catch fixes it.

**The pattern (ADR-003).** Write the event into an `outbox_messages` table **in the same database transaction**
as the state change. A separate relay reads unsent rows and publishes them, marking each as sent. Now the
state change and the event either both exist or neither does.

**The cost.** The relay may crash after publishing but before marking the row sent, so the event can be
published **twice**. This is why the bus is **at-least-once** and every consumer must be **idempotent**.

**Idempotent consumer.** Each event carries an `event_id` (UUID v7, time-ordered so indexes stay friendly).
The consumer checks "have I processed this id?" before acting: a Redis set with a 10-minute TTL for the hot
path, backed by a processed-events table where correctness matters more than speed. A message that fails
repeatedly goes to a **dead-letter topic** instead of blocking the partition.

**Interview answer.** "I use the outbox pattern because dual writes cannot be made atomic. The trade-off is
at-least-once delivery, so every consumer deduplicates on event id and poison messages go to a dead-letter
topic."

**Read more.** Chris Richardson, microservices.io "Transactional outbox"; Martin Kleppmann, *Designing
Data-Intensive Applications*, chapter 11.

---

## 4. Kafka API without running Kafka

**The decision (ADR-003).** The code speaks the **Kafka protocol** through standard clients
(Confluent.Kafka in .NET, confluent-kafka in Python). Locally the broker is **Redpanda**, a single binary
that implements the Kafka API with no ZooKeeper and a small footprint. In Azure the broker is **Event Hubs
Standard**, which exposes a Kafka endpoint.

**Why it matters for learning.** You learn the real Kafka concepts (topics, partitions, consumer groups,
offsets, keys, retention, replay) on the real protocol, and you learn the cloud lesson that *the API is the
contract, not the vendor*. Partitioning by `asset_id` keeps each asset's events ordered; consumer groups let
the ML Service and the Platform API each read the same topic independently; offsets let you **replay** a day
of telemetry through a new model version, which is how the Week 8 evaluation report is produced.

**What you will not learn here.** Operating a multi-broker cluster, ISR tuning, Kafka Streams, Schema
Registry. Say so if asked.

**Interview answer.** "I built against the Kafka API with Redpanda locally and Event Hubs' Kafka endpoint in
Azure. Same client code, same partitioning and consumer-group semantics, no cluster to operate."

---

## 5. "Does this project cover NoSQL?" — the honest answer

NoSQL is a family, not a product. This project uses two members and deliberately skips a third:

| Family | Example | In this project |
|---|---|---|
| **Key-value** | Redis | **Yes.** Last value per asset, dedup sets with TTL, rate limits. You will design keys, choose TTLs, decide what happens when Redis is empty (answer: nothing breaks, latency rises), and test that. |
| **Object / columnar files** | Blob Storage + parquet | **Yes.** Bronze/silver/gold datasets for ML. You will learn why columnar beats row storage for analytics and how a medallion layout keeps raw data immutable. |
| **Semi-structured in a relational DB** | PostgreSQL JSONB | **Yes.** Alert explanations and model metadata are JSONB columns with GIN indexes. This is how many teams avoid a document store entirely. |
| **Document** | MongoDB, Cosmos DB | **No, by design.** Nothing in the domain needs it, and adding a second database for résumé reasons is a decision a reviewer would question. The blueprint lists Cosmos DB as a Release 2 item for cloud breadth. |

**The lesson behind the table.** Pick storage by **access pattern**: point lookups by key → key-value;
time-ordered append with range scans → time-series; relational integrity and transactions → relational;
analytical scans over many rows → columnar. Being able to say *why not* a technology is worth more than
having used it once.

**Interview answer.** "I used Redis as a key-value store and parquet in Blob for analytics, and kept
semi-structured data in PostgreSQL JSONB. I did not add a document database because no access pattern needed
one. I can explain when I would."

---

## 6. The read-only OT boundary

**The decision (ADR-004).** Telemetry flows one way, from plant to platform, over MQTT. There is no code path
that writes to a machine. The only message that goes back is to the *simulator*, to reset a repaired asset.

**Why a software architect cares.** In industrial systems, the OT network (PLCs, sensors) and the IT network
(your services) are separated for safety. A system that can command machinery falls under functional-safety
and cyber-security regimes (IEC 62443) that a portfolio project cannot meet. Designing the boundary as
read-only is not a limitation; it is the correct posture, and saying so shows domain awareness.

**How it shows up in code.** The Ingest Worker only *subscribes*. An architecture test asserts that no
component other than the simulator publishes to `plant/#`. The UI carries an "advisory only" label.

**Interview answer.** "The platform is advisory by construction: a one-way MQTT boundary, no write path to
equipment, and a test that proves it."

---

## 7. ADRs: why we write decisions down

An **architecture decision record** is a one-page file with context, decision and consequences. We write one
whenever a choice is expensive to reverse. The value is not the document; it is that six months later you
(or an interviewer) can see *what you knew when you chose*. ADR-010 is a good example: the context section
records the exact support dates that drove the choice, so when .NET 12 appears nobody has to guess.

Rule: an ADR is never edited in substance. If the decision changes, a new ADR supersedes the old one.

---

## Check yourself

1. A colleague says "microservices are more scalable, we should split Inventory out now." What two questions
   do you ask before agreeing?
2. Why can the outbox relay publish the same event twice, and what makes that harmless?
3. Redis is wiped during a demo. What breaks? What degrades?
4. Explain, without naming a product, how you would choose between a key-value store, a document store and a
   relational database for a new feature.
5. Why is .NET 9 the wrong answer to "which .NET is newer and better supported?" today?

## What surprised me

*(Your paragraph.)*
