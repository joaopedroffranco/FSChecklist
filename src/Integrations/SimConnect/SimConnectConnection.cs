using System;
using System.Runtime.InteropServices;
using System.Threading;
using FSChecklist.Features.Simulator;

namespace FSChecklist.Integrations.SimConnect
{
    internal sealed class SimConnectConnection : ISimulatorConnection
    {
        private const uint OpenMessage = 2;
        private const uint QuitMessage = 3;
        private readonly object sync = new object();
        private readonly AutoResetEvent stopSignal = new AutoResetEvent(false);
        private Thread worker;
        private IntPtr connection;
        private bool disposed;
        private bool connected;

        public bool IsConnected
        {
            get { lock (sync) return connected; }
        }

        public event Action StatusChanged;

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
                    Dispatch,
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
            if (data == IntPtr.Zero || dataSize < 12) return;
            uint receiveId = unchecked((uint)Marshal.ReadInt32(data, 8));
            if (receiveId == OpenMessage)
                SetConnected(true);
            else if (receiveId == QuitMessage)
                Disconnect();
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
        }
    }
}
