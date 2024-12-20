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

        // Calculate win percentage
        public double WinPercentage => GamesPlayed > 0 ? (double)GamesWon / GamesPlayed * 100 : 0;

        // Constructor
        public SaveGame()
        {
            GuessDistribution = new Dictionary<int, int>();
        }

        // Load saved game data from device storage
        public static SaveGame Load()
        {
            string saveJson = Preferences.Default.Get("SaveGame", "");
            if (string.IsNullOrEmpty(saveJson))
            {
                return new SaveGame();
            }
            return JsonSerializer.Deserialize<SaveGame>(saveJson);
        }

        // Save game data to device storage
        public void Save()
        {
            string saveJson = JsonSerializer.Serialize(this);
            Preferences.Default.Set("SaveGame", saveJson);
        }

        // Update game statistics after a game ends
        public void UpdateGame(bool won, int guesses)
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
    }
}