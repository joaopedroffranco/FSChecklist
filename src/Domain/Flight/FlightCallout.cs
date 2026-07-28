namespace FSChecklist.Domain.Flight
{
    internal sealed class FlightCallout
    {
        public FlightCallout(string id, string spokenText)
        {
            Id = id;
            SpokenText = spokenText;
        }

        public string Id { get; }
        public string SpokenText { get; }
    }
}
