using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NightShift.PlayModeTests
{
    // Smoke test of the real game: loads the Main scene and plays it by pressing the on-screen buttons, as a player's clicks would.
    // Any exception or error log during the run fails the test (Unity Test Framework default).
    // Shift cannot be simulated through the legacy Input class, so Shift vision is not covered here.
    public class GameSmokeTests
    {
        // Object paths created by the UI and Art code.
        private const string StartButton = "TitleScreen/StartButton";
        private const string NoteContinueButton = "NoteScreen/Paper/ContinueButton";
        private const string SellButton = "Actions/SellButton";
        private const string RefuseButton = "Actions/RefuseButton";
        private const string SpecialButton = "Actions/SpecialButton";
        private const string FeedbackContinueButton = "Actions/ContinueButton";
        private const string PlayAgainButton = "EndingScreen/PlayAgainButton";

        private GameData data;

        [UnitySetUp]
        public IEnumerator LoadMainScene()
        {
            data = GameData.LoadFromResources();
            SceneManager.LoadScene("Main");
            yield return WaitFrames(3);
        }

        [UnityTest]
        public IEnumerator CleanNight_ReachesMorning_ThenPlayAgainStartsAFreshNight()
        {
            AssertOneOfEach();
            AssertOnlyTitleIsShown();
            yield return Screenshot("1-title");

            yield return Click(StartButton);
            Assert.That(IsShown("NoteScreen"), Is.True, "The note should follow the Start button");
            Assert.That(IsShown("TitleScreen"), Is.False);
            yield return Screenshot("2-note");

            yield return Click(NoteContinueButton);

            for (int i = 0; i < data.Customers.Length; i++)
            {
                CustomerData customer = data.Customers[i];
                AssertCustomerIsShown(customer);
                if (i == 0) yield return Screenshot("3-customer");

                yield return Click(ButtonFor(customer.GetCorrectAction()));

                Assert.That(TextOf("DialogueBox/Body"), Is.EqualTo(data.Get(customer.correctResultKey)), customer.id + ": feedback line");
                Assert.That(RedMistakeMarks(), Is.EqualTo(0), customer.id + ": a correct decision is not a mistake");
                if (i == 0) yield return Screenshot("4-feedback");

                yield return Click(FeedbackContinueButton);
            }

            AssertEndingIsShown(EndingId.Morning);
            yield return Screenshot("5-ending");

            yield return Click(PlayAgainButton);
            AssertOnlyTitleIsShown();
            AssertOneOfEach();

            // Second, short run: the first customer again, served, and the second one arrives.
            yield return Click(StartButton);
            yield return Click(NoteContinueButton);
            AssertCustomerIsShown(data.Customers[0]);
            AssertHudIsReset();
            yield return Click(ButtonFor(data.Customers[0].GetCorrectAction()));
            yield return Click(FeedbackContinueButton);
            AssertCustomerIsShown(data.Customers[1]);

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WrongChoices_ReachFired_ThenPlayAgainResetsTheMistakes()
        {
            int maxMistakes = data.Balance.maxMistakes;
            int mistakes = 0;

            yield return Click(StartButton);
            yield return Click(NoteContinueButton);

            for (int i = 0; i < data.Customers.Length && mistakes < maxMistakes; i++)
            {
                CustomerData customer = data.Customers[i];
                AssertCustomerIsShown(customer);

                bool wrong = customer.wrongOutcome == "mistake";
                PlayerAction action = customer.GetCorrectAction();
                if (wrong) action = action == PlayerAction.Sell ? PlayerAction.Refuse : PlayerAction.Sell;

                yield return Click(ButtonFor(action));

                if (wrong) mistakes++;
                Assert.That(TextOf("DialogueBox/Body"), Is.EqualTo(data.Get(wrong ? customer.wrongResultKey : customer.correctResultKey)), customer.id);
                Assert.That(RedMistakeMarks(), Is.EqualTo(mistakes), customer.id + ": red mistake marks in the HUD");

                yield return Click(FeedbackContinueButton);
            }

            Assert.That(mistakes, Is.EqualTo(maxMistakes), "The data should allow enough ordinary mistakes to get fired");
            AssertEndingIsShown(EndingId.Fired);

            yield return Click(PlayAgainButton);
            AssertOnlyTitleIsShown();

            yield return Click(StartButton);
            yield return Click(NoteContinueButton);
            AssertCustomerIsShown(data.Customers[0]);
            AssertHudIsReset();

            LogAssert.NoUnexpectedReceived();
        }

        // ---------- What should be on screen ----------

        private static void AssertOneOfEach()
        {
            Assert.That(CountInScene<Camera>(), Is.EqualTo(1), "cameras");
            Assert.That(CountInScene<AudioListener>(), Is.EqualTo(1), "audio listeners");
            Assert.That(CountInScene<EventSystem>(), Is.EqualTo(1), "event systems");
            Assert.That(CountInScene<Canvas>(), Is.EqualTo(1), "canvases");
        }

        private void AssertOnlyTitleIsShown()
        {
            Assert.That(IsShown("TitleScreen"), Is.True, "title screen");
            Assert.That(IsShown("NoteScreen"), Is.False, "note");
            Assert.That(IsShown("Hud"), Is.False, "HUD");
            Assert.That(IsShown("DialogueBox"), Is.False, "dialogue box");
            Assert.That(IsShown("Actions"), Is.False, "action buttons");
            Assert.That(IsShown("EndingScreen"), Is.False, "ending screen");
            Assert.That(IsShown("Notebook"), Is.False, "notebook");
            Assert.That(RendererOf("KioskView/EndingPicture").enabled, Is.False, "ending picture");
            Assert.That(RendererOf("KioskView/Shaken/Customer").enabled, Is.False, "customer picture");
            Assert.That(TextOf("TitleScreen/ShiftHintBox/ShiftHint"), Is.EqualTo(data.Get("ui.shiftHint")), "the Shift instruction on the title screen");
        }

        private void AssertCustomerIsShown(CustomerData customer)
        {
            Assert.That(IsShown("Hud"), Is.True, customer.id + ": HUD");
            Assert.That(TextOf("Hud/Clock"), Is.EqualTo(GameData.FormatClock(customer.arrivalMinute)), customer.id + ": clock");
            Assert.That(TextOf("DialogueBox/Name"), Is.EqualTo(data.Get(customer.nameKey)), customer.id + ": name");
            Assert.That(TextOf("DialogueBox/Body"), Is.EqualTo(data.Get(customer.greetingKey) + "\n" + data.Get(customer.requestKey)), customer.id + ": lines");
            Assert.That(IsShown("DialogueBox/ShiftHint"), Is.False, customer.id + ": the Shift hint is hidden while Shift is not held");
            Assert.That(IsShown(SellButton), Is.True, customer.id + ": Sell button");
            Assert.That(IsShown(RefuseButton), Is.True, customer.id + ": Refuse button");
            Assert.That(IsShown(SpecialButton), Is.EqualTo(customer.HasSpecialAction()), customer.id + ": third button");
            Assert.That(IsShown(FeedbackContinueButton), Is.False, customer.id + ": Continue button");

            SpriteRenderer picture = RendererOf("KioskView/Shaken/Customer");
            Assert.That(picture.enabled && picture.sprite != null, Is.True, customer.id + ": customer picture");
        }

        private void AssertEndingIsShown(EndingId ending)
        {
            Assert.That(IsShown("EndingScreen"), Is.True, "ending screen");
            Assert.That(IsShown("Hud"), Is.False, "HUD on the ending screen");
            Assert.That(TextOf("EndingScreen/Title"), Is.EqualTo(data.Get(DataParsing.EndingTitleKey(ending))));
            Assert.That(TextOf("EndingScreen/Body"), Is.EqualTo(data.Get(DataParsing.EndingBodyKey(ending))));

            SpriteRenderer picture = RendererOf("KioskView/EndingPicture");
            Assert.That(picture.enabled && picture.sprite != null, Is.True, "ending picture");
        }

        private void AssertHudIsReset()
        {
            Assert.That(RedMistakeMarks(), Is.EqualTo(0), "mistake marks after Play again");
            Assert.That(Find("Hud/MistakeMarks").transform.childCount, Is.EqualTo(data.Balance.maxMistakes), "one mark per allowed mistake");
            float fillWidth = ((RectTransform)Find("Hud/NoticedBar/NoticedFill").transform).sizeDelta.x;
            Assert.That(fillWidth, Is.EqualTo(0f).Within(0.01f), "Noticed meter after Play again");
        }

        // ---------- Helpers ----------

        private static string ButtonFor(PlayerAction action)
        {
            switch (action)
            {
                case PlayerAction.Sell: return SellButton;
                case PlayerAction.Refuse: return RefuseButton;
                default: return SpecialButton;
            }
        }

        // Presses a visible button, then waits a frame (the UI accepts one press per frame).
        private static IEnumerator Click(string path)
        {
            Button button = Find(path).GetComponent<Button>();
            Assert.That(button != null && button.IsInteractable(), Is.True, "'" + path + "' is not a usable button");
            button.onClick.Invoke();
            yield return null;
        }

        // GameObject.Find only returns objects that are active, which here means "on screen".
        private static bool IsShown(string path)
        {
            GameObject found = GameObject.Find(path);
            return found != null && found.activeInHierarchy;
        }

        private static GameObject Find(string path)
        {
            GameObject found = GameObject.Find(path);
            Assert.That(found != null, Is.True, "'" + path + "' is not on screen");
            return found;
        }

        private static string TextOf(string path)
        {
            return Find(path).GetComponent<Text>().text;
        }

        private static SpriteRenderer RendererOf(string path)
        {
            return Find(path).GetComponent<SpriteRenderer>();
        }

        private static int RedMistakeMarks()
        {
            int red = 0;
            foreach (Image mark in Find("Hud/MistakeMarks").GetComponentsInChildren<Image>())
            {
                if (mark.color == UITheme.Danger) red++;
            }
            return red;
        }

        private static int CountInScene<T>() where T : Component
        {
            int count = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                foreach (GameObject root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                {
                    count += root.GetComponentsInChildren<T>(true).Length;
                }
            }
            return count;
        }

        private static IEnumerator WaitFrames(int frames)
        {
            for (int i = 0; i < frames; i++) yield return null;
        }

        // Saves a picture of the screen to <project>/Logs/screens/. Never fails the test.
        private static IEnumerator Screenshot(string name)
        {
            try
            {
                string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "screens");
                Directory.CreateDirectory(folder);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
            }
            catch (Exception exception)
            {
                Debug.Log("Screenshot '" + name + "' was not taken: " + exception.Message);
            }
            yield return WaitFrames(2);
        }
    }
}
