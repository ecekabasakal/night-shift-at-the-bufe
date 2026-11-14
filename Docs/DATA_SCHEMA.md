# DATA_SCHEMA — fixed contract between data (JSON) and code (C#)

Status: **APPROVED by Orchestrator (Phase 1)**. Changing a field name or type requires an entry on `Docs/OFFICE_BOARD.md`.

All files live in `Assets/Game/Resources/Data/` and are loaded with `Resources.Load<TextAsset>("Data/<name>")` + `JsonUtility.FromJson<T>`.
JsonUtility limits apply: no Dictionary, no top-level arrays, no null — use `""` for "none", `[]` for empty lists. Field names are camelCase and must match the C# field names exactly. Enums are stored as lowercase strings and parsed in code.

## customers.json → `CustomerList`

```json
{ "customers": [ CustomerData, ... ] }
```

Array order = order of arrival. 10–14 entries.

| Field | Type | Meaning |
|---|---|---|
| `id` | string | Unique, e.g. `"c01_student"`. |
| `nameKey` | string | strings.json key of the short label shown above the dialogue ("Student", "Bekçi"...). |
| `arrivalMinute` | int | Minutes after 23:00 when this customer arrives (0–419). Strictly increasing. The HUD clock shows this time while the customer is at the counter. |
| `requestItem` | string | Item id: `tost`, `ayran`, `simit`, `cigarettes`, `water`, `matches`, `cekirdek`, `catfood`, `none`. |
| `greetingKey` | string | Line said on arrival. |
| `requestKey` | string | Line stating what they want. |
| `shiftHintKey` | string | One short line shown **only while Shift is held**, describing what the player sees (keeps the game readable even if the pixel art is subtle). Never states the correct action. |
| `isOther` | bool | True = not human. The Noticed meter fills at the faster "other" rate. |
| `staresBack` | bool | True = it looks back at you during Shift (rule "release immediately"). Meter fills at the "stare" rate. |
| `ruleId` | string | Rule that decides this customer, `""` if it is an ordinary sale. |
| `correctAction` | string | `sell` \| `refuse` \| `special`. |
| `specialActionKey` | string | Label key of the third button for this customer, `""` = no third button. May be set on customers whose correct action is NOT `special` (decoy). |
| `wrongOutcome` | string | `mistake` (adds 1 mistake) \| `instantEnd` (ends the night at once with the `fired`/`seen` ending — see `instantEndingId`). At most 2 customers use `instantEnd`. |
| `instantEndingId` | string | `seen` or `fired`; only read when `wrongOutcome` is `instantEnd`, otherwise `""`. |
| `correctResultKey` | string | Feedback line after the correct action. |
| `wrongResultKey` | string | Feedback line after a wrong action. |
| `repeatOf` | string | `id` of an earlier customer this one is "the same person" as (rule about returning customers), else `""`. Appearance must be identical to that customer. |
| `appearance` | Appearance | Normal view. |
| `shiftAnomalies` | string[] | What changes in the Shift view. `[]` = looks normal (ordinary human, shadow present). |

### Appearance

| Field | Type | Values |
|---|---|---|
| `kind` | string | `human` \| `cat` |
| `skinColor` | string | `#RRGGBB` (fur colour for cats) |
| `hairColor` | string | `#RRGGBB` |
| `hairStyle` | int | 0 bald, 1 short, 2 long, 3 bun, 4 cap/hat |
| `outfitColor` | string | `#RRGGBB` |
| `outfit` | int | 0 t-shirt, 1 jacket, 2 hoodie, 3 uniform (bekçi) |
| `accessory` | string | `none` \| `whistle` \| `glasses` \| `bag` \| `scarf` |

### Shift anomaly vocabulary (`shiftAnomalies`)

`noShadow`, `reversedShadow`, `noEyes`, `wrongEyes` (eyes looking the wrong way), `extraFingers`, `tooManyTeeth`, `noWhistle` (accessory vanishes), `hollow` (body is a dark silhouette), `staring` (bright eyes locked on the player — use together with `staresBack: true`), `tallCat` (cat stretched far too tall).

The Art programmer must support every value above; the Designer must use only these values.

## rules.json → `RuleList`

```json
{ "rules": [ { "id": "r1_shadow", "textKey": "rule.r1_shadow", "needsShift": true } ] }
```

6–8 rules. Order = order in the notebook. Every rule id must be referenced by at least one customer's `ruleId`.

## strings.json → `StringTable`

```json
{ "entries": [ { "key": "ui.start", "value": "Start shift" } ] }
```

Every player-visible string in the game. Key prefixes: `ui.*`, `rule.*`, `cust.<id>.*`, `ending.<endingId>.title|body`, `item.*`, `intro.*`. A missing key is rendered as `#key#` (and fails a QA test).

Required UI keys (UI programmer relies on these): `ui.title`, `ui.tagline`, `ui.shiftHint` ("Hold SHIFT to see what's really there"), `ui.start`, `ui.sell`, `ui.refuse`, `ui.notebookTitle`, `ui.notebookHint`, `ui.noticed`, `ui.mistakes`, `ui.playAgain`, `ui.continue`, `ui.controls`, `intro.note` (the previous worker's note shown before the first customer).

## balance.json → `BalanceData`

```json
{
  "noticedFillPerSecond": 0.0,
  "noticedFillPerSecondOther": 0.0,
  "noticedFillPerSecondStare": 0.0,
  "noticedDrainPerSecond": 0.0,
  "maxMistakes": 3,
  "changedShiftSecondsThreshold": 0.0,
  "shiftLengthMinutes": 420
}
```

The Noticed meter runs 0..1. Fill rates apply while Shift is held (base rate when the current customer is an ordinary human or nobody is at the counter; `Other` rate when `isOther`; `Stare` rate when `staresBack`). The drain applies while Shift is released. The meter carries over between customers.

## Endings

| `endingId` | When |
|---|---|
| `morning` | All customers served, mistakes < `maxMistakes`, total Shift seconds ≤ `changedShiftSecondsThreshold`. |
| `changed` | All customers served, mistakes < `maxMistakes`, total Shift seconds > threshold. |
| `seen` | Noticed meter reached 1.0, or an `instantEnd` with `instantEndingId: seen`. |
| `fired` | Mistakes reached `maxMistakes`, or an `instantEnd` with `instantEndingId: fired`. |

`seen` and `fired` are the two flavours of the third ending ("It saw you / You're fired").

## Actions

`sell`, `refuse`, `special` — keyboard 1 / 2 / 3. A decision is correct iff it equals `correctAction`.
