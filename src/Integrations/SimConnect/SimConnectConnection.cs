using System;
using System.ComponentModel;
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
        private string status = "SimConnect: aguardando o simulador.";

        public bool IsConnected
        {
            get { lock (sync) return connected; }
        }

        public string Status
        {
            get { lock (sync) return status; }
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
                    Disconnect("SimConnect: conexão perdida; tentando novamente.");
                    continue;
                }

                if (stopSignal.WaitOne(100)) break;
            }

            Disconnect(null);
        }

        private void TryConnect()
        {
            try
            {
                IntPtr candidate;
                int result = NativeMethods.SimConnect_Open(
                    out candidate,
                    "FSChecklist",
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    0);
                if (result < 0)
                {
                    SetStatus(
                        false,
                        "SimConnect: MSFS não encontrado; nova tentativa em 5 s.");
                    return;
                }

                connection = candidate;
                SetStatus(true, "SimConnect: conectado ao Microsoft Flight Simulator.");
            }
            catch (DllNotFoundException)
            {
                SetStatus(
                    false,
                    "SimConnect: SimConnect.dll não encontrado ao lado do aplicativo.");
                stopSignal.Set();
            }
            catch (BadImageFormatException)
            {
                SetStatus(
                    false,
                    "SimConnect: SimConnect.dll incompatível; use a versão x64.");
                stopSignal.Set();
            }
            catch (Exception error)
            {
                SetStatus(
                    false,
                    "SimConnect: " + new Win32Exception(
                        Marshal.GetHRForException(error)).Message);
            }
        }

        private void Dispatch(
            IntPtr data,
            uint dataSize,
            IntPtr context)
        {
            if (data == IntPtr.Zero || dataSize < 12) return;
            uint receiveId = unchecked((uint)Marshal.ReadInt32(data, 8));
            if (receiveId == OpenMessage)
                SetStatus(true, "SimConnect: conectado ao Microsoft Flight Simulator.");
            else if (receiveId == QuitMessage)
                Disconnect("SimConnect: simulador encerrado; aguardando reinício.");
        }

        private void Disconnect(string newStatus)
        {
            IntPtr current = connection;
            connection = IntPtr.Zero;
            if (current != IntPtr.Zero)
                NativeMethods.SimConnect_Close(current);
            if (newStatus != null) SetStatus(false, newStatus);
        }

        private void SetStatus(bool isConnected, string value)
        {
            bool changed;
            lock (sync)
            {
                changed = connected != isConnected ||
                          !string.Equals(status, value, StringComparison.Ordinal);
                connected = isConnected;
                status = value;
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
                IntPtr data,
                uint dataSize,
                IntPtr context);

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
