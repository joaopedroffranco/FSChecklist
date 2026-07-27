namespace FSChecklist.Features.Localization
{
    internal interface IAppLocalizer
    {
        string Language { get; }
        void SetLanguage(string language);
        string Get(string key);
        string Format(string key, params object[] arguments);
    }
}
