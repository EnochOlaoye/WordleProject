//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Text.Json;

namespace Wordle
{
    public class SaveGame
    {
        // Properties to store game statistics
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int CurrentStreak { get; set; }
        public int MaxStreak { get; set; }
        public Dictionary<int, int> GuessDistribution { get; set; }
        private PlayerHistory _history;
        public PlayerHistory History
        {
            get => _history;
            set => _history = value;
        }

        // Calculate win percentage
        public double WinPercentage => GamesPlayed > 0 ? (double)GamesWon / GamesPlayed * 100 : 0;

        // Constructor
        private SaveGame()
        {
            GuessDistribution = new Dictionary<int, int>();
        }

        public static async Task<SaveGame> Create()
        {
            var save = new SaveGame();
            save._history = await PlayerHistory.Load();
            return save;
        }

        // Load saved game data from device storage
        public static async Task<SaveGame> Load()
        {
            string saveJson = Preferences.Default.Get("SaveGame", "");
            if (string.IsNullOrEmpty(saveJson))
            {
                return await Create();
            }
            var save = JsonSerializer.Deserialize<SaveGame>(saveJson) ?? await Create();
            save._history = await PlayerHistory.Load();
            return save;
        }

        // Save game data to device storage
        public void Save()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            string saveJson = JsonSerializer.Serialize(this, options);
            Preferences.Default.Set("SaveGame", saveJson);
        }

        // Update game statistics after a game ends
        public void UpdateStats(bool won, int guesses)
        {
            GamesPlayed++;

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

            Save();
        }

        public void AddGameAttempt(string word, int guesses, List<string> history)
        {
            var attempt = new GameAttempt(word, guesses, history);
            _history.AddAttempt(attempt);
            Save();
        }
    }
}