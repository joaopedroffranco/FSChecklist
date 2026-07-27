using System.Collections.Generic;
using System.Threading.Tasks;

namespace FSChecklist.Features.AudioInput
{
    internal sealed class AudioInputDevice
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public bool IsDefault { get; private set; }

        public AudioInputDevice(string id, string name, bool isDefault)
        {
            Id = id;
            Name = name;
            IsDefault = isDefault;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal interface IAudioInputDeviceService
    {
        Task<IReadOnlyList<AudioInputDevice>> GetDevicesAsync();
        Task SetDefaultDeviceAsync(string deviceId);
    }
}
