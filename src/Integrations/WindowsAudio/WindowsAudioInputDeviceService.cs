using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FSChecklist.Features.AudioInput;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;

namespace FSChecklist.Integrations.WindowsAudio
{
    internal sealed class WindowsAudioInputDeviceService :
        IAudioInputDeviceService
    {
        public async Task<IReadOnlyList<AudioInputDevice>> GetDevicesAsync()
        {
            string defaultId = MediaDevice.GetDefaultAudioCaptureId(
                AudioDeviceRole.Default);
            DeviceInformationCollection devices =
                await DeviceInformation.FindAllAsync(
                    MediaDevice.GetAudioCaptureSelector());

            return devices
                .Select(device => new AudioInputDevice(
                    device.Id,
                    device.Name,
                    string.Equals(
                        device.Id,
                        defaultId,
                        StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(device => device.IsDefault)
                .ThenBy(device => device.Name)
                .ToList();
        }

        public Task SetDefaultDeviceAsync(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return Task.CompletedTask;

            string currentDefaultId = MediaDevice.GetDefaultAudioCaptureId(
                AudioDeviceRole.Default);
            if (string.Equals(
                deviceId,
                currentDefaultId,
                StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;

            IPolicyConfig policy = null;
            try
            {
                object policyClient = new PolicyConfigClient();
                policy = (IPolicyConfig)policyClient;
                SetDefault(policy, deviceId, EndpointRole.Console);
                SetDefault(policy, deviceId, EndpointRole.Multimedia);
                SetDefault(policy, deviceId, EndpointRole.Communications);
            }
            finally
            {
                if (policy != null && Marshal.IsComObject(policy))
                    Marshal.FinalReleaseComObject(policy);
            }

            return Task.CompletedTask;
        }

        private static void SetDefault(
            IPolicyConfig policy,
            string deviceId,
            EndpointRole role)
        {
            int result = policy.SetDefaultEndpoint(deviceId, role);
            if (result != 0) Marshal.ThrowExceptionForHR(result);
        }

        private enum EndpointRole
        {
            Console = 0,
            Multimedia = 1,
            Communications = 2
        }

        [ComImport]
        [Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
        private sealed class PolicyConfigClient
        {
        }

        [ComImport]
        [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPolicyConfig
        {
            [PreserveSig]
            int GetMixFormat(
                [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
                IntPtr format);

            [PreserveSig]
            int GetDeviceFormat(
                [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
                int defaultFormat,
                IntPtr format);

            [PreserveSig]
            int ResetDeviceFormat(
                [MarshalAs(UnmanagedType.LPWStr)] string deviceId);

            [PreserveSig]
            int SetDeviceFormat(
                [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
                IntPtr endpointFormat,
                IntPtr mixFormat);

            [PreserveSig]
            int GetProcessingPeriod(
                [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
                int defaultPeriod,
                IntPtr defaultValue,
                IntPtr minimumValue);

            [PreserveSig]
            int SetProcessingPeriod(
                [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
                IntPtr period);

            [PreserveSig]
            int GetShareMode(
                [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
                IntPtr mode);

            [PreserveSig]
            int SetShareMode(
                [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
                IntPtr mode);

            [PreserveSig]
            int GetPropertyValue(
                [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
                IntPtr key,
                IntPtr value);

            [PreserveSig]
            int SetPropertyValue(
                [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
                IntPtr key,
                IntPtr value);

            [PreserveSig]
            int SetDefaultEndpoint(
                [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
                EndpointRole role);

            [PreserveSig]
            int SetEndpointVisibility(
                [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
                int visible);
        }
    }
}
