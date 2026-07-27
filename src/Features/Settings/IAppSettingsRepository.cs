using FSChecklist.Domain.Settings;

namespace FSChecklist.Features.Settings
{
    internal interface IAppSettingsRepository
    {
        AppSettings Load();
        void Save(AppSettings settings);
    }
}
