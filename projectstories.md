# ✅ User Stories & Test Definitions: Cosmos DB Auction & Lot System

---

## 🎯 Epic: Localized Lot & Auction Presentation with Redis Optimization

### Story 1: As a system, I need to store and retrieve multilingual auction data

**Acceptance Criteria**:

- Auction document supports multiple localized blocks per language, in the format: `localizedData: [{ lng: "en", ... }, { lng: "de", ... }]`
- Schema version (`schemaVersion`) must be present in each document.
- Visibility attribute must be one of `internal` or `public`, and validated.
- Each localized block must contain `title` and `location` fields.
- If a translation is missing, fallback logic is required in read functions.

**Tests:**

- [Unit] Validate schema version and localization block structure.
- [Unit] Validate fallback for missing translations.
- [Integration] Test retrieval of auction data per language with visibility filtering.

---

### Story 2: As a system, I need to support multilingual lot records

**Acceptance Criteria**:

- Lot document must include `localizedData` array keyed by language.
- Each block contains `title`, `artist`, and `auctionSummary` with `id`, `title`, `startDate`.
- Lots may be unassigned (`auctionId = null`) and remain valid.
- When lot is assigned to an auction, auctionSummary must reflect latest auction data.

**Tests:**

- [Unit] Validate presence and structure of each language block.
- [Unit] Validate correct update behavior when assigned to a new auction.
- [Integration] Retrieve lot data and assert auctionSummary matches auction.

---

### Story 3: As a developer, I want a Redis lookup layer for fast `lotId` to `auctionId` resolution

**Acceptance Criteria**:

- Redis key format: `lot:{lotId}` maps to `{ auctionId }`.
- Redis keys are persisted and do not expire.
- If Redis misses, Cosmos DB fallback occurs and result is cached in Redis.
- Admin tooling can inspect Redis content.

**Tests:**

- [Unit] Redis key generation and format validation.
- [Integration] Redis lookup with fallback and auto-backfill.
- [Integration] Validate Redis survives container restart (manual Docker volume mount).

---

### Story 4: As a platform, I need change feed to propagate auction updates into related lots

**Acceptance Criteria**:

- Change feed listener on Auctions container processes changes and finds all lots with matching `auctionId`.
- For each localized block, update `auctionSummary` field in corresponding language block of lots.
- Logic must handle missing translation cases gracefully.
- Update preserves existing lot data outside auctionSummary.

**Tests:**

- [Unit] Language merge logic between auction and lot.
- [Integration] Simulate title change in auction and validate updated summary in all lots.

---

### Story 5: As an admin, I want to seed Redis and Cosmos with existing data

**Acceptance Criteria**:

- Admin endpoint/function accepts JSON payloads for auctions and lots.
- On insert, Redis entries are created for lots.
- Validations are run before insert (schema, visibility, required fields).

**Tests:**

- [Integration] Run full seed with known dataset.
- [Unit] Validate schema checks during import.

---

### Story 6: As a system, I need to mark data readiness for public access

**Acceptance Criteria**:

- Each document contains `visibility` field (`public` or `internal`).
- Document validator ensures that for `public`, all required localized fields are populated.
- Optional: `readyForReview = true/false` indicates editor workflow state.

**Tests:**

- [Unit] Validate completeness check.
- [Integration] Test API endpoint filtering only `public` documents.

---

### Story 7: As a system, I want to track historical changes for auditing

**Acceptance Criteria**:

- `_version` increments on every update.
- `history` stores previous document snapshots with timestamp and update type (create/update/delete).
- History is retrievable through internal API.

**Tests:**

- [Unit] Snapshot creation on update.
- [Integration] Retrieve history and compare field diffs.

---

### Story 8: As a system, I need response times <100ms for read requests

**Acceptance Criteria**:

- Redis response for partition key lookup must be under 20ms.
- Cosmos DB reads with projection queries must remain under 100ms.
- Document size and index usage must be optimized.

**Tests:**

- [Integration] Measure and log request durations.
- [Unit] Test field-level projections for known fields.

---

### Story 9: As a system, I should support lots moving between auctions

**Acceptance Criteria**:

- Lot `auctionId` can be changed.
- Change triggers Redis update and auctionSummary update.
- If `auctionId` is null, remove auctionSummary blocks.

**Tests:**

- [Unit] Auction reassignment logic.
- [Integration] Simulate lot move and confirm updates.

---

### Story 10: As a developer, I need validation tools to ensure schema integrity before writes

**Acceptance Criteria**:

- Admin/test tools include validators for auctions and lots.
- Validations cover: required fields, language presence, visibility rules, schema version.

**Tests:**

- [Unit] Test required fields, invalid structures.
- [Integration] Simulate reject on invalid schema payload.

---

## 🧪 Testing Requirements Summary

| Area                     | Unit Test | Integration Test |
| ------------------------ | --------- | ---------------- |
| Schema Validation        | ✅        | ✅               |
| Redis Fallback           | ✅        | ✅               |
| Change Feed Propagation  | ✅        | ✅               |
| Visibility Filtering     | ✅        | ✅               |
| Seeding & Backfill       | ✅        | ✅               |
| Language Consistency     | ✅        | ✅               |
| Version History          | ✅        | ✅               |
| Performance Benchmarks   | ✅        | ✅               |
| Auction Reassignment     | ✅        | ✅               |
| Document Readiness Check | ✅        | ✅               |
| Admin Tools              | ✅        | ✅               |

---

## 🔍 Notes

- Schema versioning allows compatibility with partial migrations.
- Not all localized blocks are required; fallbacks apply.
- Redis entries must persist across instance restarts (Docker volumes required).
- All tests must be Docker-compatible and CI-ready.
- All services must be observable with logging and tracing enabled for all Redis misses and change feed triggers.
- Versioning logic must preserve backward compatibility.
- Developer tools must include Cosmos emulator seeding scripts and Redis snapshot loader.
