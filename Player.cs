using System;
using System.Text.Json;

namespace Wordle
{
    public class Player
    {
        public string Name { get; set; } = string.Empty; // Default to empty string to avoid null reference exceptions

        // Default constructor required for deserialization from JSON
        public static async Task<string> GetPlayerName()
        {
            // Path to the player.json file
            try
            {
                string path = Path.Combine(FileSystem.AppDataDirectory, "player.json"); // Combine the AppDataDirectory with the file name

                // Check if the file exists
                if (File.Exists(path))
                {
                    string json = await File.ReadAllTextAsync(path);
                    var player = JsonSerializer.Deserialize<Player>(json);
                    System.Diagnostics.Debug.WriteLine($"Loaded existing player: {player?.Name}");
                    return player?.Name ?? string.Empty;
                }

                // If the file doesn't exist, return an empty string
                else
                {
                    System.Diagnostics.Debug.WriteLine("No existing player file found");
                    return string.Empty;
                }
            }

            // Catch any exceptions and log them to the debug output 
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting player name: {ex.Message}");
                return string.Empty;
            }
        }

        // Save the player name to a JSON file 
        public static async Task SavePlayerName(string name)
        {
            // Combine the AppDataDirectory with the file name
            try
            {
                string path = Path.Combine(FileSystem.AppDataDirectory, "player.json");
                var player = new Player { Name = name };
                string json = JsonSerializer.Serialize(player);

                await File.WriteAllTextAsync(path, json); // Write the JSON to the file

                string historyPath = Path.Combine(FileSystem.AppDataDirectory, $"{name}_history.json"); // Combine the AppDataDirectory with the file name

                // Check if the player history file exists
                if (!File.Exists(historyPath))
                {
                    var history = new PlayerHistory { PlayerName = name };
                    await File.WriteAllTextAsync(historyPath, JsonSerializer.Serialize(history));
                    System.Diagnostics.Debug.WriteLine($"Created new player files for: {name}");
                }
            }

            // Catch any exceptions and log them to the debug output
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving player name: {ex.Message}");
            }
        }

        // Check if a player exists
        public static bool PlayerExists(string name)
        {
            // try-catch block to handle exceptions that may occur when checking if a player exists
            try
            {
                string path = Path.Combine(FileSystem.AppDataDirectory, $"{name}_history.json"); // Combine the AppDataDirectory with the file name
                return File.Exists(path); // Return true if the file exists, otherwise return false
            }

            // Catch any exceptions and log them to the debug output
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking if player exists: {ex.Message}");
                return false;
            }           
        }

        // Get the existing players
        public static async Task<List<string>> GetExistingPlayers()
        {
            var players = new List<string>(); // Create a new list to store the player names
            // try-catch block to handle exceptions that may occur when attempting to get the existing players
            try
            {
                // Run the following code on a background thread
                await Task.Run(() =>
                {
                    var directory = new DirectoryInfo(FileSystem.AppDataDirectory); // Get the directory where the player files are stored
                    var files = directory.GetFiles("*_history.json"); // Get all files that end with "_history.json"

                    // Loop through each file and add the player name to the list
                    foreach (var file in files)
                    {
                        string playerName = Path.GetFileNameWithoutExtension(file.Name); // Get the player name from the file name
                        playerName = playerName.Replace("_history", ""); // Remove the "_history" part of the name
                        players.Add(playerName); // Add the player name to the list
                    }
                });

                return players.OrderBy(p => p).ToList(); // Return the list of players in alphabetical order
            }

            // Catch any exceptions and log them to the debug output
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting players: {ex.Message}");
                return new List<string>();
            }
        }

        // Update the "last played" timestamp for a specific player
        public static async Task UpdateLastPlayed(string name)
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, $"{name}_lastplayed.txt"); // Create the file path by combining the application's data directory and the player's history file name
            await File.WriteAllTextAsync(path, DateTime.Now.ToString()); // Write the current date and time to the file
        }
    }

    // Class to store player preferences
    public class PlayerPreferences
    {
        public bool IsDarkMode { get; set; } // Property to store the dark mode preference
        public bool ShowHints { get; set; } // Property to store the hint preference
        public bool SoundEnabled { get; set; } // Property to store the sound preference

        // Default constructor to set default values for the preferences 
        public static async Task SavePreferences(string playerName, PlayerPreferences prefs)
        {
            string json = JsonSerializer.Serialize(prefs); // Serialize the preferences object to JSON
            string path = Path.Combine(FileSystem.AppDataDirectory, $"{playerName}_prefs.json"); // Combine the AppDataDirectory with the file name
            await File.WriteAllTextAsync(path, json); // Write the JSON to the file 
        }
    }
}