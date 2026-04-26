# Project Idea: **PulseLog** — A Mini Incident Reporting API

Given your background in Clean Architecture and VSA, I want to give you a project that's *simple enough to build in a weekend* but *complex enough to force you into every meaningful logging scenario*.

---

## What Is It?

A backend API for an **internal IT incident reporting system**. Employees report technical incidents (e.g., "server is down", "login broken"), support agents update them, and the system notifies via email. Simple domain, but it has everything you need.

---

## Why This Project Specifically?

Every feature maps to a distinct logging challenge:

| Feature | Logging scenario it forces |
|---|---|
| User auth (JWT) | Security event logging |
| Submit incident | Business event + structured properties |
| Background email notifications | Background job logging |
| External email service call | External dependency + resilience logging |
| Admin dashboard query (slow) | Performance logging |
| Global exception handler | Error/Fatal + exception logging |
| Health check endpoint | Filtering noise from logs |
| Multi-step incident workflow | Correlation ID across a full flow |
| Destructuring an `Incident` object | Sensitive data masking |

---

## Domain

```
Incident
├── Id (Guid)
├── Title (string)
├── Description (string)
├── ReportedBy (UserId)
├── AssignedTo (UserId, nullable)
├── Priority (Low / Medium / High / Critical)
├── Status (Open / InProgress / Resolved / Closed)
├── CreatedAt
└── ResolvedAt (nullable)

User
├── Id (Guid)
├── Email               ← sensitive — must NOT appear in logs raw
├── FullName            ← sensitive
└── Role (Reporter / Agent / Admin)

AuditEntry
├── EntityName
├── Action (Created / Updated / StatusChanged)
├── PerformedBy (UserId)
└── Timestamp
```

---

## API Endpoints

```
POST   /auth/login                  → returns JWT
POST   /incidents                   → reporter submits incident
GET    /incidents/{id}              → get by ID
PUT    /incidents/{id}/assign       → agent assigns to themselves
PUT    /incidents/{id}/status       → agent updates status
GET    /incidents?priority=High     → filtered list (potential slow query)
GET    /health                      → health check (should NOT spam logs)
```

---

## Logging Scenarios You Must Cover — One by One

This is the real spec. Build each feature, then **deliberately** implement its logging requirement.

---

### Scenario 1 — Startup & Lifecycle
**Trigger:** App boots up

```
✔ Log app version, environment name, and machine name on startup
✔ Log Fatal + flush if startup fails (e.g., DB unreachable)
✔ Use bootstrap logger pattern
```

---

### Scenario 2 — Request Pipeline Logging
**Trigger:** Every HTTP request

```
✔ UseSerilogRequestLogging with custom template
✔ Enrich with UserId from JWT claims (if authenticated)
✔ Enrich with RequestId
✔ Suppress /health endpoint from logs entirely
```

---

### Scenario 3 — Correlation ID Middleware
**Trigger:** Every request

```
✔ Read X-Correlation-ID from header (or generate a new one)
✔ Push to LogContext so EVERY log line in that request carries it
✔ Return the ID in response headers
✔ Test: submit an incident, observe all log lines share the same CorrelationId
```

---

### Scenario 4 — Security Event Logging
**Trigger:** /auth/login

```
✔ Log successful login: UserId, Role (NOT email in plain form)
✔ Log failed login: attempted email (hashed or masked), IP address, reason
✔ Log suspicious pattern: 3+ failed attempts → Warning level
✔ NEVER log the raw password or JWT token
```

---

### Scenario 5 — Business Event Logging
**Trigger:** Incident lifecycle

```
✔ Incident created: log IncidentId, Priority, ReportedBy (UserId only)
✔ Incident assigned: log IncidentId, AssignedTo, AssignedBy
✔ Status changed: log IncidentId, OldStatus → NewStatus, ChangedBy
✔ Each log at Information level with meaningful named properties
```

---

### Scenario 6 — Sensitive Data Masking via Destructuring Policy
**Trigger:** Anywhere a User or Incident object might be logged with @

```
✔ Build a destructuring policy that masks User.Email → "u***@***.com"
✔ Masks User.FullName → "[REDACTED]"
✔ Test by logging {@User} and confirming the sink never receives raw PII
```

---

### Scenario 7 — Performance Logging
**Trigger:** GET /incidents (filtered list query)

```
✔ Measure EF Core query duration
✔ If elapsed > 500ms → LogWarning with query params and duration
✔ If elapsed > 2000ms → LogError (SLA breach)
✔ Use Stopwatch + LoggerMessage source generator for this hot path
```

---

### Scenario 8 — External Dependency Logging
**Trigger:** Email notification via a fake SMTP service (use MailKit or a stub)

```
✔ Log before the call: gateway name, recipient ID (not email), incident ID
✔ Log after: success + duration
✔ Log on failure: exception, retry attempt number, next retry delay
✔ Use Polly for retry — log each retry as a Warning
✔ After all retries exhausted → LogError, do NOT crash the app
```

---

### Scenario 9 — Background Job Logging
**Trigger:** A Hangfire (or just IHostedService) job that checks for unassigned incidents every 5 minutes

```
✔ Log job start with timestamp and job name
✔ Log how many unassigned incidents were found
✔ Log each notification dispatched (IncidentId only)
✔ Log job completion with duration
✔ Ensure CorrelationId is generated per job run (not inherited from HTTP)
```

---

### Scenario 10 — Global Exception Handler
**Trigger:** Any unhandled exception

```
✔ UseExceptionHandler middleware catches everything
✔ LogError(exception, ...) with CorrelationId, UserId if available, endpoint
✔ Return a safe error response (NEVER leak stack traces to client)
✔ Test: throw a deliberate unhandled exception and verify the log
```

---

### Scenario 11 — Audit Logging (Separate Concern)
**Trigger:** Any write operation (create/update incident)

```
✔ Intercept via EF Core SaveChanges
✔ Write AuditEntry to DB (not to Serilog — this is structured DB audit, not app log)
✔ But ALSO emit a Serilog event: "Audit: {Action} on {Entity} by {UserId}"
✔ Understand the difference: audit log = compliance record, app log = ops visibility
```

---

## Architecture Recommendation

Use **Vertical Slice Architecture** since you're comfortable with it. One slice per use case:

```
Features/
├── Auth/
│   └── Login/
├── Incidents/
│   ├── Create/
│   ├── Assign/
│   ├── UpdateStatus/
│   └── GetFiltered/
├── Notifications/
│   └── SendIncidentEmail/
└── Background/
    └── UnassignedIncidentScan/
```

Each slice has its own handler — and its own logging responsibility.

---

## Suggested Build Order

```
Week 1
  Day 1-2 → Domain + EF Core setup + Scenarios 1, 2, 3
  Day 3   → Auth endpoint + Scenario 4
  Day 4-5 → Incident CRUD + Scenarios 5, 6

Week 2
  Day 1   → Performance logging + Scenario 7
  Day 2-3 → Email stub + Polly + Scenario 8
  Day 4   → Background job + Scenario 9
  Day 5   → Global handler + Audit + Scenarios 10, 11
```

---

## What You'll Have At The End

- A production-pattern logging setup you can copy into any real project
- Hands-on experience with every logging level used *correctly*
- A destructuring policy you built yourself
- Correlation ID tracing end-to-end
- Clear intuition for the boundary between **app logs**, **audit logs**, and **error telemetry**

---

Want me to scaffold the `Program.cs`, the middleware, and the first slice to get you started?