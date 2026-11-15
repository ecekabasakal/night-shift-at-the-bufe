# ARCHITECTURE — Night Shift at the Büfe

Everything is in `namespace NightShift`, one runtime assembly (`Assets/Game/Scripts/NightShift.asmdef`).
The interfaces in `Scripts/Core/` are the contract: their comments are part of the spec. Read your interface file first.

**Version 2 (gevrek rule, street cat, shift handover): see §8 "v2 changes" at the end — it lists every new member per owner. §1–§7 still hold.**

## 1. Systems and owners

| System | Folder | Owner | What it does |
|---|---|---|---|
| Data models, `GameData`, `DataParsing`, `Anomalies` | `Core/` | Tech Lead | JSON classes (mirror `DATA_SCHEMA.md`), text lookup `Get(key)`, `FormatClock`, enum/colour parsing. |
| `GameState`, `EventBus`, `GameLayout` | `Core/` | Tech Lead | Phase machine (Intro → Serving → Ending), static events, shared layout numbers. |
| `GameBootstrap` | `Core/` | Tech Lead | The only scene object. Creates camera + systems, **reads the Shift key, ticks the session, owns the whole game flow**, raises every EventBus event, drives the UI. |
| `GameSession` | `Gameplay/` | Gameplay | The rules of the night as pure C# (no MonoBehaviour / Time / Input / EventBus). Testable by calling methods. |
| `UIController` | `UI/` | UI | Canvas, EventSystem, all screens, HUD, notebook; reads keys 1/2/3/Tab/Enter/Space/M. Passive view. |
| `SpriteFactory`, `KioskView` | `Art/` | Art | Procedural sprites; the world picture (background, customer, Shift effect, animations, ending picture). |
| `AudioController` | `Audio/` | Audio | Procedural clips, AudioSources, hum / drone / heartbeat / one-shots, mute. |

Two communication styles, on purpose:
- **UI ⇄ Bootstrap: direct.** Bootstrap calls `IUIController` methods; the UI answers with its own C# events. The UI never subscribes to `EventBus` and never touches the session.
- **Bootstrap → Art, Audio: `EventBus`.** `KioskView` and `AudioController` subscribe inside `Init()` and react (table in §3). Bootstrap calls on them directly only: `Init`, `Tick`, and for audio `BeginAfterUserGesture` / `SetMuted`.

## 2. Concrete classes Bootstrap creates (names and signatures are fixed)

| Class | File | Base / implements | Created by Bootstrap as |
|---|---|---|---|
| `GameSession` | `Assets/Game/Scripts/Gameplay/GameSession.cs` | plain class : `IGameSession` | `new GameSession(GameData data)` |
| `SpriteFactory` | `Assets/Game/Scripts/Art/SpriteFactory.cs` | plain class : `ISpriteFactory` | `new SpriteFactory()` |
| `KioskView` | `Assets/Game/Scripts/Art/KioskView.cs` | `MonoBehaviour, IKioskView` | `AddComponent<KioskView>()` then `public void Init(ISpriteFactory sprites, IShiftVision vision)` |
| `AudioController` | `Assets/Game/Scripts/Audio/AudioController.cs` | `MonoBehaviour, IAudioController` | `AddComponent<AudioController>()` then `public void Init(IShiftVision vision)` |
| `UIController` | `Assets/Game/Scripts/UI/UIController.cs` | `MonoBehaviour, IUIController` | `AddComponent<UIController>()` then `public void Init(GameData data, ISpriteFactory sprites)` |

Rules for the three MonoBehaviours: do all set-up in `Init` (create child GameObjects under your own transform); **no `Update()`** — per-frame work goes in `Tick(float deltaSeconds)`, which Bootstrap calls; no `Start()`. Extra helper classes in your own folder are fine. Do not use the type names `Input`, `Time`, `Event`, `Random`, `Object`, `Debug`, `Screen`, `Application`, `Camera`, `SceneView`.

Who creates what: Bootstrap creates the camera **and the only `AudioListener`**. `UIController.Init` creates the Canvas **and the `EventSystem` + `StandaloneInputModule`**. `KioskView.Init` shows the normal background immediately (same picture as after `ResetView`). After `Init` the UI shows nothing until Bootstrap calls `ShowTitle()`.

## 3. Events (`EventBus`) — all raised by `GameBootstrap` only

