# FI-001: Outbound webhook delivery

> **Status:** ⏸️ Parked
> **Parked date:** 2026-06-24
> **Source:** [Production pattern harvest — wave 2](../production-harvest-second-app.md) (candidate A4)
> **Would-be target:** new `SolTechnology.Core.Webhooks` package (on top of `Core.HTTP`)
> **Semver / Effort:** MINOR (new package) · M

---

## Context — what it is

A small outbound-webhook subsystem harvested from a production payments service:

- **`IWebhookService.EnqueueWebhook<T>(...)`** — early-returns if the principal has no webhook
  configured (cheap no-op), stamps an execution timestamp, and dispatches via a
  **strategy-selected** sender.
- **HTTP sender** that:
  - signs the body with **HMAC-SHA256** (hex) in a signature header,
  - sets a webhook event-type header + user-agent,
  - applies a timeout,
  - supports **mTLS / certificate-fingerprint pinning**,
  - has a clean exception taxonomy — `WebhookResponseException` (non-2xx) vs
    `WebhookRequestException` (transport / timeout) — with a safe body-read helper.
- **`WebhookSenderSelector`** strategy that swaps between a direct-HTTP sender and a broker-backed
  sender based on configuration.
- A standalone **`IHmacSignatureService`** (reusable on its own; could live in the `SolTechnology.Core`
  foundation since it is a pure utility).

## Why it matters

"Send a signed callback to a customer URL with retries and a timeout" is a recurring need with many
sharp edges (signature scheme, SSRF/timeout, partial reads, mTLS). Core has none of it today.

## Why parked

- **No current consumer** in the Core ecosystem — nothing in the sample apps needs it yet.
- **New public surface + new NuGet package**, which warrants its own decision rather than riding in a
  multi-item hardening wave.
- Cleanly separable: parking it does not block any roadmap item in
  [harvest wave 2](../production-harvest-second-app.md#prioritised-roadmap-proposed).

## Sketch (when picked up)

- New package **`SolTechnology.Core.Webhooks`** building on `Core.HTTP` for resilience + correlation
  (reuse the existing retry / circuit-breaker / bounded-body machinery instead of re-rolling it).
- Lift `IHmacSignatureService` into the foundation (`SolTechnology.Core`) as a pure utility.
- 🟢 Ship the **sender + signature** path as code.
- 🔵 Treat the **broker-backed sender** as a documented recipe rather than shipped code, unless a
  consumer needs the async path.

## Guard-rails

- Outbound URL is **attacker-influenced** → enforce a timeout + a **bounded** response read
  (reuse the `Core.HTTP` body-cap); treat as an SSRF surface.
- **Never log the signing secret.**
- Signature scheme + header names must be a documented, stable contract.

## Graduation triggers

Promote to an ADR when **any** of these hold:

- A Core consumer (sample app or downstream service) needs to deliver signed callbacks.
- We want a reusable HMAC utility in the foundation independently of webhooks.
- A platform requirement (e.g. partner integrations) makes signed outbound callbacks first-class.

## Related

- [Harvest wave 2 — A4](../production-harvest-second-app.md) — original assessment.
- [`SolTechnology.Core.HTTP`](../Clients.md) — resilience + correlation foundation to build on.
- [ADR index](../adr/README.md) — where this graduates to.

