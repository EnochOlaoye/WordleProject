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
        public Dictionary<int, int> GuessDistribution { get; set; }
        public PlayerHistory History { get; set; }

        // Add a property to store the current player's name
        private string CurrentPlayer { get; set; }

        // Calculate win percentage
        public double WinPercentage => GamesPlayed > 0 ? (double)GamesWon / GamesPlayed * 100 : 0;

        // Constructor to initialize the game statistics and player history when game is saved
        public SaveGame()
        {
            GuessDistribution = new Dictionary<int, int>(); // Initialize the guess distribution dictionary when the game is saved
            History = new PlayerHistory(); // Initialize the player history object when the game is saved
            CurrentPlayer = "Player"; // Default value
        }

        // Save the game statistics to a JSON file
        public void Save(string playerName)
        {
            string savePath = Path.Combine(FileSystem.AppDataDirectory, $"{playerName}_save.json"); // Combine the AppDataDirectory with the file name
            string jsonString = JsonSerializer.Serialize(this); // Serialize the game statistics to a JSON string
            File.WriteAllText(savePath, jsonString); // Write the JSON string to the file
        }

        // Load the game statistics from a JSON file
        public static async Task<SaveGame> Load(string playerName)
        {
            string savePath = Path.Combine(FileSystem.AppDataDirectory, $"{playerName}_save.json"); // Combine the AppDataDirectory with the file name

            // Check if the file exists
            if (!File.Exists(savePath))
            {
                return new SaveGame // Return a new instance of SaveGame if the file does not exist
                {
                    History = new PlayerHistory { PlayerName = playerName } // Initialize the player history object with the player's name
                };
            }

            string jsonString = await File.ReadAllTextAsync(savePath); // Read the JSON string from the file
            return JsonSerializer.Deserialize<SaveGame>(jsonString) ?? new SaveGame(); // Deserialize the JSON string to a SaveGame object
        }

        // Update game statistics after a game ends
        public void UpdateStats(bool won, int guesses, string playerName)
        {
            GamesPlayed++; // Increment the number of games played
            CurrentPlayer = playerName; // For the current player's name

            // Update game statistics based on the game outcome
            if (won)
            {
                GamesWon++; // Increment the number of games won
                CurrentStreak++; // Increment the current streak
                MaxStreak = Math.Max(MaxStreak, CurrentStreak); // Update the maximum streak

                // Update the guess distribution dictionary
                if (!GuessDistribution.ContainsKey(guesses))
                {
                    GuessDistribution[guesses] = 0; // Initialize the guess count for the number of guesses
                }
                GuessDistribution[guesses]++; // Increment the guess count for the number of guesses
            }

            // If the game is lost reset the current streak
            else
            {
                CurrentStreak = 0; // Reset the current streak 
            }

            Save(playerName); // Save the updated game statistics
        }

        // Addin game attempt to the player's history
        public void AddGameAttempt(string word, int guesses, List<string> history, string playerName)
        {
            var attempt = new GameAttempt(word, guesses, history); // Create a new game attempt
            History.AddAttempt(attempt); // Add the game attempt to the player's history
            Save(playerName); // Save Player name and update game statistics
        }
    }
}