| Event | Payload | Raised when | KioskView must | AudioController must |
|---|---|---|---|---|
| `PhaseChanged` | `GamePhase` | every phase change (also the first Intro) | — | — (optional) |
| `GameStarted` | — | Start clicked (after `BeginAfterUserGesture`) | — | — (hum already started) |
| `CustomerArrived` | `CustomerData` | a customer steps up | `ShowCustomer(c)` | `PlayDoorBell()`; also `PlayWhistle()` if `c.appearance.accessory == "whistle"` |
| `CustomerLeft` | `CustomerData` | Continue pressed after feedback | `HideCustomer()` | — |
| `ShiftChanged` | `bool active` | Shift vision turns on / off | `SetShiftView(active)` | `SetShiftDrone(active)` |
| `NoticedChanged` | `float` 0..1 | meter value changed | — (or poll `IShiftVision`) | — (or poll `IShiftVision`) |
| `ClockChanged` | `int` minutes after 23:00 | clock changed | — (optional) | — |
| `DecisionMade` | `DecisionResult` (`Action`, `Correct`, `FeedbackKey`, `EndsNight`) | player decided | — (optional reaction) | correct+Sell → `PlayCashRegister()`; correct+Refuse/Special → `PlayRefuse()`; wrong → `PlayError()` |
| `MistakesChanged` | `int mistakes, int max` | counter changed | — | — |
| `EndingReached` | `EndingId` | night over | `ShowEnding(id)` | `SetShiftDrone(false)`, `PlayEndingStinger(id)` |
| `RestartRequested` | — | Play again clicked | `ResetView()` | stop drone/heartbeat, keep hum and mute state |

The UI gets the same information through direct calls (`SetNoticed`, `SetClock`, `SetMistakes`, `SetShiftView`, `ShowCustomer`, `ShowFeedback`, `ShowEnding`).

UI → Bootstrap events (`IUIController`): `StartClicked`, `ContinueClicked` (intro note and feedback), `ActionChosen(PlayerAction)`, `PlayAgainClicked`, `MuteClicked`. The notebook (Tab) is internal to the UI; while it is open the UI does not raise `ActionChosen`. Bootstrap ignores intents that do not fit the current state, so a stray event is harmless.

## 4. Frame order (`GameBootstrap.Update`)

1. `dt = min(Time.deltaTime, 0.1)`
2. `ui.Tick(dt)` — keys/clicks become intent events, handled synchronously by Bootstrap.
3. If phase is Serving: read Shift (`LeftShift || RightShift`) → `session.Tick(dt, shiftHeld)` → `PublishState()` → if `session.IsFinished` → ending.
4. `kiosk.Tick(dt)`
5. `audioController.Tick(dt)`

`PublishState()` compares the session's `IsShiftActive`, `Noticed`, `Mistakes`, `ClockMinute` with the last announced values and, for each change, calls the UI setter and raises the event. It runs after every call that can change the session.

## 5. Game flow

