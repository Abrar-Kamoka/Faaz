# HESA Discover Uni dataset — bundled seed data

Source: [Discover Uni dataset](https://www.hesa.ac.uk/support/tools-and-downloads/unistats) (formerly Unistats),
published by HESA on behalf of the Office for Students. Downloaded 2026-08-25 (2025/26 record).

Licence: **Creative Commons Attribution 4.0 International (CC BY 4.0)** — free to copy, use, share, and
adapt for any purpose, with attribution. Credit: HESA, www.hesa.ac.uk.
Licence text: https://creativecommons.org/licenses/by/4.0/

## Files

- `INSTITUTION.csv` — 510 rows, UK HE providers (undergraduate). Keyed by `PUBUKPRN`.
- `KISCOURSE.csv` — ~31,000 undergraduate courses.
- `SBJ.csv` — course → CAH3 subject code links (a course can have several, for joint honours).
- `UCASCOURSEID.csv` — UCAS course codes per course/location.
- `KISAIM.csv` — qualification aim code → label lookup (e.g. `021` → `BSc`).
- `HECoS_CAH_Version_1.3.4.xlsx` — CAH subject code → name/category lookup (sheet `CAH (V1.3.4)`).

## Scope

**Undergraduate only.** This dataset does not cover postgraduate courses — HESA/OfS's Discover Uni
product is explicitly UG-scoped. Postgraduate data is a separate, tracked effort (see project notes) —
do not assume this seed covers it.

## Consumed by

`DiscoverUniSeeder.cs` — runs at startup if the `Universities`/`Programmes` tables look empty or
partially populated (see that file for the exact threshold and upsert-by-natural-key logic that keeps
re-runs idempotent).

## Updating this data

HESA refreshes the live dataset weekly. To refresh this bundled snapshot: download a new copy from the
link above, replace these files (same filenames), and re-run the seeder's threshold check — or bump the
threshold / add a version marker if you want a forced re-sync rather than the default "only fills gaps"
behaviour.
