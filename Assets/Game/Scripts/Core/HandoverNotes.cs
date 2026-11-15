using UnityEngine;

namespace NightShift
{
    // Where the handover note of the last good night is kept between games.
    // The stored value is a strings.json KEY (e.g. "handover.option2"), never the text.
    public interface INoteStore
    {
        // The stored key, or "" when nothing was saved yet.
        string Load();

        void Save(string noteKey);
    }

    // The real store: PlayerPrefs (browser storage on WebGL), so the note survives a page reload.
    public class PlayerPrefsNoteStore : INoteStore
    {
        public const string PrefsKey = "nightshift.handoverNote";

        public string Load()
        {
            return PlayerPrefs.GetString(PrefsKey, "");
        }

        public void Save(string noteKey)
        {
            PlayerPrefs.SetString(PrefsKey, noteKey);
            PlayerPrefs.Save(); // WebGL only writes to the browser when asked to
        }
    }

    // A store that only lives in memory, for tests and the Editor.
    public class MemoryNoteStore : INoteStore
    {
        private string storedKey = "";

        public string Load()
        {
            return storedKey;
        }

        public void Save(string noteKey)
        {
            storedKey = noteKey ?? "";
        }
    }

    // The rules of the shift handover, as pure functions: which notes exist, when the player
    // may leave one, and what text a stored key turns into.
    public static class HandoverNotes
    {
        public const int OptionCount = 3;
        public const string KeyPrefix = "handover.";
        public const string PromptKey = "ui.handoverPrompt";
        public const string DoneKey = "ui.handoverDone";
        public const string HeadingKey = "ui.previousNote";

        // Option index 0..2 -> "handover.option1".."handover.option3".
        public static string OptionKey(int optionIndex)
        {
            return KeyPrefix + "option" + (optionIndex + 1);
        }

        // Only the two "you survived" endings let the player leave a note.
        public static bool IsGoodEnding(EndingId ending)
        {
            return ending == EndingId.Morning || ending == EndingId.Changed;
        }

        // True when the stored key is a handover note that still exists in strings.json.
        public static bool IsValidKey(GameData data, string noteKey)
        {
            return data != null
                && !string.IsNullOrEmpty(noteKey)
                && noteKey.StartsWith(KeyPrefix, System.StringComparison.Ordinal)
                && data.Has(noteKey);
        }

        // The text to show in the notebook for a stored key; "" when there is none or it is not valid.
        public static string NoteText(GameData data, string noteKey)
        {
            return IsValidKey(data, noteKey) ? data.Get(noteKey) : "";
        }
    }
}
