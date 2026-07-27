using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using FSChecklist.Domain.Checklists;

namespace FSChecklist.Features.Repository
{
    internal sealed class JsonChecklistRepository : IChecklistRepository
    {
        public IReadOnlyList<ChecklistDocument> LoadAll(string directory)
        {
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException(
                    "Pasta de checklists nao encontrada: " + directory);

            var documents = new List<ChecklistDocument>();
            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                ChecklistDocument document;
                try
                {
                    document = JsonSerializer.Deserialize<ChecklistDocument>(
                        File.ReadAllText(file, Encoding.UTF8));
                }
                catch (Exception error)
                {
                    throw new InvalidDataException(
                        "JSON invalido em " + Path.GetFileName(file) + ": " + error.Message,
                        error);
                }

                if (document == null || string.IsNullOrWhiteSpace(document.aircraft) ||
                    document.checklists == null)
                {
                    throw new InvalidDataException(
                        "JSON invalido em " + Path.GetFileName(file) +
                        ": campos obrigatorios aircraft e checklists.");
                }

                documents.Add(document);
            }

            return documents;
        }
    }
}
