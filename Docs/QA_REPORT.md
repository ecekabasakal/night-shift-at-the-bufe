# QA REPORT — Night Shift at the Büfe

Owner: QA Engineer. Version 1 (Part 1: EditMode tests written, Unity run pending).

## 1. Scope

- **Covered:** the game data (`Assets/Game/Resources/Data/*.json`) against `Docs/DATA_SCHEMA.md` and the design brief, and the pure game logic (`GameSession`, `NoticedMeter`, `GameData`) through the public `IGameSession` contract.
- **How:** EditMode tests (Unity Test Framework 1.8.0, NUnit) in `Assets/Game/Tests/`, assembly `NightShift.Tests` (Editor only). Tests load the real JSON through `GameData.LoadFromResources()` and drive the session with fixed 0.1 s ticks; no real time, no scene, no MonoBehaviours.
- **Not covered by automated tests:** `GameBootstrap`, UI, Art, Audio, the WebGL build. These get the player's-eye review in section 6 (Part 2).
- The tests are written against the data, not against the current 13 customers: they find "an ordinary human", "a starer", "an instantEnd customer" in whatever the JSON contains, so they keep working after a data edit or a translation.

## 2. Test inventory

120 test cases in 7 fixtures.

| File | What it covers | Cases |
|---|---|---|
| `DataConsistencyTests.cs` | Customer count, unique ids, arrival order, enum-like strings, anomaly vocabulary, `staresBack` ⇄ `staring`, hex colours, `repeatOf`, special button, `instantEnd` limit, every text key present, required `ui.*` / `intro.note` / `ending.*` keys, `item.*`, no empty or duplicate strings, rules used and existing, balance values, and the design invariants (first three customers, ayran before/after 03:00, special button correct once and decoy once) | 23 |
| `CustomerDecisionTests.cs` | One case per customer: correct action (13). One case per customer and wrong button: mistake or instant end with the right ending (17). One case per customer: special button offered only when it exists (13) | 43 |
| `NoticedMeterTests.cs` | Three fill rates, drain, clamp at 0 and 1, huge delta, carry-over, full meter = `Seen` at once, meter during feedback, total Shift seconds, `IsShiftActive` / `IsStaredAt` | 11 |
| `MistakesAndEndingsTests.cs` | Mistake counter, `Fired` only after Continue, max−1 mistakes then `Morning`, `Morning` / `Changed` on each side of the threshold, `Seen`, instant ends, `Fired` wins over `Changed` | 11 |
| `ClockTests.cs` | Clock = `arrivalMinute` at every customer, ends at `shiftLengthMinutes` (06:00), `FormatClock` (8 values) | 11 |
| `CallOrderTests.cs` | Start-up state, calls before `StartNight`, double `Decide`, `Advance` at the wrong time, double `Advance`, `Special` without a button, calls after the end, `Reset()` mid-night then a clean night, `StartNight` after a lost night | 10 |
| `FullNightPlaythroughTests.cs` | The bot plays whole nights: `Morning`, `Changed`, `Seen` (two ways), `Fired`, each wrong button on each `instantEnd` customer (3), all four endings reachable, "always Sell" and "always Refuse" do not win (2) | 11 |
| `TestSupport.cs`, `NightBot.cs` | Helpers: data loading, `SessionDriver`, `SessionTestBase`, the scripted bot | — |

## 3. Results

| Run | Result |
|---|---|
| **Unity batchmode run (EditMode)** | **PENDING — to be filled from the results XML** |
| Compile check, Unity's Roslyn (`-langversion:9.0`), against Unity's own `nunit.framework.dll` and TestRunner assemblies; Core + Gameplay + throwaway stubs of UI/Art/Audio | 0 errors, 0 warnings |
| Run **outside Unity** (.NET 8, NUnit 3.14, `System.Text.Json` standing in for `JsonUtility`, files read from disk standing in for `Resources.Load`) | 120 of 120 passed |
| Mutation check outside Unity (a deliberately broken copy of the data: wrong colour, unknown anomaly, missing strings, unused rule, arrival order, balance) | 14 tests failed, as intended |

What the outside-Unity run does **not** prove: that `JsonUtility` reads the files the same way, that `Resources.Load` finds them in the editor, and that the asmdef resolves. Only the Unity run settles those.

## 4. Playthrough traces (run OUTSIDE Unity, not yet confirmed in Unity)

