using System.Collections.Generic;

namespace FSChecklist.Domain.Checklists
{
    internal sealed class ChecklistRules
    {
        public bool acceptAnyAnswer { get; set; }
        public List<string> acceptedResponses { get; set; }
    }

    internal sealed class ChecklistDefinition
    {
        public string id { get; set; }
        public string name { get; set; }
        public string next { get; set; }
        public string completedCallout { get; set; }
        public List<object> items { get; set; }
    }

    internal sealed class ChecklistDocument
    {
        public string aircraft { get; set; }
        public string language { get; set; }
        public ChecklistRules rules { get; set; }
        public List<ChecklistDefinition> checklists { get; set; }
    }
}
