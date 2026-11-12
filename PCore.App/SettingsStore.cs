using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace PCore.App
{
    [DataContract]
    public sealed class UserSettings
    {
        [DataMember] public int RefreshSeconds { get; set; }
        [DataMember] public bool StartWithWindows { get; set; }
        [DataMember] public bool NotificationsEnabled { get; set; }
        [DataMember] public string Language { get; set; }
        [DataMember] public string Theme { get; set; }

        public static UserSettings CreateDefault()
        {
            return new UserSettings { RefreshSeconds = 1, StartWithWindows = false, NotificationsEnabled = true, Language = "Русский", Theme = "Тёмная" };
        }
    }

    public sealed class SettingsStore
    {
        private readonly string _path;
        public SettingsStore()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PCore");
            Directory.CreateDirectory(folder);
            _path = Path.Combine(folder, "settings.json");
        }

        public UserSettings Load()
        {
            if (!File.Exists(_path)) return UserSettings.CreateDefault();
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(UserSettings));
                using (var stream = File.OpenRead(_path))
                    return (UserSettings)serializer.ReadObject(stream) ?? UserSettings.CreateDefault();
            }
            catch (Exception) { return UserSettings.CreateDefault(); }
        }

        public void Save(UserSettings settings)
        {
            var serializer = new DataContractJsonSerializer(typeof(UserSettings));
            string temporary = _path + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                serializer.WriteObject(stream, settings);
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(temporary, _path);
        }
    }
}