Columns: clock, customer, seconds of Shift held, Noticed right after looking, button, result, mistakes. The same traces are written to the log by `FullNightPlaythroughTests` in Unity.

```
Playthrough: careful perfect play (2 s of Shift per customer, a glance at starers)
23:05  c01_student         shift 2.0s  noticed 0.20  Sell     correct  mistakes 0/3
23:40  c02_taxi            shift 2.0s  noticed 0.20  Sell     correct  mistakes 0/3
00:15  c03_auntie          shift 2.0s  noticed 0.20  Sell     correct  mistakes 0/3
00:50  c04_bekci           shift 2.0s  noticed 0.20  Sell     correct  mistakes 0/3
01:20  c05_courier         shift 2.0s  noticed 0.50  Refuse   correct  mistakes 0/3
01:50  c06_student_again   shift 2.0s  noticed 0.20  Sell     correct  mistakes 0/3
02:20  c07_cat             shift 2.0s  noticed 0.50  Special  correct  mistakes 0/3
02:50  c08_auntie_again    shift 2.0s  noticed 0.50  Refuse   correct  mistakes 0/3
03:20  c09_nightowl        shift 2.0s  noticed 0.20  Refuse   correct  mistakes 0/3
03:55  c10_bekci_fake      shift 2.0s  noticed 0.50  Refuse   correct  mistakes 0/3
04:25  c11_tall            shift 0.3s  noticed 0.20  Sell     correct  mistakes 0/3
04:55  c12_fisherman       shift 2.0s  noticed 0.20  Sell     correct  mistakes 0/3
05:30  c13_hasan           shift 2.0s  noticed 0.50  Refuse   correct  mistakes 0/3
06:00  ENDING: Morning  (mistakes 0/3, Shift held 24.3 s, noticed 0.00)

Playthrough: perfect decisions, heavy Shift use (3.5 s per customer, a glance at starers)
23:05  c01_student         shift 3.5s  noticed 0.35  Sell     correct  mistakes 0/3
23:40  c02_taxi            shift 3.5s  noticed 0.35  Sell     correct  mistakes 0/3
00:15  c03_auntie          shift 3.5s  noticed 0.35  Sell     correct  mistakes 0/3
00:50  c04_bekci           shift 3.5s  noticed 0.35  Sell     correct  mistakes 0/3
01:20  c05_courier         shift 3.5s  noticed 0.87  Refuse   correct  mistakes 0/3
01:50  c06_student_again   shift 3.5s  noticed 0.35  Sell     correct  mistakes 0/3
02:20  c07_cat             shift 3.5s  noticed 0.87  Special  correct  mistakes 0/3
02:50  c08_auntie_again    shift 3.5s  noticed 0.87  Refuse   correct  mistakes 0/3
03:20  c09_nightowl        shift 3.5s  noticed 0.35  Refuse   correct  mistakes 0/3
03:55  c10_bekci_fake      shift 3.5s  noticed 0.87  Refuse   correct  mistakes 0/3
04:25  c11_tall            shift 0.3s  noticed 0.20  Sell     correct  mistakes 0/3
04:55  c12_fisherman       shift 3.5s  noticed 0.35  Sell     correct  mistakes 0/3
05:30  c13_hasan           shift 3.5s  noticed 0.87  Refuse   correct  mistakes 0/3
06:00  ENDING: Changed  (mistakes 0/3, Shift held 42.3 s, noticed 0.00)

Playthrough: never lets go of Shift
23:05  c01_student         shift 60.0s  noticed 1.00  -> the meter filled while looking
23:05  ENDING: Seen  (mistakes 0/3, Shift held 10.1 s, noticed 1.00)

Playthrough: perfect decisions, but looks at the starer as long as at anyone (2 s)
(c01 to c10 as in the careful night)
04:25  c11_tall            shift 2.0s  noticed 1.00  -> the meter filled while looking
04:25  ENDING: Seen  (mistakes 0/3, Shift held 21.5 s, noticed 1.00)

Playthrough: always presses a wrong button
23:05  c01_student         shift 0.0s  noticed 0.00  Refuse   WRONG    mistakes 1/3
23:40  c02_taxi            shift 0.0s  noticed 0.00  Refuse   WRONG    mistakes 2/3
00:15  c03_auntie          shift 0.0s  noticed 0.00  Refuse   WRONG    mistakes 3/3  -> ends the night
00:15  ENDING: Fired  (mistakes 3/3, Shift held 0.0 s, noticed 0.00)

Playthrough: perfect play, then Refuse on c11_tall
(c01 to c10 as in the careful night)
04:25  c11_tall            shift 0.3s  noticed 0.20  Refuse   WRONG    mistakes 0/3  -> ends the night
04:25  ENDING: Seen  (mistakes 0/3, Shift held 20.3 s, noticed 0.00)

Playthrough: perfect play, then Sell on c13_hasan   (same result with Special, the keys button)
(c01 to c12 as in the careful night)
05:30  c13_hasan           shift 2.0s  noticed 0.50  Sell     WRONG    mistakes 0/3  -> ends the night
05:30  ENDING: Fired  (mistakes 0/3, Shift held 24.3 s, noticed 0.00)

Playthrough: always Sell, never looks
23:05  c01_student         shift 0.0s  noticed 0.00  Sell     correct  mistakes 0/3
23:40  c02_taxi            shift 0.0s  noticed 0.00  Sell     correct  mistakes 0/3
00:15  c03_auntie          shift 0.0s  noticed 0.00  Sell     correct  mistakes 0/3
00:50  c04_bekci           shift 0.0s  noticed 0.00  Sell     correct  mistakes 0/3
01:20  c05_courier         shift 0.0s  noticed 0.00  Sell     WRONG    mistakes 1/3
01:50  c06_student_again   shift 0.0s  noticed 0.00  Sell     correct  mistakes 1/3
02:20  c07_cat             shift 0.0s  noticed 0.00  Sell     WRONG    mistakes 2/3
02:50  c08_auntie_again    shift 0.0s  noticed 0.00  Sell     WRONG    mistakes 3/3  -> ends the night
02:50  ENDING: Fired  (mistakes 3/3, Shift held 0.0 s, noticed 0.00)

Playthrough: always Refuse, never looks
23:05  c01_student         shift 0.0s  noticed 0.00  Refuse   WRONG    mistakes 1/3
23:40  c02_taxi            shift 0.0s  noticed 0.00  Refuse   WRONG    mistakes 2/3
00:15  c03_auntie          shift 0.0s  noticed 0.00  Refuse   WRONG    mistakes 3/3  -> ends the night
00:15  ENDING: Fired  (mistakes 3/3, Shift held 0.0 s, noticed 0.00)
```

