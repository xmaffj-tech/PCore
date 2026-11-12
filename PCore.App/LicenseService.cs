using System;

namespace PCore.App
{
    public enum LicenseTier { Free, Pro, Max }

    // Until a payment provider and server-side license validation are configured,
    // the application deliberately remains in Free mode. No local flag can unlock paid features.
    public sealed class LicenseService
    {
        public LicenseTier CurrentTier { get { return LicenseTier.Free; } }
        public bool IsFeatureAvailable(string feature)
        {
            if (string.IsNullOrWhiteSpace(feature)) return false;
            return feature == "basic-monitoring" || feature == "process-list" ||
                   feature == "network-monitoring" || feature == "basic-history" ||
                   feature == "storage-overview";
        }
        public string Status { get { return "FREE — платёжная система ещё не подключена"; } }
    }
}