# Night Shift at the Büfe — Game Design Document

Game Off 2026, theme: **SHIFT**. Data contract: `Docs/DATA_SCHEMA.md`. All numbers and text below live in `Assets/Game/Resources/Data/*.json`; if this document and the JSON disagree, the JSON wins.

## Pitch

You work the night shift (23:00 to 06:00) at a neighbourhood büfe in İzmir. People come for tost, ayran, simit, cigarettes. Some of them are not people. The previous worker left a notebook of rules. Hold **Shift** to see the other side of the shop and what your customers really are, but the other side can see you too. The theme is used three ways: the work shift, the Shift key, and what the shift does to you.

Tone: quiet, dry, uncanny, a little funny. No gore. One screen, 13 customers, 10–15 minutes.

## Core loop

1. A customer arrives; the clock jumps to their `arrivalMinute`. They greet and ask for an item.
2. The player gathers information: the clock, the face (seen before?), the notebook (Tab), and, at a cost, the Shift view.
3. The player decides: **Sell (1) / Refuse (2) / special (3)**, the third only when the customer has a `specialActionKey`.
4. Feedback line. Correct: nothing. Wrong: +1 mistake, or for two customers the night ends at once.
5. Next customer. After the 13th, the night ends at 06:00.

## Shift and the Noticed meter

Holding Shift tints the screen, applies each customer's `shiftAnomalies`, and shows their one-line `shiftHintKey`. While held, the Noticed meter (0..1) fills; released, it drains. It carries over between customers. At 1.0: ending `seen`.

| balance.json | Value | Meaning |
|---|---|---|
| `noticedFillPerSecond` | 0.10 | Ordinary human or empty counter: 10 s from empty to full |
| `noticedFillPerSecondOther` | 0.25 | `isOther`: 4 s |
| `noticedFillPerSecondStare` | 0.67 | `staresBack`: 1.5 s |
| `noticedDrainPerSecond` | 0.075 | Full to empty in 13.3 s |
| `maxMistakes` | 3 | Third mistake = `fired` |
| `changedShiftSecondsThreshold` | 40.0 | Total Shift seconds above this = `changed` |
| `shiftLengthMinutes` | 420 | 23:00 to 06:00 |

Why these numbers:

- A careful look is 1–2 s. On a human that costs 0.10–0.20, on an other 0.25–0.50. Reading the dialogue and deciding takes 15 s or more with Shift released, which drains about 1.1, so a careful player starts every customer near zero. The meter punishes lingering, not looking.
- The starer fills from empty in 1.5 s. A half-second glance costs 0.33 and is survivable; a normal 2 s read is not. That is rule 6 expressed in numbers. Arriving with a part-filled meter makes it tighter, which rewards discipline on the customer before.
- The careful total is about 2 s x 13 = 26 s. A player who holds Shift about 4–5 s per customer reaches 52–65 s without ever filling the meter (4 s on a human is +0.4 and drains away during the dialogue). 40 s sits between the two, so `changed` is the ending for the habit of looking, and `seen` for a single long stare.

## The notebook (rules.json)

Readable at any time with Tab. Seven rules, in this order:

| id | Shift? | Text |
|---|---|---|
| `r1_shadow` | yes | No shadow, no sale. Shadows only show on the other side, so hold Shift and look at the floor. |
| `r2_ayran` | no | After 03:00 the ayran is not ours. Whoever asks, say we are out. Do not open the fridge to check. |
| `r3_bekci` | yes | If someone says he is the bekçi, look with Shift. A real bekçi keeps his whistle. No whistle: refuse. |
| `r4_repeat` | no | Same face twice in one night must want the same thing as before. Different order: refuse. |
| `r5_cat` | no | Cats stay outside. If a 'cat' comes in anyway, give it cat food. Nothing else, whatever it points at. |
| `r6_stare` | yes | If something looks back at you from the other side, let go of Shift at once. Then sell. Do not refuse it. |
| `r7_keys` | no | Never hand over the keys. Not to the bekçi, not to the owner, not to me if I come back. |

Rule 6 carries its own decision (sell), so the starer is solvable from the notebook after a glance. Rule 7 exists to give the third button a meaning other than "the right answer": the keys button is never correct.

## Customers (customers.json)

