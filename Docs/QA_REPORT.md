# QA REPORT — Night Shift at the Büfe

Owner: QA Engineer. Version 3 (UI fixes for B1 and B2 applied; PlayMode run 3 pending).

## 1. Scope

- **EditMode tests** (`Assets/Game/Tests/`, assembly `NightShift.Tests`): the game data against `Docs/DATA_SCHEMA.md` and the design brief, and the pure game logic (`GameSession`, `NoticedMeter`, `GameData`) through the public `IGameSession` contract. They load the real JSON through `GameData.LoadFromResources()` and use fixed 0.1 s ticks.
- **PlayMode smoke test** (`Assets/Game/Tests/PlayMode/`, assembly `NightShift.PlayModeTests`): loads the real `Main` scene and plays it by pressing the on-screen buttons.
- **Player's-eye review** (section 6): a read of the UI, Art, Audio and `GameBootstrap` code. Nothing in it was seen on a screen by QA; items that need a human look are marked.
- **Not covered:** Shift vision in the running game (the legacy `Input` class cannot be simulated from a test, so the smoke test never holds Shift; the Shift logic itself is covered by the EditMode tests), keyboard input, and the WebGL build in a browser.
- The tests are written against the data, not against the current 13 customers, so they keep working after a data edit or a translation.

## 2. Test inventory

120 EditMode cases in 7 fixtures, 2 PlayMode cases in 1 fixture.

| File | What it covers | Cases |
|---|---|---|
| `DataConsistencyTests.cs` | Customer count, unique ids, arrival order, enum-like strings, anomaly vocabulary, `staresBack` ⇄ `staring`, hex colours, `repeatOf`, special button, `instantEnd` limit, every text key present, required `ui.*` / `intro.note` / `ending.*` keys (including the four extra keys the UI code uses), `item.*`, no empty or duplicate strings, rules used and existing, balance values, design invariants | 23 |
| `CustomerDecisionTests.cs` | Per customer: correct action (13); per customer and wrong button: mistake or instant end (17); special button offered only when it exists (13) | 43 |
| `NoticedMeterTests.cs` | Three fill rates, drain, clamps, huge delta, carry-over, full meter = `Seen` at once, meter during feedback, total Shift seconds, `IsShiftActive` / `IsStaredAt` | 11 |
| `MistakesAndEndingsTests.cs` | Mistake counter, `Fired` only after Continue, max−1 mistakes then `Morning`, `Morning` / `Changed` on each side of the threshold, `Seen`, instant ends, `Fired` wins over `Changed` | 11 |
| `ClockTests.cs` | Clock = `arrivalMinute` at every customer, ends at 06:00, `FormatClock` (8 values) | 11 |
| `CallOrderTests.cs` | Start-up state, calls before `StartNight`, double `Decide`, `Advance` at the wrong time, `Special` without a button, calls after the end, `Reset()` mid-night, `StartNight` after a lost night | 10 |
| `FullNightPlaythroughTests.cs` | The bot plays whole nights: every ending reachable, each wrong button on each `instantEnd` customer, "always Sell" / "always Refuse" do not win | 11 |
| `PlayMode/GameSmokeTests.cs` | (1) Start → note → all customers with the correct button → `morning` ending → Play again → title → a second short run. (2) Wrong choices → `fired` → Play again → mistake marks and meter reset. Checks along the way: clock text, name and dialogue lines, feedback line, which buttons are shown, the Shift reminder in the hint line, customer and ending pictures, red mistake marks, exactly one Camera / AudioListener / EventSystem / Canvas, no error logs. Tries to save 5 screenshots to `Logs/screens/`; in batchmode none were written. | 2 |
| `TestSupport.cs`, `NightBot.cs` | Helpers | — |

## 3. Results

