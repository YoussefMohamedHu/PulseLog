# Software Requirements Document (SRD)
## PulseLog — Internal IT Incident Reporting System

---

### Document Control

| Property | Value |
|---|---|
| **Document ID** | SRD-PL-001 |
| **Version** | 1.0 |
| **Status** | Draft |
| **Classification** | Internal — Confidential |
| **Date** | 2026-04-26 |
| **Author** | Architecture Team |

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [System Overview](#2-system-overview)
3. [Functional Requirements](#3-functional-requirements)
4. [Non-Functional Requirements](#4-non-functional-requirements)
5. [Detailed Use Cases](#5-detailed-use-cases)
6. [Logging Scenarios Specification](#6-logging-scenarios-specification)
7. [Domain Model](#7-domain-model)
8. [API Contract](#8-api-contract)
9. [Architecture Constraints](#9-architecture-constraints)
10. [Acceptance Criteria](#10-acceptance-criteria)

---

## 1. Introduction

### 1.1 Purpose

PulseLog is a backend API system enabling internal IT incident reporting, assignment, tracking, and resolution. The system supports reporters submitting technical issues, support agents managing the incident lifecycle, and administrators monitoring system health.

### 1.2 Scope

This document covers the complete backend API. Email notifications are simulated/stubbed. The frontend UI is out of scope.

### 1.3 Document Conventions

| Keyword | Meaning |
|---|---|
| **MUST** | Mandatory requirement |
| **SHOULD** | Recommended but not mandatory |
| **MAY** | Optional |
| **SHALL** | Equivalent to MUST |

### 1.4 Stakeholders

| Role | Interest |
|---|---|
| Reporter | Submitting and tracking own incidents |
| Support Agent | Claiming, investigating, and resolving incidents |
| Admin | System oversight, auditing, dashboard queries |
| DevOps/SRE | Monitoring, log analysis, alerting |
| Security Team | Audit trail, access control compliance |
| Compliance Officer | Audit record retention and PII protection |

---

## 2. System Overview

### 2.1 System Context Diagram

```
┌──────────┐     ┌──────────────────┐     ┌──────────────────┐
│  Client  │────▶│   PulseLog API   │────▶│   SQL Database   │
│  (HTTP)  │     │   (ASP.NET 8)    │     │   (PostgreSQL)   │
└──────────┘     └────────┬─────────┘     └──────────────────┘
                          │
                          │ (stub/fake)
                          ▼
                   ┌──────────────────┐
                   │  Email Provider  │
                   │  (SMTP Stub)     │
                   └──────────────────┘
```

### 2.2 Key Capabilities

- JWT-based authentication with role-based access control
- Full incident lifecycle management (create → assign → progress → resolve → close)
- Background scanning for unassigned incidents with targeted notifications
- External email notification with resilience (retry/failover)
- Complete audit trail of all write operations (database level)
- Structured application logging covering security, business, performance, error, and lifecycle events
- Correlation ID tracing across synchronous and background execution contexts

### 2.3 User Roles

| Role | Permissions |
|---|---|
| **Reporter** | Submit incidents; view own incidents |
| **Agent** | View all incidents; assign to self; update status; view audit trail |
| **Admin** | All agent permissions; view system audit trail; access filtered dashboard queries |

---

## 3. Functional Requirements

### FR-1: Authentication

| ID | Requirement | Priority |
|---|---|---|
| **FR-1.1** | System MUST accept email and password, returning a signed JWT on successful authentication | P0 |
| **FR-1.2** | System MUST reject invalid credentials with a generic "Invalid email or password" response (no user enumeration) | P0 |
| **FR-1.3** | System MUST include UserId and Role as claims in the JWT | P0 |
| **FR-1.4** | System MUST set JWT expiration to 8 hours | P1 |
| **FR-1.5** | System MUST detect and log suspicious patterns (3+ consecutive failed attempts for same email within 5 minutes) | P1 |

### FR-2: Incident Submission

| ID | Requirement | Priority |
|---|---|---|
| **FR-2.1** | System MUST allow authenticated reporters to create incidents with Title, Description, and Priority | P0 |
| **FR-2.2** | System MUST set initial Status to "Open" and populate ReportedBy from the JWT | P0 |
| **FR-2.3** | System MUST return the created IncidentId and all properties on success | P0 |
| **FR-2.4** | System MUST validate Title (3–200 characters) and Description (10–4000 characters) | P0 |
| **FR-2.5** | System MUST notify all agents via email when a Critical-priority incident is created | P1 |

### FR-3: Incident Retrieval

| ID | Requirement | Priority |
|---|---|---|
| **FR-3.1** | System MUST allow authenticated users to retrieve a specific incident by ID | P0 |
| **FR-3.2** | Reporters SHALL only retrieve their own incidents | P0 |
| **FR-3.3** | Agents and Admins SHALL retrieve any incident | P0 |
| **FR-3.4** | System MUST support filtering by Priority and/or Status via query parameters | P1 |
| **FR-3.5** | System MUST paginate results (default 20 items, maximum 100) | P1 |

### FR-4: Incident Assignment

| ID | Requirement | Priority |
|---|---|---|
| **FR-4.1** | System MUST allow Agents and Admins to assign an incident to themselves | P0 |
| **FR-4.2** | System MUST reject assignment of an already-assigned incident (status conflict) | P1 |
| **FR-4.3** | System MUST reject assignment by Reporters | P0 |
| **FR-4.4** | Assignment MUST update Status to "InProgress" automatically | P1 |
| **FR-4.5** | System MUST send email notification to the Reporter when their incident is assigned | P1 |

### FR-5: Status Update

| ID | Requirement | Priority |
|---|---|---|
| **FR-5.1** | System MUST allow Agents/Admins to update the Status of an assigned incident | P0 |
| **FR-5.2** | Valid status transitions: Open→InProgress, InProgress→Resolved, Resolved→Closed, Any→Open (reopen) | P0 |
| **FR-5.3** | System MUST reject invalid transitions with appropriate error | P0 |
| **FR-5.4** | System MUST set ResolvedAt timestamp when Status transitions to "Resolved" | P0 |
| **FR-5.5** | System MUST send email notification to Reporter on Status change | P1 |

### FR-6: Health Endpoint

| ID | Requirement | Priority |
|---|---|---|
| **FR-6.1** | System MUST expose a GET /health endpoint returning 200 OK when DB and services are available | P1 |
| **FR-6.2** | System MUST NOT generate application logs for health check requests | P1 |

### FR-7: Background Job — Unassigned Incident Scan

| ID | Requirement | Priority |
|---|---|---|
| **FR-7.1** | System MUST execute a background job every 5 minutes | P1 |
| **FR-7.2** | Job MUST query for all Open and unassigned High/Critical incidents older than 15 minutes | P1 |
| **FR-7.3** | Job MUST notify all agents for each qualifying incident | P1 |
| **FR-7.4** | Job MUST generate its own Correlation ID per execution (independent from HTTP requests) | P1 |

### FR-8: Audit Trail

| ID | Requirement | Priority |
|---|---|---|
| **FR-8.1** | System MUST persist an AuditEntry record for every create, assign, and status-update operation | P0 |
| **FR-8.2** | AuditEntry MUST specify EntityName, Action, PerformedBy (UserId), and Timestamp | P0 |
| **FR-8.3** | System MUST write audit records within the same database transaction as the domain change | P0 |
| **FR-8.4** | Admin users SHALL be able to retrieve the full audit trail | P1 |

---

## 4. Non-Functional Requirements

### NFR-1: Logging

| ID | Requirement | Priority |
|---|---|---|
| **NFR-1.1** | System MUST produce structured JSON logs with named properties (never string interpolation) | P0 |
| **NFR-1.2** | Every log event MUST carry a CorrelationId for request tracing | P0 |
| **NFR-1.3** | System MUST mask/redact sensitive PII (email, full name) via destructuring policy | P0 |
| **NFR-1.4** | System MUST NOT log raw passwords, JWT tokens, or connection strings | P0 |
| **NFR-1.5** | System MUST use LogLevel hierarchy correctly: Debug, Information, Warning, Error, Fatal | P0 |
| **NFR-1.6** | Health check endpoint traffic MUST be excluded from application logging | P1 |
| **NFR-1.7** | System MUST log app version, environment, and machine name on successful startup | P0 |
| **NFR-1.8** | System MUST log Fatal and flush logs if startup fails (critical dependency unavailable) | P0 |

### NFR-2: Performance

| ID | Requirement | Priority |
|---|---|---|
| **NFR-2.1** | Filtered list queries (GET /incidents) MUST log Warning if duration exceeds 500ms | P1 |
| **NFR-2.2** | Filtered list queries MUST log Error if duration exceeds 2000ms (SLA breach) | P1 |
| **NFR-2.3** | External email calls MUST complete or fail within 10 seconds (timeout) | P1 |

### NFR-3: Resilience

| ID | Requirement | Priority |
|---|---|---|
| **NFR-3.1** | Email notification MUST retry up to 3 times with exponential backoff (1s, 2s, 4s) | P1 |
| **NFR-3.2** | Email failure after all retries MUST log Error and continue (never crash the calling context) | P1 |
| **NFR-3.3** | Each retry attempt MUST emit a Warning log with attempt number and next delay | P1 |

### NFR-4: Security

| ID | Requirement | Priority |
|---|---|---|
| **NFR-4.1** | Error responses to clients MUST NEVER expose stack traces, internal paths, or raw exceptions | P0 |
| **NFR-4.2** | All endpoints except /health and /auth/login MUST require a valid JWT | P0 |
| **NFR-4.3** | Sensitive data (User.Email, User.FullName) MUST be masked in logs via destructuring policy | P0 |

### NFR-5: Observability

| ID | Requirement | Priority |
|---|---|---|
| **NFR-5.1** | Every HTTP request log MUST include RequestId, UserId (if authenticated), HTTP method, path, status code, and elapsed ms | P0 |
| **NFR-5.2** | The CorrelationId MUST be returned to the caller via X-Correlation-ID response header | P0 |
| **NFR-5.3** | The CorrelationId MUST be read from X-Correlation-ID request header if provided; otherwise, a new one MUST be generated | P0 |

---

## 5. Detailed Use Cases

### UC-1: Reporter Submits Incident

| Field | Detail |
|---|---|
| **Actor** | Reporter (authenticated) |
| **Precondition** | Valid JWT with Role = Reporter |
| **Trigger** | Reporter fills out incident form and submits |

**Main Flow:**
1. Reporter POSTs to `/incidents` with Title, Description, Priority
2. System validates input
3. System creates Incident with Status=Open, ReportedBy=current UserId, CreatedAt=UtcNow
4. System persists Incident and AuditEntry in a single transaction
5. System logs "IncidentCreated" with IncidentId, Priority, ReportedBy
6. If Priority == Critical, system enqueues agent notification
7. System returns 201 Created with full Incident JSON

**Alternative Flows:**
- **A1 (Validation fails):** System returns 400 with field-level errors. Logs Warning.
- **A2 (Critical incident):** Background notification is queued; response is not delayed.

**Postcondition:** Incident exists in DB with Status=Open. Audit trail updated.

**Logging Events:**
| Event | Level | Properties |
|---|---|---|
| IncidentCreated | Information | IncidentId, Priority, ReportedBy, CorrelationId |
| CriticalIncidentCreated | Warning | IncidentId, Priority, CorrelationId |
| ValidationFailed | Warning | Errors, Endpoint, CorrelationId |

---

### UC-2: Agent Authenticates

| Field | Detail |
|---|---|
| **Actor** | Unauthenticated user |
| **Precondition** | User account exists with known role |
| **Trigger** | POST to `/auth/login` |

**Main Flow:**
1. System receives email + password
2. System hashes password and compares with stored hash
3. Match found → System generates JWT with UserId, Role claims
4. System logs successful login (UserId, Role — NOT raw email)
5. System returns 200 with JWT token

**Alternative Flows:**
- **A1 (Invalid credentials):** System logs hashed email, IP, failure reason. Returns 401.
- **A2 (3+ recent failures):** System logs Warning "SuspiciousLoginAttempts". Still returns 401.
- **A3 (Account locked):** System logs Warning. Returns 403.

**Postcondition:** User receives valid JWT or generic error.

**Logging Events:**
| Event | Level | Properties |
|---|---|---|
| LoginSucceeded | Information | UserId, Role, CorrelationId |
| LoginFailed | Warning | EmailHash, IPAddress, FailureReason, CorrelationId |
| SuspiciousLoginPattern | Warning | EmailHash, AttemptCount, TimeWindow, CorrelationId |

**Sensitive Data Rules:**
- Password: NEVER logged
- JWT: NEVER logged
- Email: Hashed (SHA256) or masked for log context

---

### UC-3: Agent Assigns Incident to Self

| Field | Detail |
|---|---|
| **Actor** | Agent or Admin |
| **Precondition** | Incident exists with Status=Open and AssignedTo=null |
| **Trigger** | Agent PUTs to `/incidents/{id}/assign` |

**Main Flow:**
1. System validates JWT role (Agent or Admin)
2. System loads incident, confirms it is Open and unassigned
3. System sets AssignedTo=current UserId, Status=InProgress
4. System persists Incident and AuditEntry
5. System logs "IncidentAssigned" with IncidentId, AssignedTo, AssignedBy
6. System enqueues email notification to Reporter
7. System returns 200 with updated Incident

**Alternative Flows:**
- **A1 (Already assigned):** System returns 409 Conflict. Logs Warning.
- **A2 (Not found):** System returns 404. Logs Information.
- **A3 (Reporter role):** System returns 403. Logs Warning "UnauthorizedAssignmentAttempt".

**Postcondition:** Incident assigned, Status=InProgress. Reporter notified via email stub.

**Logging Events:**
| Event | Level | Properties |
|---|---|---|
| IncidentAssigned | Information | IncidentId, AssignedTo, AssignedBy, CorrelationId |
| AssignmentConflict | Warning | IncidentId, CurrentAssignee, CorrelationId |
| UnauthorizedAssignmentAttempt | Warning | UserId, Role, IncidentId, CorrelationId |

---

### UC-4: Agent Updates Incident Status

| Field | Detail |
|---|---|
| **Actor** | Agent or Admin |
| **Precondition** | Incident assigned to the agent, or agent is Admin |
| **Trigger** | PUT to `/incidents/{id}/status` with new Status |

**Main Flow:**
1. System validates JWT role, loads incident
2. System validates the state transition is allowed
3. System updates Status; if new Status == Resolved, sets ResolvedAt=UtcNow
4. System persists and creates AuditEntry
5. System logs "IncidentStatusChanged" with IncidentId, OldStatus, NewStatus, ChangedBy
6. System enqueues email notification to Reporter
7. Returns 200

**Alternative Flows:**
- **A1 (Invalid transition):** System returns 400. Logs Warning with attempted transition.
- **A2 (Not assigned to this agent):** Returns 403 (admin exempt). Logs Warning.

**Valid State Machine:**
```
Open ──────▶ InProgress ──────▶ Resolved ──────▶ Closed
  ▲              │                    │              │
  │              │                    │              │
  └──────────────┴────────────────────┴──────────────┘
           (reopen — any state → Open)
```

**Logging Events:**
| Event | Level | Properties |
|---|---|---|
| IncidentStatusChanged | Information | IncidentId, OldStatus, NewStatus, ChangedBy, CorrelationId |
| InvalidStatusTransition | Warning | IncidentId, From, To, AttemptedBy, CorrelationId |

---

### UC-5: Admin Queries High-Priority Incidents (Slow Query)

| Field | Detail |
|---|---|
| **Actor** | Admin |
| **Precondition** | Large dataset of incidents |
| **Trigger** | GET `/incidents?priority=Critical&status=Open` |

**Main Flow:**
1. System authenticates Admin
2. System executes filtered EF Core query
3. System measures elapsed time via Stopwatch
4. If < 500ms: returns 200 with paginated results. Logs (via request pipeline only — no extra app log).
5. If 500–2000ms: returns 200. **Additionally** logs Warning with query parameters and duration.
6. If > 2000ms: returns 200. **Additionally** logs Error for SLA breach.

**Logging Events:**
| Event | Level | Properties |
|---|---|---|
| SlowQuery | Warning | Endpoint, QueryParams, DurationMs, CorrelationId |
| SlaBreachQuery | Error | Endpoint, QueryParams, DurationMs, CorrelationId |

---

### UC-6: Background Job — Unassigned Incident Alert

| Field | Detail |
|---|---|
| **Actor** | System (IHostedService / Hangfire) |
| **Precondition** | Job triggered every 5 minutes |
| **Trigger** | Timer fires |

**Main Flow:**
1. Job instantiates a new CorrelationId
2. Job logs "UnassignedIncidentScanStarted" with JobName, CorrelationId, Timestamp
3. Job queries DB for Open, unassigned, High/Critical incidents older than 15 minutes
4. Job logs "UnassignedIncidentsFound" with Count and CorrelationId
5. For each incident, job calls email service stub
6. For each notification, job logs "NotificationDispatched" with IncidentId (NOT recipient email)
7. Job logs "UnassignedIncidentScanCompleted" with TotalIncidents, DurationMs, CorrelationId

**Alternative Flows:**
- **A1 (No unassigned incidents):** Job logs Information "NoUnassignedIncidentsFound". Completes.
- **A2 (Email failure):** Follows resilience policy (UC-7). Job continues to next incident.

**Logging Events:**
| Event | Level | Properties |
|---|---|---|
| JobStarted | Information | JobName, CorrelationId |
| UnassignedIncidentsFound | Information | Count, CorrelationId |
| NotificationDispatched | Information | IncidentId, CorrelationId |
| JobCompleted | Information | JobName, TotalIncidents, DurationMs, CorrelationId |

---

### UC-7: External Email Notification (Resilience)

| Field | Detail |
|---|---|
| **Actor** | Any calling context (HTTP handler, background job) |
| **Precondition** | Email notification required |
| **Trigger** | System calls email service |

**Main Flow:**
1. System logs "EmailSending" with gateway name, recipient UserId (not email), IncidentId
2. System calls SMTP stub via Polly policy (3 retries, 1s/2s/4s backoff, 10s timeout)
3. Call succeeds
4. System logs "EmailSent" with duration, IncidentId
5. Calling context continues

**Alternative Flows:**
- **A1 (First attempt fails):** Logs Warning "EmailRetryAttempt" with attempt #, delay. Retries.
- **A2 (Second attempt fails):** Logs Warning again. Retries.
- **A3 (All attempts exhausted):** Logs Error "EmailFailedAfterRetries" with IncidentId, exception details. Does NOT throw. Calling context continues.

**Logging Events:**
| Event | Level | Properties |
|---|---|---|
| EmailSending | Information | Gateway, RecipientUserId, IncidentId, CorrelationId |
| EmailSent | Information | IncidentId, DurationMs, CorrelationId |
| EmailRetryAttempt | Warning | Attempt, NextDelayMs, Exception, IncidentId, CorrelationId |
| EmailFailedAfterRetries | Error | IncidentId, TotalAttempts, FinalException, CorrelationId |

**Constraint:** Recipient email address MUST NOT be logged. Use UserId instead.

---

### UC-8: Unhandled Exception

| Field | Detail |
|---|---|
| **Actor** | Any HTTP request |
| **Precondition** | Unhandled exception thrown in application |
| **Trigger** | Exception bubbles to global handler |

**Main Flow:**
1. Global exception middleware catches exception
2. System logs Error with: Exception (full detail), CorrelationId, UserId (if authenticated), Endpoint path, HTTP method
3. System returns 500 Internal Server Error with generic body: `{"error": "An unexpected error occurred", "correlationId": "..."}`
4. Stack trace, internal paths, and exception type are NOT exposed to client

**Logging Events:**
| Event | Level | Properties |
|---|---|---|
| UnhandledException | Error | Exception, CorrelationId, UserId, Endpoint, Method |

---

### UC-9: Sensitive Data Destructuring

| Field | Detail |
|---|---|
| **Actor** | Any code path that logs a User or Incident object with `{@Object}` |
| **Precondition** | Destructuring policy registered in Serilog configuration |
| **Trigger** | `logger.LogInformation("User details: {@User}", user);` |

**Main Flow:**
1. Serilog applies registered `IDestructuringPolicy` for `User` type
2. Policy transforms Email to mask format: `"u***@***.com"`
3. Policy transforms FullName to `"[REDACTED]"`
4. All other properties (Id, Role) pass through unchanged
5. Log sink receives safe representation

**Verification:** No test log output shall contain a raw email or full name.

---

## 6. Logging Scenarios Specification

This section cross-references each required logging scenario with its trigger, verification method, and corresponding use case.

| Scenario # | Name | Trigger | Verification | UC Ref |
|---|---|---|---|---|
| 1 | Startup & Lifecycle | App boot | Log contains Version, Environment, MachineName; Fatal log on DB failure | — |
| 2 | Request Pipeline | Any HTTP request | Log contains RequestId, UserId (if auth'd), method, path, status, duration; /health suppressed | — |
| 3 | Correlation ID | Any request | X-Correlation-ID in response headers; all log lines share same ID | UC-1, UC-2, UC-3 |
| 4 | Security Event | Login | Successful login: UserId + Role. Failed: hashed email. 3+ failures → Warning. No raw PII/JWT. | UC-2 |
| 5 | Business Event | Incident CRUD | Create → IncidentId, Priority, ReportedBy. Assign → AssignedTo. Status → Old→New. All Information level. | UC-1, UC-3, UC-4 |
| 6 | Sensitive Data Masking | Any `{@User}` log | Email appears as `u***@***.com`, FullName as `[REDACTED]` | UC-9 |
| 7 | Performance | Filtered GET query | >500ms → Warning with params + duration. >2000ms → Error SLA breach. | UC-5 |
| 8 | External Dependency | Email call | Before: gateway + recipient UserId. After: success + duration. Failure: retry Warning, final Error. No crash. | UC-7 |
| 9 | Background Job | Timer fires | Start log, incident count, per-notification log, completion + duration. Unique CorrelationId per run. | UC-6 |
| 10 | Global Exception | Unhandled throw | Error log with CorrelationId + UserId + endpoint. Client receives safe 500. | UC-8 |
| 11 | Audit Logging | Any write via EF Core | AuditEntry in DB. Serilog emits "Audit: Action on Entity by UserId". Separate from app logs. | UC-1, UC-3, UC-4 |

---

## 7. Domain Model

### 7.1 Entity Definitions

#### Incident

| Property | Type | Constraints | Notes |
|---|---|---|---|
| Id | Guid | PK, auto-generated | |
| Title | string | Required, 3–200 chars | |
| Description | string | Required, 10–4000 chars | |
| ReportedBy | Guid | FK → User.Id | |
| AssignedTo | Guid? | FK → User.Id, nullable | Null when unassigned |
| Priority | Enum | Low, Medium, High, Critical | |
| Status | Enum | Open, InProgress, Resolved, Closed | |
| CreatedAt | DateTimeOffset | Utc, auto-set | |
| ResolvedAt | DateTimeOffset? | Nullable, set on resolution | |

#### User

| Property | Type | Constraints | Sensitivity |
|---|---|---|---|
| Id | Guid | PK | — |
| Email | string | Required, unique | **PII — MUST mask in logs** |
| FullName | string | Required | **PII — MUST mask in logs** |
| PasswordHash | string | Required | **Secret — NEVER log** |
| Role | Enum | Reporter, Agent, Admin | — |

#### AuditEntry

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK, auto-generated |
| EntityName | string | e.g., "Incident" |
| EntityId | Guid | ID of affected entity |
| Action | Enum | Created, Updated, StatusChanged |
| PerformedBy | Guid | FK → User.Id |
| Timestamp | DateTimeOffset | Utc, auto-set |
| Metadata | string? | JSON for old/new values |

### 7.2 Enumerations

**Priority:** `Low = 0, Medium = 1, High = 2, Critical = 3`

**Status:** `Open = 0, InProgress = 1, Resolved = 2, Closed = 3`

**Role:** `Reporter = 0, Agent = 1, Admin = 2`

**AuditAction:** `Created = 0, Updated = 1, StatusChanged = 2`

---

## 8. API Contract

### 8.1 Endpoint Summary

| Method | Path | Auth | Roles | Description |
|---|---|---|---|---|
| POST | /auth/login | None | — | Authenticate, receive JWT |
| POST | /incidents | Required | Reporter, Agent, Admin | Submit new incident |
| GET | /incidents/{id} | Required | All | Get incident by ID |
| PUT | /incidents/{id}/assign | Required | Agent, Admin | Assign to self |
| PUT | /incidents/{id}/status | Required | Agent, Admin | Update status |
| GET | /incidents | Required | All (filtered by role) | List with filters |
| GET | /health | None | — | Health check |

### 8.2 Request/Response Examples

#### POST /auth/login

**Request:**
```json
{
  "email": "agent@company.com",
  "password": "SecurePass123!"
}
```

**Response (200):**
```json
{
  "token": "eyJhbGciOi...",
  "expiresAt": "2026-04-27T04:00:00Z",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "role": "Agent"
  }
}
```

**Response (401):**
```json
{
  "error": "Invalid email or password."
}
```

#### POST /incidents

**Request:**
```json
{
  "title": "VPN Connection Dropping Every 5 Minutes",
  "description": "Users in the remote office are experiencing intermittent VPN disconnects throughout the workday.",
  "priority": "High"
}
```

**Response (201):**
```json
{
  "id": "d94f1c60-1b5f-4e3a-8c7a-9e5f3b2d1c0a",
  "title": "VPN Connection Dropping Every 5 Minutes",
  "description": "Users in the remote office...",
  "priority": "High",
  "status": "Open",
  "reportedBy": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
  "assignedTo": null,
  "createdAt": "2026-04-26T14:30:00Z",
  "resolvedAt": null
}
```

#### PUT /incidents/{id}/assign

**Response (200):**
```json
{
  "id": "d94f1c60-...",
  "status": "InProgress",
  "assignedTo": "7bc8c921-0ece-22e2-91c5-11d15fe541d9",
  "...": "..."
}
```

#### PUT /incidents/{id}/status

**Request:**
```json
{
  "status": "Resolved"
}
```

**Response (200):**
```json
{
  "status": "Resolved",
  "resolvedAt": "2026-04-26T16:45:00Z",
  "...": "..."
}
```

### 8.3 Error Response Format

All error responses follow:
```json
{
  "error": "Human-readable message",
  "correlationId": "guid-from-request"
}
```

Validation errors additionally include:
```json
{
  "error": "Validation failed",
  "correlationId": "...",
  "details": [
    { "field": "title", "message": "Title must be between 3 and 200 characters." }
  ]
}
```

---

## 9. Architecture Constraints

### 9.1 Vertical Slice Architecture

Each use case SHALL be implemented as a self-contained vertical slice:

```
src/PulseLog/
├── Features/
│   ├── Auth/
│   │   └── Login/
│   │       ├── LoginEndpoint.cs
│   │       ├── LoginHandler.cs
│   │       └── LoginRequest.cs
│   ├── Incidents/
│   │   ├── Create/
│   │   │   ├── CreateIncidentEndpoint.cs
│   │   │   ├── CreateIncidentHandler.cs
│   │   │   └── CreateIncidentRequest.cs
│   │   ├── Assign/
│   │   ├── UpdateStatus/
│   │   └── GetFiltered/
│   ├── Notifications/
│   │   └── SendIncidentEmail/
│   └── Background/
│       └── UnassignedIncidentScan/
├── Domain/
│   ├── Incident.cs
│   ├── User.cs
│   ├── AuditEntry.cs
│   └── Enums/
├── Infrastructure/
│   ├── Persistence/
│   ├── Auth/
│   ├── Email/
│   └── Logging/
└── Program.cs
```

### 9.2 Technology Stack

| Component | Technology |
|---|---|
| Runtime | .NET 8+ |
| Framework | ASP.NET Core Minimal API |
| Database | PostgreSQL (EF Core) |
| Logging | Serilog + Console/File sinks |
| Auth | ASP.NET Core JWT Bearer |
| Resilience | Polly (HttpClient resilience / retry) |
| Background Jobs | IHostedService (or Hangfire optional) |
| Email | MailKit (with stub/mock backend for dev) |

### 9.3 Key Design Decisions

| Decision | Rationale |
|---|---|
| Vertical slices | Each use case is self-contained; logging responsibility is local to the handler |
| Minimal API | Reduces ceremony; use case endpoints are explicit |
| EF Core interceptors for audit | Audit logging triggered on SaveChanges, not scattered in handlers |
| Destructuring policy via Serilog | Centralized PII masking — no developer needs to remember to mask manually |
| Polly at email gateway boundary | Resilience is a cross-cutting concern externalized from business logic |
| Correlation ID via middleware + LogContext | Ensures every log line is traceable without passing CorrelationId manually |

---

## 10. Acceptance Criteria

### 10.1 Per-Scenario Verification Checklist

| Scenario | Acceptance Test |
|---|---|
| **1. Startup** | Launch app → console shows Version, Environment, MachineName. Kill DB → app crashes with Fatal log. |
| **2. Request Pipeline** | Send any request → log shows RequestId, method, path, status, elapsed. Send GET /health → NO log entry. |
| **3. Correlation ID** | Send POST /incidents with X-Correlation-ID header. All log lines for that request share the same ID. Response includes X-Correlation-ID header. |
| **4. Security Login** | Successful login → log shows UserId, Role (NO raw email). Failed → hashed/masked email. 3 rapid fails → Warning log. Grep logs for password → no hits. |
| **5. Business Events** | Create incident → log: "IncidentCreated" with IncidentId + Priority. Assign → "IncidentAssigned". Status change → "IncidentStatusChanged" with Old→New. |
| **6. Destructuring** | Force-log `{@User}` → Email appears as `u***@***.com`, FullName = `[REDACTED]`. Never see raw email. |
| **7. Performance** | Trigger slow filtered query → Warning log at 500ms with duration. Artificially delay to 2s → Error log. |
| **8. External Email** | Send notification → log shows "EmailSending" + "EmailSent". Kill email stub → see 3 Warning retries, then Error. No unhandled exception. |
| **9. Background Job** | Wait for timer → log shows start, count, per-notification, completion. Different CorrelationId from HTTP requests. |
| **10. Global Exception** | Throw in an endpoint → client gets 500 `{"error": "...", "correlationId": "..."}`. Log contains full exception + CorrelationId. |
| **11. Audit Logging** | Create an incident → AuditEntry row in DB with Action=Created. Serilog emits "Audit: Created on Incident by {UserId}". |

### 10.2 Anti-Acceptance Criteria (Things That Should NOT Happen)

- ❌ Raw email or FullName appears in any log file
- ❌ Password or JWT token appears in any log file
- ❌ Stack trace appears in any HTTP 500 response body
- ❌ Health check requests generate log entries
- ❌ Application crashes when email service is unavailable
- ❌ Log lines lack CorrelationId
- ❌ String interpolation used instead of structured properties

---

## Appendix A: Log Event Catalog

| Event Name | Level | Trigger | Properties |
|---|---|---|---|
| AppStarted | Information | Startup success | Version, Environment, MachineName |
| AppStartupFailed | Fatal | Critical dependency down | Exception |
| HTTPRequest | Information | Every request (except /health) | RequestId, Method, Path, StatusCode, ElapsedMs, UserId |
| LoginSucceeded | Information | Successful auth | UserId, Role, CorrelationId |
| LoginFailed | Warning | Failed auth | EmailHash, IPAddress, FailureReason, CorrelationId |
| SuspiciousLoginPattern | Warning | 3+ rapid failures | EmailHash, AttemptCount, TimeWindow, CorrelationId |
| IncidentCreated | Information | Incident submission | IncidentId, Priority, ReportedBy, CorrelationId |
| CriticalIncidentCreated | Warning | Critical incident | IncidentId, CorrelationId |
| IncidentAssigned | Information | Assignment | IncidentId, AssignedTo, AssignedBy, CorrelationId |
| IncidentStatusChanged | Information | Status update | IncidentId, OldStatus, NewStatus, ChangedBy, CorrelationId |
| InvalidStatusTransition | Warning | Bad transition | IncidentId, From, To, CorrelationId |
| SlowQuery | Warning | Query > 500ms | Endpoint, QueryParams, DurationMs, CorrelationId |
| SlaBreachQuery | Error | Query > 2000ms | Endpoint, QueryParams, DurationMs, CorrelationId |
| EmailSending | Information | Before email call | Gateway, RecipientUserId, IncidentId, CorrelationId |
| EmailSent | Information | Successful email call | IncidentId, DurationMs, CorrelationId |
| EmailRetryAttempt | Warning | Email retry | Attempt, NextDelayMs, IncidentId, CorrelationId |
| EmailFailedAfterRetries | Error | All retries exhausted | IncidentId, TotalAttempts, Exception, CorrelationId |
| JobStarted | Information | Background job start | JobName, CorrelationId |
| UnassignedIncidentsFound | Information | Scan finds incidents | Count, CorrelationId |
| NotificationDispatched | Information | Per-incident notification | IncidentId, CorrelationId |
| JobCompleted | Information | Background job end | JobName, TotalIncidents, DurationMs, CorrelationId |
| UnhandledException | Error | Global handler | Exception, CorrelationId, UserId, Endpoint, Method |
| AuditAction | Information | EF write intercept | Action, EntityName, EntityId, UserId, CorrelationId |

---

## Appendix B: State Transition Matrix

| From \ To | Open | InProgress | Resolved | Closed |
|---|---|---|---|---|
| **Open** | — | ✅ Assign | ❌ | ❌ |
| **InProgress** | ✅ Reopen | — | ✅ Resolve | ❌ |
| **Resolved** | ✅ Reopen | ❌ | — | ✅ Close |
| **Closed** | ✅ Reopen | ❌ | ❌ | — |

---

**Document End**

---

This SRD covers every feature, use case, logging scenario, error condition, and security constraint you specified — structured for a methodical build across two weeks. Ready to scaffold when you are.