# Coding Challenge — RFID Tool Tracking (C# / .NET 8)

## The story

A manufacturing customer equips its expensive tools (torque wrenches, calipers, gauges) with
UHF RFID tags. Fixed RFID readers are installed in the **Warehouse**, on the **Production** floor and
in the **Calibration Lab**. Every reader antenna continuously reports every tag it can "see".

The customer wants to know:

* **Where is every tool right now?**
* **Which tools have gone missing?**
* **How long has each tool spent in each zone?** (e.g. to bill calibration time or find hoarding)

The readers export their raw data as CSV. Your job is to turn this noisy stream into reliable
business information.

> RFID readers are chatty: a tag sitting in front of an antenna is reported many times per second,
> the export can arrive out of order (readers buffer when the network is down), and sometimes the
> data is just broken. Your code must cope with all of that.

## Getting started

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet test
```

```bash
dotnet run --project src/ToolTracking.Cli -- data/reads.csv data/readers.json 2026-03-02T12:00:00Z
```

All tests fail at first with `NotImplementedException`. Make them pass, level by level.

```
src/ToolTracking/          <- the library - your work goes here
  Models.cs                   domain records (given, you may extend them)
  ReaderConfiguration.cs      reader -> zone mapping (given)
  CsvReadParser.cs            Level 1   TODO
  SightingAggregator.cs       Level 2   TODO
  MovementDetector.cs         Level 3   TODO
  InventoryReportBuilder.cs   Level 3   TODO
  Sgtin96Decoder.cs           Level 4   TODO (bonus)
src/ToolTracking.Cli/      <- console app wiring everything together (given)
tests/ToolTracking.Tests/  <- xUnit tests (given - add your own!)
data/                      <- sample reader export and reader configuration
```

You may change public signatures if you have a good reason — explain it in `NOTES.md`.

---

## Level 1 — Parse the reader export

`CsvReadParser.Parse(TextReader csv)`

Input format (`data/reads.csv`):

```
timestamp,readerId,antenna,epc,rssi
2026-03-02T08:00:00.100Z,WH-01,1,3034F4E4E40C0E40000003E9,-61.5
```

| Column      | Rule                                                                                  |
|-------------|---------------------------------------------------------------------------------------|
| `timestamp` | ISO-8601 **with** a UTC offset (`Z` or `±hh:mm`). Normalize to UTC.                    |
| `readerId`  | Together with `antenna` it must exist in the reader configuration -> gives the `Zone`. |
| `antenna`   | Integer.                                                                              |
| `epc`       | Exactly 24 hex characters (96 bit). Case-insensitive -> normalize to upper case.        |
| `rssi`      | Decimal number in dBm, invariant culture (`-61.5`).                                    |

* The first line is always a header. Blank lines are ignored.
* Surrounding whitespace in a field is ignored.
* An invalid line is **skipped** and reported as `ParseError(lineNumber, reason)`
  (line numbers are 1-based, the header is line 1). One bad line must never stop the import.
* Fields never contain commas; you don't need a full CSV library.

## Level 2 — De-duplicate reads into sightings

`SightingAggregator.Aggregate(IEnumerable<TagRead> reads)`

Collapse bursts of reads into one logical **sighting**:

* Reads of the **same EPC** in the **same zone** belong to the same sighting as long as each read is
  at most `window` (default **2 s**, inclusive) after the **previous** read of that sighting.
* A larger gap, or a read in a different zone, starts a new sighting.
* A sighting records `FirstSeen`, `LastSeen`, `ReadCount` and the strongest signal (`MaxRssi`,
  closest to 0).
* The input may be in any order. Return sightings ordered by `FirstSeen`, then `Epc` (ordinal).

## Level 3 — Movements and inventory report

`MovementDetector.Detect(IEnumerable<Sighting> sightings)`

* For each tool, walk its sightings in time order. Emit a `Movement` whenever the zone differs
  from the tool's previous zone, at the sighting's `FirstSeen`.
* The first sighting of a tool is a movement with `FromZone = null`.
* Return movements ordered by `At`, then `Epc`.

`InventoryReportBuilder.Build(IEnumerable<Sighting> sightings, DateTimeOffset asOf)`

One `AssetState` per tool, ordered by `Epc` (ordinal):

* `CurrentZone` — zone of the tool's most recent sighting.
* `LastSeen` — the last time the tool was read at all.
* `Status` — `Missing` if `asOf - LastSeen` is **greater than** `missingAfter`
  (default **30 min**), otherwise `Present`.
* `DwellTimeByZone` — total time per zone: a tool "is" in a zone from the moment it moved there
  until it moved somewhere else, or until `asOf` for its current zone.
* If `asOf` is earlier than any sighting's `LastSeen`, throw `ArgumentException`.

## Level 4 — Bonus (pick what you like)

**a) Decode the EPC.** Most tags carry a GS1 **SGTIN-96** (header `0x30`). Implement
`Sgtin96Decoder.TryDecode` so the report can show `urn:epc:id:sgtin:4012345.012345.1001`
instead of hex. Layout (MSB first):

| Bits | Field                          |
|------|--------------------------------|
| 8    | Header (`0x30`)                |
| 3    | Filter                         |
| 3    | Partition (0–6, see table)     |
| *M*  | Company prefix                 |
| *N*  | Item reference                 |
| 38   | Serial number                  |

| Partition | Company prefix bits / digits | Item reference bits / digits |
|-----------|------------------------------|------------------------------|
| 0         | 40 / 12                      | 4 / 1                        |
| 1         | 37 / 11                      | 7 / 2                        |
| 2         | 34 / 10                      | 10 / 3                       |
| 3         | 30 / 9                       | 14 / 4                       |
| 4         | 27 / 8                       | 17 / 5                       |
| 5         | 24 / 7                       | 20 / 6                       |
| 6         | 20 / 6                       | 24 / 7                       |

Company prefix and item reference are zero-padded to their digit count. Return `false` (never
throw) for anything that is not a valid SGTIN-96.

**b) Cross-reads.** Look at the caliper `...07D1` in the sample data: at 11:50:02 it is read *once*,
weakly (-70 dBm), by the lab reader right next to the production floor. Your report now says it
moved to the Calibration Lab. Propose — and if you have time, implement and test — a rule that
suppresses such ghost movements without hiding real ones.

**c) Production-ready.** In `NOTES.md`, sketch how you would turn this batch tool into a service
that receives reads live from hundreds of readers (e.g. via MQTT / LLRP) and feeds an ERP system.
What changes, what stays?

---

## What we look for

* **Correctness** — the rules above, including the edge cases.
* **Readability** — code a colleague can maintain; sensible names, small focused methods.
* **Tests** — the given ones pass; you added the ones *you* think are missing.
* **Robustness** — bad input is handled deliberately, not by accident.
* **Communication** — a short `NOTES.md`: assumptions, trade-offs, what you'd do with more time.

Timebox: **about 2–3 hours** for Levels 1–3. Level 4 is optional — a good `NOTES.md` beats a rushed
implementation. Please don't add NuGet packages to the library project.

Hand in a Git repository (or zip) with a meaningful commit history.