| Run | Result |
|---|---|
| **Unity 6000.6.9f1 batchmode, EditMode** (`Logs/editmode-results.xml`, real UI/Art/Audio code in the project, balance with `noticedFillPerSecondOther` 0.2 and `changedShiftSecondsThreshold` 35) | **120 of 120 passed**, 0 failed, 0 skipped, 0 compile errors. All 7 fixtures passed. |
| **Unity batchmode, PlayMode** (`GameSmokeTests`), run 1, before the UI fixes | **2 of 2 passed**, no error logs. Screenshots were not written: `Logs/screens/` was created but stayed empty (batchmode limitation). |
| PlayMode run 2, after the UI fixes for B1 and B2 | 0 of 2: both failed on a test assertion that was out of date ("the Shift hint is hidden while Shift is not held"), not on a game defect. EditMode in the same run: 120 of 120. |
| PlayMode run 3, with the assertion updated to the new behaviour | **PENDING — to be filled from the results XML** |
| Compile check with Unity's Roslyn (`-langversion:9.0`): the real runtime sources, then both test assemblies against Unity's `nunit.framework.dll`, `UnityEngine.TestRunner.dll` and `UnityEngine.UI.dll` | 0 errors, 0 warnings |
| EditMode tests outside Unity (.NET 8, NUnit 3.14, stand-ins for `JsonUtility` and `Resources.Load`), rerun after adding four UI keys to the required list | 120 of 120 passed |
| Mutation check outside Unity (Part 1, deliberately broken copy of the data) | 14 tests failed, as intended |

The updated PlayMode test has been compiled (0 errors, 0 warnings) but not yet run. It finds objects by the names the UI and Art code give them (`TitleScreen/StartButton`, `Hud/Clock`, `KioskView/Shaken/Customer` and so on); if one of those names changes, the test fails with the path in the message.

## 4. Playthrough traces (from the Unity EditMode run)

Taken from the test output in `Logs/editmode-results.xml`. Columns: clock, customer, seconds of Shift held, Noticed right after looking, button, result, mistakes.

```
Playthrough: careful perfect play (2 s of Shift per customer, a glance at starers)
23:05  c01_student         shift 2.0s  noticed 0.20  Sell     correct  mistakes 0/3
23:40  c02_taxi            shift 2.0s  noticed 0.20  Sell     correct  mistakes 0/3
00:15  c03_auntie          shift 2.0s  noticed 0.20  Sell     correct  mistakes 0/3
00:50  c04_bekci           shift 2.0s  noticed 0.20  Sell     correct  mistakes 0/3
01:20  c05_courier         shift 2.0s  noticed 0.40  Refuse   correct  mistakes 0/3
01:50  c06_student_again   shift 2.0s  noticed 0.20  Sell     correct  mistakes 0/3
02:20  c07_cat             shift 2.0s  noticed 0.40  Special  correct  mistakes 0/3
02:50  c08_auntie_again    shift 2.0s  noticed 0.40  Refuse   correct  mistakes 0/3
03:20  c09_nightowl        shift 2.0s  noticed 0.20  Refuse   correct  mistakes 0/3
03:55  c10_bekci_fake      shift 2.0s  noticed 0.40  Refuse   correct  mistakes 0/3
04:25  c11_tall            shift 0.3s  noticed 0.20  Sell     correct  mistakes 0/3
04:55  c12_fisherman       shift 2.0s  noticed 0.20  Sell     correct  mistakes 0/3
05:30  c13_hasan           shift 2.0s  noticed 0.40  Refuse   correct  mistakes 0/3
06:00  ENDING: Morning  (mistakes 0/3, Shift held 24.3 s, noticed 0.00)

Playthrough: perfect decisions, heavy Shift use (3.5 s per customer, a glance at starers)
23:05  c01_student         shift 3.5s  noticed 0.35  Sell     correct  mistakes 0/3
23:40  c02_taxi            shift 3.5s  noticed 0.35  Sell     correct  mistakes 0/3
00:15  c03_auntie          shift 3.5s  noticed 0.35  Sell     correct  mistakes 0/3
00:50  c04_bekci           shift 3.5s  noticed 0.35  Sell     correct  mistakes 0/3
01:20  c05_courier         shift 3.5s  noticed 0.70  Refuse   correct  mistakes 0/3
01:50  c06_student_again   shift 3.5s  noticed 0.35  Sell     correct  mistakes 0/3
02:20  c07_cat             shift 3.5s  noticed 0.70  Special  correct  mistakes 0/3
02:50  c08_auntie_again    shift 3.5s  noticed 0.70  Refuse   correct  mistakes 0/3
03:20  c09_nightowl        shift 3.5s  noticed 0.35  Refuse   correct  mistakes 0/3
03:55  c10_bekci_fake      shift 3.5s  noticed 0.70  Refuse   correct  mistakes 0/3
04:25  c11_tall            shift 0.3s  noticed 0.20  Sell     correct  mistakes 0/3
04:55  c12_fisherman       shift 3.5s  noticed 0.35  Sell     correct  mistakes 0/3
05:30  c13_hasan           shift 3.5s  noticed 0.70  Refuse   correct  mistakes 0/3
06:00  ENDING: Changed  (mistakes 0/3, Shift held 42.3 s, noticed 0.00)

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
```

