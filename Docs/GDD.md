# Night Shift at the Büfe — Game Design Document

Game Off 2026, theme: **SHIFT**. Data contract: `Docs/DATA_SCHEMA.md`. All numbers and text below live in `Assets/Game/Resources/Data/*.json`; if this document and the JSON disagree, the JSON wins.

## Pitch

You work the night shift (23:00 to 06:00) at a neighbourhood büfe in İzmir. People come for tost, ayran, gevrek, cigarettes. Some of them are not people. The previous worker left a notebook of rules. Hold **Shift** to see the other side of the shop and what your customers really are, but the other side can see you too. The theme is used three ways: the work shift, the Shift key, and what the shift does to you.

Tone: quiet, dry, uncanny, a little funny. No gore. One screen, 14 customers, 10–15 minutes.

## Core loop

1. A customer arrives; the clock jumps to their `arrivalMinute`. They greet and ask for an item.
2. The player gathers information: the clock, the face (seen before?), the word they use, the cat outside, the notebook (Tab), and, at a cost, the Shift view.
3. The player decides: **Sell (1) / Refuse (2) / special (3)**, the third only when the customer has a `specialActionKey`.
4. Feedback line. Correct: nothing. Wrong: +1 mistake, or for two customers the night ends at once.
5. Next customer. After the 14th, the night ends at 06:00. On a good ending the player leaves a note for the morning shift (see Shift handover).

## Shift and the Noticed meter

Holding Shift tints the screen, applies each customer's `shiftAnomalies`, and shows their one-line `shiftHintKey`. While held, the Noticed meter (0..1) fills; released, it drains. It carries over between customers. At 1.0: ending `seen`.

| balance.json | Value | Meaning |
|---|---|---|
| `noticedFillPerSecond` | 0.10 | Ordinary human or empty counter: 10 s from empty to full |
| `noticedFillPerSecondOther` | 0.20 | `isOther`: 5 s |
| `noticedFillPerSecondStare` | 0.67 | `staresBack`: 1.5 s |
| `noticedDrainPerSecond` | 0.075 | Full to empty in 13.3 s |
| `maxMistakes` | 3 | Third mistake = `fired` |
| `changedShiftSecondsThreshold` | 35.0 | Total Shift seconds above this = `changed` |
| `shiftLengthMinutes` | 420 | 23:00 to 06:00 |

Why these numbers:

- A careful look is 1–2 s. On a human that costs 0.10–0.20, on an other 0.20–0.40. Reading the dialogue and deciding takes 15 s or more with Shift released, which drains about 1.1, so a careful player starts every customer near zero. The meter punishes lingering, not looking.
- The starer fills from empty in 1.5 s. A half-second glance costs 0.33 and is survivable; a normal 2 s read is not. That is rule 6 expressed in numbers. Arriving with a part-filled meter makes it tighter, which rewards discipline on the customer before.
- The careful total is about 2 s x 14 = 28 s. Passing 35 s needs an average above 2.5 s per customer. A player who habitually holds Shift 3–4 s per customer reaches 42–56 s and survives: 4 s costs +0.4 on a human and +0.8 on an other, and both drain away during the dialogue. Holding 5 s on an other (or 1.5 s on the starer) fills the meter from empty, so the habit has a ceiling. 35 s sits between careful and habitual play, so `changed` is the ending for the habit of looking, and `seen` for a single long stare.

## The notebook (rules.json)

Readable at any time with Tab. Eight rules, in this order:

| id | Shift? | Text |
|---|---|---|
| `r1_shadow` | yes | No shadow, no sale. Shadows only show on the other side, so hold Shift and look at the floor. |
| `r2_ayran` | no | After 03:00 the ayran is not ours. Whoever asks, say we are out. Do not open the fridge to check. |
| `r3_bekci` | yes | If someone says he is the bekçi, look with Shift. A real bekçi keeps his whistle. No whistle: refuse. |
| `r4_repeat` | no | Same face twice in one night must want the same thing as before. Different order: refuse. |
| `r5_cat` | no | Cats stay outside. If a 'cat' comes in anyway, give it cat food. Nothing else, whatever it points at. |
| `r6_stare` | yes | If something looks back at you from the other side, let go of Shift at once. Then sell. Do not refuse it. |
| `r7_keys` | no | Never hand over the keys. Not to the bekçi, not to the owner, not to me if I come back. |
| `r8_gevrek` | no | Sesame bread ring: İzmir says gevrek, the rest of Turkey says simit. Anyone asking for 'simit' here: refuse. |

