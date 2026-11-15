using NUnit.Framework;

namespace NightShift.Tests
{
    // v2 shift handover: the pure rules (which notes exist, when one may be left, what a stored key shows) and the in-memory store.
    public class HandoverNotesTests
    {
        private GameData data;

        [SetUp]
        public void LoadData()
        {
            data = TestData.Load();
        }

        [Test]
        public void OptionCount_IsThree()
        {
            Assert.That(HandoverNotes.OptionCount, Is.EqualTo(3));
        }

        [TestCase(0, "handover.option1")]
        [TestCase(1, "handover.option2")]
        [TestCase(2, "handover.option3")]
        public void OptionKey_MapsIndexToTheStringKey(int optionIndex, string expectedKey)
        {
            Assert.That(HandoverNotes.OptionKey(optionIndex), Is.EqualTo(expectedKey));
        }

        [TestCase(EndingId.Morning, true)]
        [TestCase(EndingId.Changed, true)]
        [TestCase(EndingId.Seen, false)]
        [TestCase(EndingId.Fired, false)]
        public void IsGoodEnding_OnlyForMorningAndChanged(EndingId ending, bool expected)
        {
            Assert.That(HandoverNotes.IsGoodEnding(ending), Is.EqualTo(expected));
        }

        [Test]
        public void IsValidKey_IsTrueForEveryOption()
        {
            for (int i = 0; i < HandoverNotes.OptionCount; i++)
            {
                Assert.That(HandoverNotes.IsValidKey(data, HandoverNotes.OptionKey(i)), Is.True, HandoverNotes.OptionKey(i));
            }
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("handover.option4")]   // a note that does not exist (any more)
        [TestCase("handover.")]
        [TestCase("ui.title")]           // exists in strings.json, but is not a handover note
        [TestCase("Handover.option1")]   // keys are case-sensitive
        public void IsValidKey_IsFalseForMissingOrForeignKeys(string noteKey)
        {
            Assert.That(HandoverNotes.IsValidKey(data, noteKey), Is.False);
        }

        [Test]
        public void IsValidKey_IsFalseWithoutData()
        {
            Assert.That(HandoverNotes.IsValidKey(null, HandoverNotes.OptionKey(0)), Is.False);
        }

        [Test]
        public void NoteText_IsTheTextOfTheStoredKey()
        {
            for (int i = 0; i < HandoverNotes.OptionCount; i++)
            {
                string key = HandoverNotes.OptionKey(i);
                Assert.That(HandoverNotes.NoteText(data, key), Is.EqualTo(data.Get(key)), key);
                Assert.That(HandoverNotes.NoteText(data, key), Is.Not.Empty, key);
            }
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("handover.option4")]
        [TestCase("ui.title")]
        public void NoteText_IsEmptyForMissingOrForeignKeys(string noteKey)
        {
            Assert.That(HandoverNotes.NoteText(data, noteKey), Is.EqualTo(""));
        }

        [Test]
        public void MemoryNoteStore_IsEmptyByDefault()
        {
            Assert.That(new MemoryNoteStore().Load(), Is.EqualTo(""));
        }

        [Test]
        public void MemoryNoteStore_LoadReturnsTheLastSavedKey()
        {
            INoteStore store = new MemoryNoteStore();

            store.Save(HandoverNotes.OptionKey(1));
            Assert.That(store.Load(), Is.EqualTo("handover.option2"));

            store.Save(HandoverNotes.OptionKey(2));
            Assert.That(store.Load(), Is.EqualTo("handover.option3"));
        }

        [Test]
        public void MemoryNoteStore_SavingNullStoresEmpty()
        {
            INoteStore store = new MemoryNoteStore();
            store.Save(HandoverNotes.OptionKey(0));

            store.Save(null);

            Assert.That(store.Load(), Is.EqualTo(""));
        }

        [Test]
        public void MemoryNoteStore_InstancesDoNotShareTheNote()
        {
            new MemoryNoteStore().Save(HandoverNotes.OptionKey(0));

            Assert.That(new MemoryNoteStore().Load(), Is.EqualTo(""));
        }
    }
}
