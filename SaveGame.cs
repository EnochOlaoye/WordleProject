using System.Text.Json;
using System.IO;

namespace Wordle
{
    public class SaveGame
    {
        // Properties to store game statistics
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int CurrentStreak { get; set; }
        public int MaxStreak { get; set; }
        public Dictionary<int, int> GuessDistribution { get; set; } = new();
        public PlayerHistory? History { get; set; }

        // Add a property to store the current player's name
        private string CurrentPlayer { get; set; }

        // Calculate win percentage
        public double WinPercentage => GamesPlayed > 0 ? (double)GamesWon / GamesPlayed * 100 : 0;

        // Constructor
        public SaveGame()
        {
            GuessDistribution = new Dictionary<int, int>();
            History = new PlayerHistory();
            CurrentPlayer = "Player"; // Default value
        }

        public void Save(string playerName)
        {
            try
            {
                string path = Path.Combine(FileSystem.AppDataDirectory, $"{playerName}_save.json");
                string json = JsonSerializer.Serialize(this);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving game: {ex.Message}");
                throw;
            }
        }

        public static async Task<SaveGame> Load(string playerName)
        {
            try
            {
                string path = Path.Combine(FileSystem.AppDataDirectory, $"{playerName}_save.json");
                if (!File.Exists(path))
                {
                    return new SaveGame { History = new PlayerHistory { PlayerName = playerName } };
                }

                string json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<SaveGame>(json) ??
                       new SaveGame { History = new PlayerHistory { PlayerName = playerName } };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading save: {ex.Message}");
                return new SaveGame { History = new PlayerHistory { PlayerName = playerName } };
            }
        }

        // Update game statistics after a game ends
        public void UpdateStats(bool won, int guesses, string playerName)
        {
            GamesPlayed++;
            CurrentPlayer = playerName;

            if (won)
            {
                GamesWon++;
                CurrentStreak++;
                MaxStreak = Math.Max(MaxStreak, CurrentStreak);

                if (!GuessDistribution.ContainsKey(guesses))
                {
                    GuessDistribution[guesses] = 0;
                }
                GuessDistribution[guesses]++;
            }
            else
            {
                CurrentStreak = 0;
            }

            Save(playerName);
        }

        public void AddGameAttempt(string word, int guesses, List<string> history, string playerName)
        {
            var attempt = new GameAttempt(word, guesses, history);
            History.AddAttempt(attempt);
            Save(playerName);
        }
    }
}