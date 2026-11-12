using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace PCore.App
{
    public sealed class NetworkMonitor
    {
        private long _lastReceived;
        private long _lastSent;
        private DateTime _lastSample;
        private bool _hasSample;

        public NetworkSnapshot Read()
        {
            long received = 0;
            long sent = 0;
            int activeAdapters = 0;

            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    continue;

                try
                {
                    IPv4InterfaceStatistics stats = adapter.GetIPv4Statistics();
                    received += stats.BytesReceived;
                    sent += stats.BytesSent;
                    activeAdapters++;
                }
                catch (NetworkInformationException) { }
            }

            DateTime now = DateTime.UtcNow;
            double? download = null;
            double? upload = null;

            if (_hasSample)
            {
                double seconds = (now - _lastSample).TotalSeconds;
                if (seconds > 0 && received >= _lastReceived && sent >= _lastSent)
                {
                    download = (received - _lastReceived) / seconds;
                    upload = (sent - _lastSent) / seconds;
                }
            }

            _lastReceived = received;
            _lastSent = sent;
            _lastSample = now;
            _hasSample = true;

            return new NetworkSnapshot
            {
                DownloadBytesPerSecond = download,
                UploadBytesPerSecond = upload,
                TotalReceivedBytes = received,
                TotalSentBytes = sent,
                ActiveAdapters = activeAdapters
            };
        }
    }

    public sealed class NetworkSnapshot
    {
        public double? DownloadBytesPerSecond { get; set; }
        public double? UploadBytesPerSecond { get; set; }
        public long TotalReceivedBytes { get; set; }
        public long TotalSentBytes { get; set; }
        public int ActiveAdapters { get; set; }
    }
}