# ARCHITECTURE — Night Shift at the Büfe

Everything is in `namespace NightShift`, one runtime assembly (`Assets/Game/Scripts/NightShift.asmdef`).
The interfaces in `Scripts/Core/` are the contract: their comments are part of the spec. Read your interface file first.

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
