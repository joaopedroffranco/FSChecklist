using System.Collections.Generic;
using FSChecklist.Domain.Checklists;

namespace FSChecklist.Features.Repository
{
    internal interface IChecklistRepository
    {
        IReadOnlyList<ChecklistDocument> LoadAll(string directory);
    }
}
