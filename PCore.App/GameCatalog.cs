using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace PCore.App
{
    public sealed class GameEntry
    {
        public string Name { get; set; }
        public string InstallLocation { get; set; }
        public string Publisher { get; set; }
    }

    public sealed class GameCatalog
    {
        private static readonly string[] GameHints = { "steam", "epic games", "ubisoft", "battle.net", "riot", "ea app", "rockstar", "minecraft", "war thunder", "valorant", "counter-strike", "fortnite" };

        public IList<GameEntry> FindInstalledGames()
        {
            var games = new Dictionary<string, GameEntry>(StringComparer.OrdinalIgnoreCase);
            ReadUninstall(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", games);
            ReadUninstall(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", games);
            ReadUninstall(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", games);
            var result = new List<GameEntry>();
            foreach (GameEntry entry in games.Values) result.Add(entry);
            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        private static void ReadUninstall(RegistryKey root, string path, IDictionary<string, GameEntry> games)
        {
            try
            {
                using (RegistryKey key = root.OpenSubKey(path, false))
                {
                    if (key == null) return;
                    foreach (string subName in key.GetSubKeyNames())
                    {
                        using (RegistryKey app = key.OpenSubKey(subName, false))
                        {
                            if (app == null) continue;
                            string name = Convert.ToString(app.GetValue("DisplayName"));
                            if (string.IsNullOrWhiteSpace(name) || !LooksLikeGameOrLauncher(name)) continue;
                            string location = Convert.ToString(app.GetValue("InstallLocation"));
                            games[name] = new GameEntry { Name = name, InstallLocation = location, Publisher = Convert.ToString(app.GetValue("Publisher")) };
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (System.Security.SecurityException) { }
        }

        private static bool LooksLikeGameOrLauncher(string name)
        {
            foreach (string hint in GameHints)
                if (name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}