| # | Time | id | Who | Wants | Normal look | Shift look | Rule | Correct | If wrong |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 23:05 | `c01_student` | Student | tost | Hoodie, glasses | Nothing odd | none | sell | mistake |
| 2 | 23:40 | `c02_taxi` | Taxi driver | ayran | Cap, jacket | Nothing odd | r2 | sell | mistake |
| 3 | 00:15 | `c03_auntie` | Auntie | çekirdek | Grey bun, scarf | Nothing odd | none | sell | mistake |
| 4 | 00:50 | `c04_bekci` | Bekçi (real) | cigarettes | Uniform, whistle | Whistle still there | r3 | sell | mistake |
| 5 | 01:20 | `c05_courier` | Courier | water | Red hoodie, bag | No shadow | r1 | refuse | mistake |
| 6 | 01:50 | `c06_student_again` | Student, back | tost | Same as #1 | Nothing odd | r4 | sell | mistake |
| 7 | 02:20 | `c07_cat` | "Cat" | paws at cigarettes | Orange cat | Far too tall | r5 | special (cat food) | mistake |
| 8 | 02:50 | `c08_auntie_again` | "Auntie", back | matches | Same as #3 | Reversed shadow, wrong eyes | r4 | refuse | mistake |
| 9 | 03:20 | `c09_nightowl` | Night owl, drunk | ayran | Pink hair, black tee | Nothing odd | r2 | refuse | mistake |
| 10 | 03:55 | `c10_bekci_fake` | Bekçi (fake) | cigarettes | Uniform, whistle | Whistle gone | r3 | refuse | mistake |
| 11 | 04:25 | `c11_tall` | Tall customer | matches | Pale, dark coat | Bright eyes on you; has a shadow | r6 | sell | **instant end: seen** |
| 12 | 04:55 | `c12_fisherman` | Fisherman | simit | Cap, green jacket | Nothing odd | none | sell | mistake |
| 13 | 05:30 | `c13_hasan` | Hasan abi | the keys (no item) | Plain, like you | Hollow silhouette | r7 | refuse | **instant end: fired** |

Totals: 7 sell, 5 refuse, 1 special. 7 humans, 6 others.

How the requirements are met:

- **Shift is genuinely required.** Customers 1, 2, 3, 6, 9, 12 and the courier (5) all look like ordinary people; only the floor under Shift tells the courier apart. The two bekçis say the same lines, ask for the same thing and both wear a whistle in normal view; they have different faces, so rule 4 cannot be used to shortcut rule 3.
- **Returning pair.** The student returns honestly (same order, sell). The auntie returns asking for matches instead of çekirdek (refuse). The second is checkable in normal view; Shift only confirms it.
- **Ayran.** 23:40 sell, 03:20 refuse. The 03:20 customer is a plain human: not every rule is about spotting an other.
- **Special button.** Appears on the cat (cat food, correct) and as a "Hand over the keys" decoy on both bekçis and on Hasan, where it is always wrong.
- **No blanket strategy.** "Refuse everyone late" fails on 11 (ends the night) and 12. "Sell to everyone" collects mistakes at 5, 7 and 8 and is fired before 03:00. "Never use Shift" fails at 5 and 10 at least and guesses at 4.
- **`ruleId` convention.** Set when a customer triggers a rule's condition, including when they pass it (2, 4, 6). Empty for plain sales. Every customer is implicitly subject to rule 1.

## Endings

| id | Title | Condition |
|---|---|---|
| `morning` | Morning | All 13 served, fewer than 3 mistakes, total Shift time at most 40 s |
| `changed` | The shift changed you | Same, but total Shift time above 40 s. In the final scene the player has no shadow. |
| `seen` | It saw you | Noticed meter reached 1.0, or refused the tall customer (#11) |
| `fired` | You're fired | 3 mistakes, or said yes to Hasan (#13), by Sell or by the keys button |

## Difficulty curve

- **Tutorial (1–3, 23:05–00:15).** Three ordinary humans. The student's greeting ("Hasan abi always squinted at me") invites a first, safe Shift. The taxi driver teaches that ayran is fine before 03:00. The auntie foreshadows the cat and sets up her own return.
- **Learning the rules (4–7).** One new rule each: real bekçi (look, then sell), first other (no shadow), honest return, the cat and the third button.
- **Rules collide (8–10).** The false return, the first ayran after 03:00, the fake bekçi. By now the player has seen the "safe" version of each.
- **Late night (11–13).** The starer punishes the habit of long looks built up over the night and is the first instant loss. The fisherman is a breather and a trap for the paranoid. Hasan closes the night on the simplest rule and the strongest temptation.

Pacing: about 45–60 s per customer, 10–13 minutes in total.

## Out of scope

- Inventory, money, prices, change, stock counts, or actually preparing the tost.
- Player movement; the camera never leaves the counter.
- Randomised or procedurally generated customers; the night is a fixed script.
- Branching dialogue, player dialogue choices, or more than one request per customer.
- More than one night, saves, difficulty settings, or unlockables.
- Voice acting; Turkish localisation (strings.json is ready for it, but it is not shipped).
- Gore, jump scares, or chase sequences.
- Rule changes mid-night or rules that contradict each other.
- Mouse-only or gamepad control schemes, rebinding, mobile layout.
