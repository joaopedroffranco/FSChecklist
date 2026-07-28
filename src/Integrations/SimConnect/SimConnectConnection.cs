using System;
using System.Runtime.InteropServices;
using System.Threading;
using FSChecklist.Domain.Flight;
using FSChecklist.Features.Simulator;

namespace FSChecklist.Integrations.SimConnect
{
    internal sealed class SimConnectConnection : ISimulatorConnection
    {
        private const uint OpenMessage = 2;
        private const uint QuitMessage = 3;
        private const uint SimObjectDataMessage = 8;
        private const uint TelemetryDefinition = 1;
        private const uint TelemetryRequest = 1;
        private const uint UserAircraft = 0;
        private const uint SimFramePeriod = 3;
        private const int SimObjectDataOffset = 40;
        private const int TelemetryValueCount = 17;
        private readonly object sync = new object();
        private readonly AutoResetEvent stopSignal = new AutoResetEvent(false);
        private readonly NativeMethods.DispatchProc dispatchCallback;
        private Thread worker;
        private IntPtr connection;
        private bool disposed;
        private bool connected;

        public bool IsConnected
        {
            get { lock (sync) return connected; }
        }

        public event Action StatusChanged;
        public event Action<FlightTelemetry> TelemetryReceived;

        public SimConnectConnection()
        {
            dispatchCallback = Dispatch;
        }

        public void Start()
        {
            lock (sync)
            {
                if (disposed || worker != null) return;
                worker = new Thread(Run)
                {
                    IsBackground = true,
                    Name = "FSChecklist SimConnect"
                };
                worker.Start();
            }
        }

        private void Run()
        {
            while (!stopSignal.WaitOne(0))
            {
                if (connection == IntPtr.Zero)
                {
                    TryConnect();
                    if (connection == IntPtr.Zero)
                    {
                        if (stopSignal.WaitOne(TimeSpan.FromSeconds(5))) break;
                        continue;
                    }
                }

                int result = NativeMethods.SimConnect_CallDispatch(
                    connection,
                    dispatchCallback,
                    IntPtr.Zero);
                if (result < 0)
                {
                    Disconnect();
                    continue;
                }
                if (stopSignal.WaitOne(100)) break;
            }
            Disconnect();
        }

        private void TryConnect()
        {
            try
            {
                IntPtr candidate;
                int result = NativeMethods.SimConnect_Open(
                    out candidate, "FSChecklist", IntPtr.Zero, 0, IntPtr.Zero, 0);
                if (result < 0)
                {
                    SetConnected(false);
                    return;
                }
                connection = candidate;
                ConfigureTelemetry(candidate);
                SetConnected(true);
            }
            catch (DllNotFoundException)
            {
                SetConnected(false);
                stopSignal.Set();
            }
            catch (BadImageFormatException)
            {
                SetConnected(false);
                stopSignal.Set();
            }
            catch (Exception)
            {
                SetConnected(false);
            }
        }

        private void Dispatch(IntPtr data, uint dataSize, IntPtr context)
        {
            try
            {
                if (data == IntPtr.Zero || dataSize < 12) return;
                uint receiveId = unchecked((uint)Marshal.ReadInt32(data, 8));
                if (receiveId == OpenMessage)
                    SetConnected(true);
                else if (receiveId == QuitMessage)
                    Disconnect();
                else if (receiveId == SimObjectDataMessage)
                    PublishTelemetry(data, dataSize);
            }
            catch
            {
                // Never allow a managed exception to cross the native callback.
            }
        }

        private void ConfigureTelemetry(IntPtr currentConnection)
        {
            AddTelemetryValue(
                currentConnection, "AIRSPEED INDICATED", "Knots");
            AddTelemetryValue(
                currentConnection, "GROUND VELOCITY", "Knots");
            AddTelemetryValue(
                currentConnection, "INDICATED ALTITUDE", "Feet");
            AddTelemetryValue(
                currentConnection, "RADIO HEIGHT", "Feet");
            AddTelemetryValue(
                currentConnection, "VERTICAL SPEED", "Feet per minute");
            AddTelemetryValue(
                currentConnection, "SIM ON GROUND", "Bool");
            AddTelemetryValue(
                currentConnection, "TURB ENG N1:1", "Percent");
            AddTelemetryValue(
                currentConnection, "TURB ENG N1:2", "Percent");
            AddTelemetryValue(
                currentConnection,
                "TURB ENG REVERSE NOZZLE PERCENT:1",
                "Percent");
            AddTelemetryValue(
                currentConnection,
                "TURB ENG REVERSE NOZZLE PERCENT:2",
                "Percent");
            AddTelemetryValue(
                currentConnection, "SPOILERS LEFT POSITION", "Percent");
            AddTelemetryValue(
                currentConnection, "SPOILERS RIGHT POSITION", "Percent");
            AddTelemetryValue(
                currentConnection, "BRAKE LEFT POSITION", "Position");
            AddTelemetryValue(
                currentConnection, "BRAKE RIGHT POSITION", "Position");
            AddTelemetryValue(
                currentConnection, "AUTOBRAKES ACTIVE", "Bool");
            AddTelemetryValue(
                currentConnection, "L:AIRLINER_V1_SPEED", "Knots");
            AddTelemetryValue(
                currentConnection, "L:FNX2PLD_speedV1", "Number");

            ThrowIfFailed(NativeMethods.SimConnect_RequestDataOnSimObject(
                currentConnection,
                TelemetryRequest,
                TelemetryDefinition,
                UserAircraft,
                SimFramePeriod,
                0,
                0,
                2,
                0));
        }

