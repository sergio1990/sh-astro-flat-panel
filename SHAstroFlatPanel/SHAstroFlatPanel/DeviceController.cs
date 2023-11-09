using ASCOM.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.SHAstroFlatPanel
{
    internal class DeviceController : IDisposable
    {
        private TraceLogger traceLogger;
        private Serial serialConnection = new Serial();
        private Boolean isConnected;

        internal DeviceController()
        {
            traceLogger = new TraceLogger("", "SHAstroFlatPanel DeviceController");
            traceLogger.Enabled = true;
            isConnected = false;
        }

        internal bool Connected
        {
            get
            {
                traceLogger.LogMessage("Connected Get", isConnected.ToString());
                return this.isConnected;
            }
        }

        internal ConnectResult Connect(String comPortDefault, bool comPortAutoDetect)
        {
            traceLogger.LogMessage("Connect called", "comPortDefault=" + comPortDefault + ", comPortAutoDetect=" + comPortAutoDetect.ToString());

            return new ConnectResult(false, "");
        }

        internal void Disconnect()
        {
            traceLogger.LogMessage("Disconnect called", "");
            serialConnection.Connected = false;
            isConnected = false;
        }

        internal int Brightness
        {
            get
            {
                return 0;
            }
            set
            {
                // TODO: add implementation
            }
        }

        public void Dispose()
        {
            traceLogger.Enabled = false;
            traceLogger.Dispose();
            traceLogger = null;
            serialConnection.Dispose();
        }

        internal struct ConnectResult
        {
            internal bool IsConnected { get; }
            internal string ComPort { get; }

            internal ConnectResult(bool isConnected, string comPort)
            {
                IsConnected = isConnected;
                ComPort = comPort;
            }
        }
    }
}