The other bots, last line only:

| Bot | Result |
|---|---|
| always presses a wrong button | `00:15  ENDING: Fired  (mistakes 3/3, Shift held 0.0 s, noticed 0.00)` |
| never lets go of Shift | `23:05  ENDING: Seen  (mistakes 0/3, Shift held 10.1 s, noticed 1.00)` |
| perfect decisions, but looks at the starer as long as at anyone (2 s) | `04:25  ENDING: Seen  (mistakes 0/3, Shift held 21.5 s, noticed 1.00)` |
| perfect play, then Refuse on c11_tall | `04:25  ENDING: Seen  (mistakes 0/3, Shift held 20.3 s, noticed 0.00)` |
| perfect play, then Sell on c13_hasan | `05:30  ENDING: Fired  (mistakes 0/3, Shift held 24.3 s, noticed 0.00)` |
| perfect play, then Special on c13_hasan | `05:30  ENDING: Fired  (mistakes 0/3, Shift held 24.3 s, noticed 0.00)` |
| always Refuse, never looks | `00:15  ENDING: Fired  (mistakes 3/3, Shift held 0.0 s, noticed 0.00)` |

## 5. Bugs and risks

No defect found in the data, `Scripts/Core` or `Scripts/Gameplay`. The player's-eye review produced the UI items below.

| # | Finding | Owner | Severity | Status |
|---|---|---|---|---|
| R1 | The `changed` ending had a narrow window (a 4 s look at any other ended the night as `seen`). | Game Designer | Medium | **Fixed**: `noticedFillPerSecondOther` 0.2, `changedShiftSecondsThreshold` 35. An other now takes 5 s to fill the meter; 3 s looks everywhere now add up to 36.3 s, over the threshold (by arithmetic). The Unity run confirms the 2 s bot gets `morning` and the 3.5 s bot gets `changed`. |
| R2 | GDD said 4–5 s of Shift per customer never fills the meter. | Game Designer | Low | Check that the GDD text matches the new numbers (4 s is now safe on an other, 5 s is not). |
| B1 | **No reminder of the Shift key during play.** The instruction was on the title screen only. | UI | Medium | **Fixed**: the hint line of the dialogue box is always visible while a customer is at the counter; it shows `ui.shiftHint` dimmed while Shift is not held and the customer's Shift hint in green while held. The smoke test now checks the reminder text. Confirmation: PlayMode run 3. |
| B2 | **A double press of Enter/Space could skip an ending.** | UI | Medium | **Fixed** in code: Enter/Space is ignored for 1.0 s after the ending appears and 0.3 s after a feedback line appears; button clicks are not delayed. Not covered by a test (keyboard input cannot be simulated). |
| B3 | Correct and wrong feedback differ by text colour (green / red), the sound and the red mistake mark, but there is no word or symbol. | UI | Low | Parked in `Docs/IDEAS_LATER.md` |
| B4 | The M key mutes on the title, note and ending screens too, but the sound label is only in the HUD. | UI | Low | Parked in `Docs/IDEAS_LATER.md` |
| R3 | PlayMode smoke test: run 3 pending after the assertion update. | Orchestrator (run) / QA | Medium until run | Open |
| R4 | Shift vision in the running game is not covered by any automated test (see Scope). It needs a human to hold Shift once on a human, an other and the starer. | Orchestrator / owner | Medium | Open |
| R5 | WebGL keyboard behaviour is unverified: Tab may move focus out of the canvas, Space may scroll the host page, and a Shift key held while the tab loses focus may stay "held". The notebook also has a clickable button, which covers the Tab case. | Build | Low to medium | Needs a browser check |
| R6 | `IGameSession.Decide` returns the previous decision when called at the wrong time; `GameBootstrap` checks `CanDecide` first, so it is harmless today. | Tech Lead / Gameplay | Info | — |