        private static void AddTelemetryValue(
            IntPtr currentConnection,
            string name,
            string unit)
        {
            ThrowIfFailed(NativeMethods.SimConnect_AddToDataDefinition(
                currentConnection,
                TelemetryDefinition,
                name,
                unit,
                4,
                0F,
                uint.MaxValue));
        }

        private static void ThrowIfFailed(int result)
        {
            if (result < 0)
                Marshal.ThrowExceptionForHR(result);
        }

        private void PublishTelemetry(IntPtr data, uint dataSize)
        {
            int requiredSize =
                SimObjectDataOffset + (TelemetryValueCount * sizeof(double));
            if (dataSize < requiredSize) return;

            uint requestId =
                unchecked((uint)Marshal.ReadInt32(data, 12));
            uint definitionId =
                unchecked((uint)Marshal.ReadInt32(data, 20));
            uint definitionCount =
                unchecked((uint)Marshal.ReadInt32(data, 36));
            if (requestId != TelemetryRequest ||
                definitionId != TelemetryDefinition ||
                definitionCount < TelemetryValueCount)
            {
                return;
            }

            int offset = SimObjectDataOffset;
            double[] values = new double[TelemetryValueCount];
            for (int index = 0; index < values.Length; index++)
            {
                values[index] = Marshal.PtrToStructure<double>(
                    IntPtr.Add(data, offset));
                offset += sizeof(double);
            }

            TelemetryReceived?.Invoke(new FlightTelemetry
            {
                Timestamp = DateTimeOffset.UtcNow,
                IndicatedAirspeedKnots = values[0],
                GroundSpeedKnots = values[1],
                IndicatedAltitudeFeet = values[2],
                RadioAltitudeFeet = values[3],
                VerticalSpeedFeetPerMinute = values[4],
                IsOnGround = values[5] >= 0.5D,
                EngineOneN1Percent = values[6],
                EngineTwoN1Percent = values[7],
                EngineOneReversePercent = values[8],
                EngineTwoReversePercent = values[9],
                LeftSpoilerPercent = values[10],
                RightSpoilerPercent = values[11],
                LeftBrakePosition = values[12],
                RightBrakePosition = values[13],
                AutobrakesActive = values[14] >= 0.5D,
                AirlineV1Knots = values[15],
                FenixV1Knots = values[16]
            });
        }

        private void Disconnect()
        {
            IntPtr current = connection;
            connection = IntPtr.Zero;
            if (current != IntPtr.Zero)
                NativeMethods.SimConnect_Close(current);
            SetConnected(false);
        }

        private void SetConnected(bool value)
        {
            bool changed;
            lock (sync)
            {
                changed = connected != value;
                connected = value;
            }
            if (changed) StatusChanged?.Invoke();
        }

        public void Dispose()
        {
            Thread current;
            lock (sync)
            {
                if (disposed) return;
                disposed = true;
                current = worker;
            }
            stopSignal.Set();
            if (current != null && current != Thread.CurrentThread)
                current.Join(TimeSpan.FromSeconds(2));
            stopSignal.Dispose();
        }

        private static class NativeMethods
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void DispatchProc(
                IntPtr data, uint dataSize, IntPtr context);

            [DllImport("SimConnect.dll", CharSet = CharSet.Ansi)]
            internal static extern int SimConnect_Open(
                out IntPtr connection,
                string name,
                IntPtr windowHandle,
                uint userEvent,
                IntPtr eventHandle,
                uint configIndex);

            [DllImport("SimConnect.dll")]
            internal static extern int SimConnect_Close(IntPtr connection);

            [DllImport("SimConnect.dll")]
            internal static extern int SimConnect_CallDispatch(
                IntPtr connection,
                DispatchProc callback,
                IntPtr context);

            [DllImport("SimConnect.dll", CharSet = CharSet.Ansi)]
            internal static extern int SimConnect_AddToDataDefinition(
                IntPtr connection,
                uint definitionId,
                string datumName,
                string unitsName,
                uint datumType,
                float epsilon,
                uint datumId);

            [DllImport("SimConnect.dll")]
            internal static extern int SimConnect_RequestDataOnSimObject(
                IntPtr connection,
                uint requestId,
                uint definitionId,
                uint objectId,
                uint period,
                uint flags,
                uint origin,
                uint interval,
                uint limit);
        }
    }
}