1. **Boot** (`Awake`): `EventBus.Clear()` → load `GameData` → camera → `GameSession`, `SpriteFactory`, `KioskView`, `AudioController`, `UIController` → `EnterIntro()`: `session.Reset()`, phase Intro, `PublishState()`, `ui.ShowTitle()`.
2. **Start clicked**: `audio.BeginAfterUserGesture()` → `GameStarted` → `ui.ShowIntroNote()`.
3. **Continue on the note**: `session.StartNight()` → phase Serving → `PublishState()` (clock) → `CustomerArrived` → `ui.ShowCustomer(c)`.
4. **Each frame**: Shift held fills the meter (`ShiftChanged`, `NoticedChanged`); meter full → step 7 with `seen`.
5. **Action chosen** (only if `session.CanDecide(action)`): `session.Decide` → `DecisionMade` → `PublishState()` (mistakes) → `ui.ShowFeedback(key, correct)`.
6. **Continue on feedback**: `CustomerLeft` → `session.Advance()` → `PublishState()` → next customer (as step 3's last two calls) or step 7.
7. **Ending**: `PublishState()` (Shift turns off, clock) → phase Ending → `EndingReached(id)` → `ui.ShowEnding(id)`.
8. **Play again**: `RestartRequested` → `EnterIntro()` (step 1's last part). Back at the title screen.

**Play again resets like this:** nothing is destroyed or rebuilt. Systems stay subscribed to `EventBus`. `session.Reset()` zeroes the logic (so polled `Noticed` is 0 again), `PublishState()` pushes the zeroed values to UI and events, `KioskView`/`AudioController` reset themselves on `RestartRequested`, and `ui.ShowTitle()` hides every other screen. The mute setting is kept. `EventBus.Clear()` runs only in Bootstrap `Awake`/`OnDestroy`.

## 6. Coordinates and layout (`GameLayout`)

- Logical screen **960 × 540**, origin bottom-left. World: **100 logical px = 1 unit**, orthographic camera at (0,0,−10), **size 2.7** → visible world 9.6 × 5.4 units centred on (0,0). `GameLayout.LogicalToWorld(x, y)` converts.
- Pixel art: **1 art pixel = 4 logical px → world sprites use `pixelsPerUnit = 25`**, `FilterMode.Point`. Background / vignette / ending picture: 240 × 135 art px, pivot centre, at world (0,0). Customer: 64 × 80 art px canvas (256 × 320 logical), pivot bottom-centre, shadow drawn inside the canvas, feet at logical **(480, 168)** → top at y 488.
- Sorting orders: background 0, customer 10, Shift tint 30, vignette 40, ending picture 50. The UI canvas (Screen Space – Overlay) is always on top.
- UI canvas: `CanvasScaler` Scale With Screen Size, reference 960 × 540, match 0.5. Font: `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`.

| Region (logical px) | Used by |
|---|---|
| y 492–540, full width | HUD strip: clock (left), Noticed meter (centre), mistakes + mute (right) |
| y 152–492, full width | **World band — UI keeps it empty during Serving** (customer + shadow must stay visible). Only the notebook overlay may cover it. |
| y 0–152, x 0–624 | Dialogue box: name, greeting + request lines, and the Shift hint line (customer's `shiftHintKey`, only while Shift is active) |
| y 0–152, x 624–960 | Action buttons (Sell / Refuse / Special), replaced by Continue during feedback |
| Title, intro note, ending | Full-screen UI allowed; the ending screen should leave the upper half see-through so the ending picture shows |

## 7. Game rules (for `GameSession` and tests)

Exact behaviour is in the comments of `IGameSession.cs`. Summary: `Tick` fills/drains `Noticed` only in `AwaitingDecision` / `ShowingFeedback`; `Noticed ≥ 1` finishes at once with `Seen`. `Decide` → `ShowingFeedback`; wrong + `mistake` adds a mistake (`Fired` pending when `Mistakes == MaxMistakes`); wrong + `instantEnd` leaves mistakes unchanged and makes the customer's `instantEndingId` pending. `Advance` applies the pending ending, or moves to the next customer, or finishes with `Changed` (`TotalShiftSeconds > changedShiftSecondsThreshold`) / `Morning`.

A test can play a night without Unity objects:
`var s = new GameSession(GameData.FromJson(c, r, st, b)); s.StartNight(); s.Tick(0.5f, true); s.Decide(PlayerAction.Sell); s.Advance(); ...`

## 8. v2 changes (gevrek rule, street cat, shift handover)

Additive only: nothing in §1–§7 changed its name, signature or order. New Core pieces: `CustomerData.catReacts`, `GameLayout.SortStreetCat = 5`, `HandoverNotes.cs` (`INoteStore`, `PlayerPrefsNoteStore`, `MemoryNoteStore`, `HandoverNotes`), `GameBootstrap.NoteStoreOverride`. No new EventBus event.

### 8.1 Feature 1 — gevrek / simit
Data only. No runtime code knows the item list (`requestItem` is a free string; `r8_gevrek` is decided by `correctAction` like every rule). The notebook must cope with one more rule (it already divides the space by `data.Rules.Length`) **plus** the previous-note block (8.3).

### 8.2 Feature 2 — street cat
Trigger: the existing `CustomerArrived(CustomerData c)` event with `c.catReacts == true`. Bootstrap does nothing extra.

| On | KioskView must | AudioController must |
|---|---|---|
| `CustomerArrived(c)`, `c.catReacts` | `PlayCatAlarm()` (after `ShowCustomer(c)`) | `PlayDoorBell()` as before, then `PlayCatHiss()` so that the hiss becomes audible **about 0.35 s after the bell starts** (timer in `Tick`, or silence at the head of the clip) — it must not mask the bell. If the customer also has the whistle, the hiss comes after the whistle has started. |
| `CustomerArrived(c)`, `!c.catReacts` | cat stays calm (cancel a running alarm) | no hiss |
| `CustomerLeft`, `EndingReached`, `RestartRequested` | alarm ends at once (inside `HideCustomer` / `ShowEnding` / `ResetView`) | a hiss that has not started yet is cancelled |

The alarm also ends by itself about 1.5 s after it started. It must be visible in **both** looks: if Shift is pressed or released during the alarm, the overlay switches between `CreateStreetCatAlarmFrames(false)` and `(true)` like the background does. The calm cat is painted into the background (`BackgroundPainter.PaintStreetCat`), so the alarm is an **overlay**: a full-canvas transparent sprite at the background's position, child of the shaken transform, `sortingOrder = GameLayout.SortStreetCat` (above background 0, below customer 10), that completely covers the calm cat.

### 8.3 Feature 3 — shift handover
Flow (Bootstrap owns it):
1. **Boot**: `noteStore = GameBootstrap.NoteStoreOverride ?? new PlayerPrefsNoteStore()`; after the UI is created: `ui.SetPreviousNote(HandoverNotes.NoteText(data, noteStore.Load()))`.
2. **Ending** (§5 step 7): unchanged up to `EndingReached(id)`. Then `Seen` / `Fired` → `ui.ShowEnding(id)` exactly as before. `Morning` / `Changed` → `ui.ShowEndingWithHandover(id)`; Bootstrap ignores `PlayAgainClicked` until a note is chosen.
3. **`HandoverNoteChosen(index)`**: `noteStore.Save(HandoverNotes.OptionKey(index))` → `ui.SetPreviousNote(text)` → `ui.ShowHandoverDone()`. Play again then works as before; the next night in the same session already shows the note in the notebook.

`HandoverNotes` (pure, static): `OptionCount` (3), `OptionKey(int index)` → `"handover.option1..3"`, `IsGoodEnding(EndingId)`, `IsValidKey(GameData, string)` (starts with `handover.` and `data.Has(key)`), `NoteText(GameData, string)` (text or `""`), constants `PromptKey`, `DoneKey`, `HeadingKey`. `PlayerPrefsNoteStore.PrefsKey = "nightshift.handoverNote"`.

### 8.4 To-do per owner (exact signatures)

**Gameplay** — nothing. `GameSession` is untouched.

**UI** (`UIController : IUIController`, plus `EndingScreen` / `NotebookPanel`)
- `public event Action<int> HandoverNoteChosen;` — index 0..2; from the three note buttons and from keys 1 / 2 / 3 (also keypad) while they are visible. Use the same one-intent-per-frame guard as the other intents.
- `public void ShowEndingWithHandover(EndingId ending)` — as `ShowEnding` (same `EndingScreen/Title`, `EndingScreen/Body` objects and texts) plus a prompt (`ui.handoverPrompt`) and three buttons with the texts of `HandoverNotes.OptionKey(0..2)`, each prefixed with its key number. `PlayAgainButton` is hidden; Enter / Space do nothing.
- `public void ShowHandoverDone()` — hides prompt + note buttons, shows `ui.handoverDone` and `PlayAgainButton` (Enter / Space work again, after the usual confirm delay).
- `public void SetPreviousNote(string noteText)` — notebook: after the last rule, heading `ui.previousNote` and the text; `""` hides both. Callable at any time.
- `ShowEnding(id)` must keep showing the plain ending (no prompt, no note buttons, Play again visible) even if a handover screen was shown in an earlier night.
- Fixed object names (the PlayMode test finds them by path): `EndingScreen/HandoverPrompt` (Text), `EndingScreen/HandoverButton1`, `EndingScreen/HandoverButton2`, `EndingScreen/HandoverButton3` (Buttons), `EndingScreen/HandoverDone` (Text), `Notebook/Paper/PreviousNoteHeading`, `Notebook/Paper/PreviousNote` (Texts; inactive when there is no note). Existing names stay.
- Notes are up to 110 characters: the ending panel may grow upwards on good endings, but keep some of the ending picture visible.

**Art** (`SpriteFactory : ISpriteFactory`, `KioskView : IKioskView`)
- `public Sprite[] CreateStreetCatAlarmFrames(bool shiftView)` — ≥ 2 frames, each 240 × 135 art px, pivot centre, PPU 25, transparent except the puffed-up hissing cat, which fully covers the calm cat in the background; cached like the other frames.
- `public void PlayCatAlarm()` — starts the reaction described in 8.2; object name `StreetCatAlarm` under `Shaken`.
- `CustomerArrived` handler: `ShowCustomer(c)`, then `PlayCatAlarm()` if `c.catReacts`. `HideCustomer`, `ShowEnding`, `ResetView` stop the alarm.

**Audio** (`AudioController : IAudioController`)
- `public void PlayCatHiss()` — a short procedural hiss (filtered noise), quieter than the bell; silent before `BeginAfterUserGesture` like every other sound.
- `CustomerArrived` handler: timing as in 8.2; pending hiss cancelled on `CustomerLeft`, `EndingReached`, `RestartRequested`.

**QA**
- `DataConsistencyTests.Items` hard-codes the v1 item list: add `gevrek`, `boyoz`, `kumru`; add the `catReacts` invariant and the new required string keys.
- New pure tests: `HandoverNotes` (option keys, good endings, valid / invalid / missing key → `""`) and `MemoryNoteStore`.
- PlayMode: set `GameBootstrap.NoteStoreOverride = new MemoryNoteStore()` **before** creating the `GameBootstrap` object and back to `null` in teardown, so tests never write the developer's PlayerPrefs. `CleanNight_ReachesMorning_...` must now click `EndingScreen/HandoverButton1..3` before `PlayAgainButton` (which is inactive until then), and can check the notebook texts in the following night.
