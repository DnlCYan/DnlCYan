# Interviewer Guide — RFID Tool Tracking

> **Remove the whole `interviewer/` folder before handing the challenge to a candidate.**

## Formats

| Format                 | Scope                         | Time      |
|------------------------|-------------------------------|-----------|
| Take-home              | Levels 1–3 + `NOTES.md`, L4 optional | 2–3 h     |
| Live pairing           | Levels 1–2, talk through Level 3 | 60–90 min |
| Follow-up interview    | Walk through their solution, then the discussion questions below | 45 min |

## Checking a submission

The reference solution lives in `reference-solution/`. To sanity-check the test suite, copy it over
the stubs:

```bash
cp interviewer/reference-solution/*.cs src/ToolTracking/
```

```bash
dotnet test
```

Run the candidate's code against the sample data:

```bash
dotnet run --project src/ToolTracking.Cli -- data/reads.csv data/readers.json 2026-03-02T12:00:00Z
```

### Expected result on `data/reads.csv` (asOf `2026-03-02T12:00:00Z`)

**18 reads parsed, 6 lines rejected:**

| Line | Reason                       | Trap                                  |
|------|------------------------------|---------------------------------------|
| 8    | unknown reader `DOCK-09`      | configuration lookup                  |
| 13   | timestamp `yesterday`         | basic validation                      |
| 17   | EPC has 23 characters         | length check                          |
| 18   | only 4 columns                | column count                          |
| 19   | timestamp without offset      | silent local-time assumption          |
| 20   | RSSI `n/a`                    | number parsing                        |

Not errors (common mistakes): line 14 is blank (must be ignored, not reported); line 15 has a
lower-case EPC and line 16 has spaces around the EPC — both valid and the **same tag** as other reads;
line 9 uses `+01:00` (= 09:00Z).

**12 sightings. Inventory:**

| EPC                        | Zone           | Last seen (UTC) | Status  | Dwell time                                   | SGTIN                                  |
|----------------------------|----------------|-----------------|---------|----------------------------------------------|----------------------------------------|
| `3034F4E4E40C0E40000003E9` | Warehouse      | 11:45:00.000    | Present | Warehouse 00:29:59.900, Production 03:30:00 | `urn:epc:id:sgtin:4012345.012345.1001` |
| `3034F4E4E40C0E40000003EA` | CalibrationLab | 09:00:03.000    | Missing | Warehouse 00:55:00, CalibrationLab 03:00:00 | `urn:epc:id:sgtin:4012345.012345.1002` |
| `3034F4E4E40C0E40000003EB` | Production     | 11:59:00.000    | Present | Production 02:00:00                          | `urn:epc:id:sgtin:4012345.012345.1003` |
| `3034F4E4E4424C80000007D1` | CalibrationLab | 11:50:02.400    | Present | Production 00:00:02.400, CalibrationLab 00:09:57.600 | `urn:epc:id:sgtin:4012345.067890.2001` |
| `E2801160600002084D6A3B1C` | Warehouse      | 11:58:00.000    | Present | Warehouse 00:02:00                           | not an SGTIN-96                        |

Useful invariants: for every tool, the dwell times add up to `asOf - first movement`. Tool `...3E9`
has two Production sightings (08:15 and 09:30) but only one Production movement. Tool `...3EA`
has two lab sightings 2.5 s apart (window exceeded) — still only one movement.

## Rubric

Score each 1–4. A solid hire is mostly 3s with no 1s.

| Area          | 1 — concern                                    | 4 — excellent                                                         |
|---------------|------------------------------------------------|----------------------------------------------------------------------|
| Correctness   | Given tests fail; ordering/edge cases ignored   | All tests pass; found edge cases themselves (NaN RSSI, empty input)  |
| Parsing       | Throws on first bad line; culture-dependent parsing (`double.Parse` without invariant culture breaks on German Windows!) | Invariant culture, explicit offset handling, clear error reasons |
| Design        | One 200-line method; static mutable state      | Small pure functions, immutable records (`with`), clear separation   |
| C# idioms     | Manual loops where LINQ is clearer, or unreadable LINQ chains | Appropriate LINQ, `TryParse` patterns, nullable annotations respected |
| Testing       | No new tests                                   | Adds meaningful tests (window boundary, multiple tags, time zones)  |
| Communication | No notes                                       | States assumptions, trade-offs, what is missing                     |

### Red flags

* `DateTime` instead of `DateTimeOffset`, or `DateTime.Now` inside the logic (untestable).
* `DateTimeOffset.Parse` on line 19 — silently uses the machine's time zone.
* Aggregation window measured from `FirstSeen` instead of the previous read (test catches it).
* Comparing zones / EPCs with culture-sensitive comparison, or sorting without `StringComparer.Ordinal`.
* Catch-all `try { ... } catch { }` hiding bugs.
* Mutating input collections or sharing state between calls.

### Green flags

* Notices that sorting the whole input is O(n log n) and memory-bound, and mentions streaming.
* Validates constructor arguments (negative window).
* Explains *why* timestamps without an offset are rejected.
* Writes a test with the `+01:00` timestamp and the out-of-order rows.

## Discussion questions

Pick a few, depending on seniority.

**Code & .NET**

1. Why `DateTimeOffset` rather than `DateTime`? What happens on the night of a DST change?
2. Your parser loads everything into memory. The file is 20 GB — what do you change?
   (`IAsyncEnumerable<TagRead>`, streaming, `System.IO.Pipelines`, `Span<char>` parsing.)
3. `SightingAggregator` sorts all reads. Readers deliver *almost* sorted data. How can you
   aggregate in a stream with bounded memory? (Per-tag open sightings, close on timeout,
   watermark / allowed lateness.)
4. How would you make `InventoryReportBuilder` testable without passing `asOf`? (`TimeProvider` in .NET 8.)
5. Records vs. classes — where would value equality bite you?

**RFID domain (Level 4)**

6. Ghost reads / cross-reads: what signals could you use? (RSSI threshold per antenna, minimum
   read count, require *N* reads within *T*, compare RSSI across zones within a window,
   hysteresis / dwell confirmation, reader-pair directionality at gates.)
7. What is the trade-off of suppressing short sightings? (Latency of reporting real movements vs
   false movements; a tool really carried quickly through a gate.)
8. The EPC is 96 bit. Why might two readers report different EPCs for the same physical tag?
   (Badly encoded tags, TID vs EPC memory bank, re-commissioned tags.)

**Architecture (Level 4c)**

9. Hundreds of readers send reads live over MQTT. Sketch the service: ingestion, de-duplication,
   state store, outbound events. Where does the state of "open sightings" live when you scale
   out to several instances? (Partition by EPC / reader, Kafka keyed topics, actor model.)
10. The ERP (e.g. SAP) should only receive *movements*, at most once each. How do you guarantee
    that across restarts? (Idempotent event IDs, outbox pattern, checkpointing.)
11. A reader was offline for 3 hours and now flushes its buffer. What happens to your
    "Missing" alerts and movements that were already sent?
12. How do you monitor this in production? (Reads/s per reader, silent readers, parse error rate.)

## Possible extensions for a live session

* Add a configurable RSSI threshold per antenna (extends `readers.json`).
* Add an "alert" when a tool leaves the plant through a gate reader.
* Expose the report via a minimal ASP.NET Core endpoint `GET /assets/{epc}`.
