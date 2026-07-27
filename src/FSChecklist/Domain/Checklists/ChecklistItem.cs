using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace FSChecklist.Domain.Checklists
{
    internal sealed class ChecklistItem
    {
        public string Callout { get; private set; }
        public IReadOnlyList<string> Responses { get; private set; }

        public ChecklistItem(string callout, IEnumerable<string> responses)
        {
            Callout = callout ?? string.Empty;
            Responses = responses == null
                ? new List<string>()
                : responses.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
        }

        public static ChecklistItem FromJson(object value)
        {
            if (value is JsonElement element)
                return FromJsonElement(element);

            string text = value as string;
            if (text != null)
                return new ChecklistItem(text, null);

            Dictionary<string, object> data = value as Dictionary<string, object>;
            if (data == null)
                return new ChecklistItem(
                    Convert.ToString(value, CultureInfo.InvariantCulture), null);

            object calloutValue;
            string callout = data.TryGetValue("callout", out calloutValue)
                ? Convert.ToString(calloutValue, CultureInfo.InvariantCulture)
                : string.Empty;

            var responses = new List<string>();
            object responseValue;
            object[] responseArray = data.TryGetValue("responses", out responseValue)
                ? responseValue as object[]
                : null;
            if (responseArray != null)
                responses.AddRange(responseArray.Select(Convert.ToString));

            return new ChecklistItem(callout, responses);
        }

        private static ChecklistItem FromJsonElement(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return new ChecklistItem(element.GetString(), null);

            if (element.ValueKind != JsonValueKind.Object)
                return new ChecklistItem(element.ToString(), null);

            JsonElement calloutElement;
            string callout = element.TryGetProperty("callout", out calloutElement)
                ? calloutElement.GetString()
                : string.Empty;

            var responses = new List<string>();
            JsonElement responsesElement;
            if (element.TryGetProperty("responses", out responsesElement) &&
                responsesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement response in responsesElement.EnumerateArray())
                    responses.Add(response.GetString());
            }

            return new ChecklistItem(callout, responses);
        }
    }
}