## 6. Player's-eye checklist

From reading the code and measuring the real longest strings against the box sizes. "Needs eyes" means the code looks right but only a person looking at the screen can confirm it.

| # | Question | Verdict | Notes |
|---|---|---|---|
| 1 | Is it clear what to do in the first 30 seconds? | Pass | Title: name, tagline, a boxed green "Hold SHIFT…" line, Start button with `[ENTER]`, a controls line. Then Hasan's note, then the first customer with buttons labelled `[1] Sell`, `[2] Refuse`. |
| 2 | Is the Shift instruction visible on the title screen? | Pass | Own bordered box in the accent colour, 20 px. The smoke test checks its text. |
| 3 | Is there a Shift reminder during play? | Pass (was Fail) | Fixed, see B1: a dim reminder sits in the hint line whenever Shift is not held. |
| 4 | Is the notebook discoverable? | Pass | A "TAB: notebook" button is always in the HUD, the controls line and the note mention it. Tab in a browser: see R5. |
| 5 | Can any real string overflow its box? | Pass, needs eyes | Every long text uses best-fit with truncation as the last resort. Longest real strings: greeting + request 125 characters in a 584 × 76 box (about 3 lines of 16 px); feedback 85; Shift hint 79 on one 584 px line; rule 105 in a 772 × 35 slot (2 lines); ending body 271 in 720 × 108; note 422 in 584 × 276. All fit at full size by estimate. Fixed-size texts (name, tagline, HUD labels, clock) are short. A Turkish translation would need this re-checked, mainly the HUD labels (80 and 128 px wide, no best-fit). |
| 6 | Does the Shift view visibly change the screen? | Pass, needs eyes | Other-side background palette, purple tint, darker vignette, shake that grows with the meter, faster light flicker. |
| 7 | Does the Shift view visibly change the customer? | Pass, needs eyes | The figure is recoloured and a shadow appears on the counter (shadows exist only in the Shift view, as rule 1 says). All ten anomalies are handled by the painters. Whether "no shadow" and "no whistle" read clearly at this pixel size needs a look. |
| 8 | Is the customer's Shift hint shown only while Shift is held? | Pass | `DialoguePanel.RefreshHint` shows the customer's hint only while Shift is active, otherwise the reminder; the smoke test checks the line without Shift. |
| 9 | Are correct and wrong decisions clearly distinguishable? | Pass, with B3 | Green or red feedback line, cash register / refuse / error sound, red mistake mark. |
| 10 | Does every ending show its own title and body? | Pass | Four title/body pairs, title colour per ending, own picture and stinger. Smoke test checks `morning` and `fired`; the data test checks all eight keys. |
| 11 | Does Play again fully reset UI, kiosk view and audio? | Pass | Session reset and republished (clock, meter, mistakes, Shift off); UI back to title with notebook closed; kiosk hides customer and ending picture; audio stops drone, heartbeat and stinger, keeps hum and mute. Smoke test checks the UI and kiosk part. |
| 12 | Does anything play sound before the Start click? | Pass | Every play call is behind the `started` flag, set in `BeginAfterUserGesture`. |
| 13 | Per-frame allocations or texture leaks? | Pass | Sprites and textures are cached in `SpriteFactory` by look, so a restart creates nothing new; customer sprites are painted once on first arrival. Audio clips are built once in `Init`. No allocations found in the `Tick` paths. |
| 14 | Anything that would break in WebGL? | Pass, needs eyes | No threads, `System.IO`, `Shader.Find`, `PlayerPrefs` or streaming audio in the runtime code; clips use `AudioClip.Create` + `SetData`, the font is the built-in one. Keyboard behaviour in the browser: see R5. |
| 15 | Can the ending be skipped by accident? | Pass (was Fail), needs eyes | Fixed, see B2: a 1.0 s keyboard delay on the ending screen. |
