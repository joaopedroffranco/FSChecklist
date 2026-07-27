using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FSChecklist.Domain.Checklists;

namespace FSChecklist.Features.Checklist
{
    internal sealed class ChecklistSession
    {
        public ChecklistDocument Document { get; private set; }
        public ChecklistDefinition Checklist { get; private set; }
        public int ItemIndex { get; private set; } = -1;

        public int ItemCount
        {
            get { return Checklist == null || Checklist.items == null ? 0 : Checklist.items.Count; }
        }

        public bool IsActive
        {
            get { return Checklist != null && ItemIndex >= 0; }
        }

        public bool IsComplete
        {
            get { return IsActive && ItemIndex >= ItemCount; }
        }

        public ChecklistItem CurrentItem
        {
            get
            {
                if (!IsActive || IsComplete) return null;
                return ChecklistItem.FromJson(Checklist.items[ItemIndex]);
            }
        }

        public void Start(ChecklistDocument document, ChecklistDefinition checklist)
        {
            Document = document;
            Checklist = checklist;
            ItemIndex = 0;
        }

        public bool MoveBack()
        {
            if (!IsActive || ItemIndex <= 0) return false;
            ItemIndex--;
            return true;
        }

        public bool TryConfirm(string spokenText)
        {
            ChecklistItem item = CurrentItem;
            if (item == null) return false;

            string heard = NormalizeSpeech(spokenText);
            bool acceptsAny = Document != null && Document.rules != null &&
                              Document.rules.acceptAnyAnswer;
            bool matched = acceptsAny && heard.Length > 0;

            if (!matched)
            {
                foreach (string response in item.Responses)
                {
                    string answer = NormalizeSpeech(response);
                    if (heard == answer ||
                        Regex.IsMatch(heard, "(^| )" + Regex.Escape(answer) + "( |$)"))
                    {
                        matched = true;
                        break;
                    }
                }
            }

            if (matched) ItemIndex++;
            return matched;
        }

        private static string NormalizeSpeech(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            string decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (char character in decomposed)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category != UnicodeCategory.NonSpacingMark)
                    builder.Append(character);
            }
            return Regex.Replace(builder.ToString(), "[^a-z0-9]+", " ").Trim();
        }
    }
}
