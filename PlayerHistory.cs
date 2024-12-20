using System.Collections.ObjectModel;
using System.Text.Json;

namespace Wordle
{
    public class PlayerHistory
    {
        public string PlayerName { get; set; }
        public ObservableCollection<GameAttempt> Attempts { get; set; }

        public PlayerHistory()
        {
            PlayerName = string.Empty;
            Attempts = new ObservableCollection<GameAttempt>();
        }

        public void AddAttempt(GameAttempt attempt)
        {
            Attempts.Insert(0, attempt); // Add new attempts at the start
            Save();
        }

        public IEnumerable<GameAttempt> GetSortedAttempts()
        {
            return Attempts.OrderByDescending(a => a.Timestamp);
        }

        // Save history to JSON file with player name
        public async void Save()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(this, options);
                string playerName = await Player.GetPlayerName();
                string path = Path.Combine(FileSystem.AppDataDirectory, $"{playerName}_history.json");
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving history: {ex.Message}");
            }
        }

        // Load history from JSON file for specific player
        public static async Task<PlayerHistory> Load()
        {
            try
            {
                string playerName = await Player.GetPlayerName();
                string path = Path.Combine(FileSystem.AppDataDirectory, $"{playerName}_history.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var history = JsonSerializer.Deserialize<PlayerHistory>(json, options) ?? new PlayerHistory();
                    history.PlayerName = playerName;
                    return history;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading history: {ex.Message}");
            }
            var newHistory = new PlayerHistory();
            newHistory.PlayerName = await Player.GetPlayerName();
            return newHistory;
        }
    }
}