## 5. Bugs and risks

No bug found in the data, in `Scripts/Core` data code or in `Scripts/Gameplay`. Open points:

| # | Finding | Owner | Severity |
|---|---|---|---|
| R1 | **The `changed` ending has a narrow window.** A single 4 s look at any `isOther` customer fills the meter (0.25 × 4 = 1.0) and ends the night as `seen`; six of the thirteen customers are others, the first at 01:20. To pass 40 s in total the player has to average more than 3.1 s per customer while staying under 4 s on every other. A player who looks 3 s everywhere gets `morning` (36.3 s); one who looks 4 s everywhere is `seen` at the courier. Most players with the "habit of looking" will see `seen`, not `changed`. Options: lower `changedShiftSecondsThreshold` (about 30), or lower `noticedFillPerSecondOther`. | Game Designer | Medium (balance, not a defect) |
| R2 | **GDD text contradicts the numbers.** "A player who holds Shift about 4–5 s per customer reaches 52–65 s without ever filling the meter" is not true for others (see R1); it only holds for humans. | Game Designer | Low (document) |
| R3 | The Unity run is still pending. The outside-Unity run used a stand-in for `JsonUtility` and `Resources.Load`; a difference in how Unity reads the JSON would only show there. | Orchestrator (run), QA (fix tests if needed) | Medium until run |
| R4 | The per-customer cases are generated with `[TestCaseSource]`, which loads the data while Unity builds the test list. If the data cannot be loaded at that moment, those cases appear as errors rather than as 43 separate tests. The expected total in Unity is 120. | QA | Low |
| R5 | `IGameSession.Decide` returns the previous decision when called at the wrong time. `GameBootstrap` always checks `CanDecide` first, so this is harmless today; a new caller must do the same. | Tech Lead / Gameplay | Info |

## 6. Player's-eye checklist (Part 2)

To be filled after integration.