Rule 6 carries its own decision (sell), so the starer is solvable from the notebook after a glance. Rule 7 exists to give the third button a meaning other than "the right answer": the keys button is never correct. Rule 8 is a listening rule: it is decided by one word in the request line, with no Shift and no clock. Hasan's intro note now says gevrek, never simit.

After the last rule the notebook shows the note left by the previous player, if there is one (see Shift handover).

## Customers (customers.json)

| # | Time | id | Who | Wants | Normal look | Shift look | Cat hisses | Rule | Correct | If wrong |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 23:05 | `c01_student` | Student | tost | Hoodie, glasses | Nothing odd | no | none | sell | mistake |
| 2 | 23:35 | `c02_taxi` | Taxi driver | ayran | Cap, jacket | Nothing odd | no | r2 | sell | mistake |
| 3 | 00:05 | `c03_auntie` | Auntie | çekirdek | Grey bun, scarf | Nothing odd | no | none | sell | mistake |
| 4 | 00:35 | `c04_bekci` | Bekçi (real) | cigarettes | Uniform, whistle | Whistle still there | no | r3 | sell | mistake |
| 5 | 01:05 | `c05_courier` | Courier | kumru | Red hoodie, bag | No shadow | yes | r1 | refuse | mistake |
| 6 | 01:35 | `c06_student_again` | Student, back | tost | Same as #1 | Nothing odd | no | r4 | sell | mistake |
| 7 | 02:05 | `c06b_riser` | Early riser | "simit" | Blond, pale tee, big smile | Too many teeth, extra fingers | yes | r8 | refuse | mistake |
| 8 | 02:30 | `c07_cat` | "Cat" | paws at cigarettes | Orange cat | Far too tall | yes | r5 | special (cat food) | mistake |
| 9 | 02:55 | `c08_auntie_again` | "Auntie", back | "simit" | Same as #3 | Reversed shadow, no eyes | yes | r8 | refuse | mistake |
| 10 | 03:20 | `c09_nightowl` | Night owl, drunk | ayran | Pink hair, black tee | Nothing odd | no | r2 | refuse | mistake |
| 11 | 03:55 | `c10_bekci_fake` | Bekçi (fake) | cigarettes | Uniform, whistle | Whistle gone | **no (the cat's miss)** | r3 | refuse | mistake |
| 12 | 04:25 | `c11_tall` | Tall customer | boyoz | Pale, dark coat | Bright eyes on you; has a shadow | yes | r6 | sell | **instant end: seen** |
| 13 | 04:55 | `c12_fisherman` | Fisherman | gevrek | Cap, green jacket | Nothing odd | no | r8 | sell | mistake |
| 14 | 05:30 | `c13_hasan` | Hasan abi | the keys (no item) | Plain, like you | Hollow silhouette | yes | r7 | refuse | **instant end: fired** |

Totals: 7 sell, 6 refuse, 1 special. 7 humans, 7 others. v1 ids are unchanged; the one new customer is `c06b_riser`.

How the requirements are met:

- **Shift is genuinely required.** The two bekçis say the same lines, ask for the same thing and both wear a whistle in normal view; they have different faces, so rule 4 cannot shortcut rule 3, and the cat is silent for both. The courier looks like any other customer; the cat warns about him, but a player who has learned to distrust the cat still has to look at the floor.
- **Returning pair.** The student returns honestly (same order, sell). The auntie returns asking for a "simit" instead of çekirdek (refuse). She breaks rule 4 and rule 8 at once; her `ruleId` is `r8_gevrek` because the schema requires it of every simit asker, and rule 4 keeps its own test in the student's honest return.
- **Gevrek.** Exactly two customers say "simit" (7 and 9), both others, both with Shift anomalies that touch no other rule (they have shadows and do not stare). The fisherman is the local who says "gevrek" and is sold to. No ordinary human says "simit" in any line.
- **Boyoz and kumru.** The courier orders a kumru ("I eat my sandwiches on the bike") and the tall customer a boyoz ("the pastry one eats here at breakfast"); each line glosses the word in passing.
- **Ayran.** 23:35 sell, 03:20 refuse. The 03:20 customer is a plain human: not every rule is about spotting an other.
- **Special button.** Appears on the cat (cat food, correct) and as a "Hand over the keys" decoy on both bekçis and on Hasan, where it is always wrong.
- **No blanket strategy.** "Always refuse" is fired at 00:05. "Always sell" is fired at 02:30 (mistakes at 5, 7, 8). "Refuse everyone late" fails on 12 (ends the night) and 13. "Never use Shift" fails at 11 and guesses at 4. "Do what the cat says" fails at 11 and gives no answer at 8 and 12.
- **`ruleId` convention.** Set when a customer triggers a rule's condition, including when they pass it (2, 4, 6, 13). Empty for plain sales. Every customer is implicitly subject to rule 1.

## The street cat

A cat sits outside the window all night. When a customer with `catReacts: true` arrives, she hisses and puffs up. She reacts to every other except one, and never to a human.

- **Her one miss is the fake bekçi (03:55).** The real bekçi came at 00:35 and she stayed calm, correctly. When the second bekçi arrives and she stays calm again, a player who leans on her skips the Shift look and sells. The notebook still catches him: rule 3 says to look at every bekçi.
- **She is a hint, not a rule.** She is not in the notebook. Hasan's intro note says only: "The cat outside can tell, mostly. She gets tired late." The handover note option 1 is the one place a player can pass the lesson on.
- **She tells you what, not what to do.** A hiss means "other", and three others are not refusals: the "cat" (cat food), the tall customer (sell) and Hasan (who is decided by rule 7 either way). For the tall customer the hiss is a fair warning to keep the look short.
- The early riser mentions her ("Your cat does not like me. Cats never do."), which teaches the reaction the first time it matters in dialogue.

## Shift handover

On a good ending (`morning` or `changed`) the ending screen shows `ui.handoverPrompt` and three ready-made notes; the player must pick one. The key of the chosen note is saved, and in later games the notebook shows it after the last rule under "Previous shift's note". The lose endings are unchanged.

| Key | Attitude | Text |
|---|---|---|
| `handover.option1` | Helpful warning | The rules are real. Look when you must, never for long. And don't trust the cat after three. |
| `handover.option2` | A lie that all was fine | Quiet night. Nothing to report. The fridge is fine. Do not check the fridge. |
| `handover.option3` | The shift changed you | You'll see me tonight. I'll ask for a gevrek, like always. Please sell it to me. |

The notes are flavour plus one real tip (option 1). None of them changes the next night's script.

## Endings

| id | Title | Condition |
|---|---|---|
| `morning` | Morning | All 14 served, fewer than 3 mistakes, total Shift time at most 35 s |
| `changed` | The shift changed you | Same, but total Shift time above 35 s. In the final scene the player has no shadow. |
| `seen` | It saw you | Noticed meter reached 1.0, or refused the tall customer (#12) |
| `fired` | You're fired | 3 mistakes, or said yes to Hasan (#14), by Sell or by the keys button |

`morning` and `changed` are followed by the handover note choice.

## Difficulty curve

- **Tutorial (1–3, 23:05–00:05).** Three ordinary humans. The student's greeting ("Hasan abi always squinted at me") invites a first, safe Shift. The taxi driver teaches that ayran is fine before 03:00. The auntie foreshadows the cat and sets up her own return.
- **Learning the rules (4–8).** One new rule each: real bekçi (look, then sell), first other (no shadow, first hiss), honest return, the first "simit", the "cat" and the third button.
- **Rules collide (9–11).** The false return who also says "simit", the first ayran after 03:00, and the fake bekçi, the one time the cat is wrong.
- **Late night (12–14).** The starer punishes the habit of long looks built up over the night and is the first instant loss. The fisherman is a breather, a trap for the paranoid, and the proof that a bread ring can be sold if it is asked for properly. Hasan closes the night on the simplest rule and the strongest temptation.

Pacing: about 45–55 s per customer, 10–13 minutes in total.

## Out of scope

- Inventory, money, prices, change, stock counts, or actually preparing the tost.
- Player movement; the camera never leaves the counter.
- Randomised or procedurally generated customers; the night is a fixed script.
- Branching dialogue, player dialogue choices, or more than one request per customer.
- More than one night, saves (other than the one handover note), difficulty settings, or unlockables.
- Free-text handover notes, or notes that change the next night.
- Voice acting; Turkish localisation (strings.json is ready for it, but it is not shipped).
- Gore, jump scares, or chase sequences.
- Rule changes mid-night or rules that contradict each other.
- Mouse-only or gamepad control schemes, rebinding, mobile layout.
