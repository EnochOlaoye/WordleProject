using System;
using System.Text.Json;

namespace Wordle
{
    public class Player
    {
        public string Name { get; set; } = string.Empty;

        public static async Task<string> GetPlayerName()
        {
            try
            {
                string path = Path.Combine(FileSystem.AppDataDirectory, "player.json");

                if (File.Exists(path))
                {
                    string json = await File.ReadAllTextAsync(path);
                    var player = JsonSerializer.Deserialize<Player>(json);
                    System.Diagnostics.Debug.WriteLine($"Loaded existing player: {player?.Name}");
                    return player?.Name ?? string.Empty;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No existing player file found");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting player name: {ex.Message}");
                return string.Empty;
            }
        }

        public static async Task SavePlayerName(string name)
        {
            try
            {
                string path = Path.Combine(FileSystem.AppDataDirectory, "player.json");
                var player = new Player { Name = name };
                string json = JsonSerializer.Serialize(player);

                await File.WriteAllTextAsync(path, json);

                string historyPath = Path.Combine(FileSystem.AppDataDirectory, $"{name}_history.json");
                if (!File.Exists(historyPath))
                {
                    var history = new PlayerHistory { PlayerName = name };
                    await File.WriteAllTextAsync(historyPath, JsonSerializer.Serialize(history));
                    System.Diagnostics.Debug.WriteLine($"Created new player files for: {name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving player name: {ex.Message}");
            }
        }

        public static bool PlayerExists(string name)
        {
            try
            {
                string path = Path.Combine(FileSystem.AppDataDirectory, $"{name}_history.json");
                return File.Exists(path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking if player exists: {ex.Message}");
                return false;
            }
        }

        public static async Task<List<string>> GetExistingPlayers()
        {
            var players = new List<string>();
            try
            {
                await Task.Run(() =>
                {
                    var directory = new DirectoryInfo(FileSystem.AppDataDirectory);
                    var files = directory.GetFiles("*_history.json");

                    foreach (var file in files)
                    {
                        string playerName = Path.GetFileNameWithoutExtension(file.Name);
                        playerName = playerName.Replace("_history", "");
                        players.Add(playerName);
                    }
                });

                return players.OrderBy(p => p).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting players: {ex.Message}");
                return new List<string>();
            }
        }

        public static async Task UpdateLastPlayed(string name)
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, $"{name}_lastplayed.txt");
            await File.WriteAllTextAsync(path, DateTime.Now.ToString());
        }
    }

    public class PlayerPreferences
    {
        public bool IsDarkMode { get; set; }
        public bool ShowHints { get; set; }
        public bool SoundEnabled { get; set; }

        public static async Task SavePreferences(string playerName, PlayerPreferences prefs)
        {
            string json = JsonSerializer.Serialize(prefs);
            string path = Path.Combine(FileSystem.AppDataDirectory, $"{playerName}_prefs.json");
            await File.WriteAllTextAsync(path, json);
        }
    }
}