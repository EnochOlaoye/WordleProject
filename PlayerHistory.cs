using System.Collections.ObjectModel;
using System.Text.Json;

namespace Wordle
{
    public class GameAttemptWord
    {
        public string Word { get; set; } // Word to guess
        public int GuessCount { get; set; } // Number of guesses
        public DateTime DatePlayed { get; set; } // Date and time of the game
        public List<string> GuessHistory { get; set; } // List of guesses made
        public GameDifficulty Difficulty { get; set; } // Difficulty level of the game

        // Default constructor to initialize the date and list of guesses 
        public GameAttemptWord(string word, int guesses, List<string> history, GameDifficulty difficulty)
        {
            Word = word; // Set the word to guess
            GuessCount = guesses; // Set the number of guesses
            GuessHistory = history ?? new List<string>(); // Set the list of guesses
            DatePlayed = DateTime.Now; // Set the date and time
            Difficulty = difficulty; // Set the difficulty level
        }
    }

    // Public class to manage and organize the game attempts
    public class PlayerHistory
    {
        public string PlayerName { get; set; } // Name of the player
        private List<GameAttempt> Attempts { get; set; } = new List<GameAttempt>(); // List of game attempts

        // Default constructor to initialize the list of user's game attempts 
        public PlayerHistory()
        {
            Attempts = new List<GameAttempt>(); // Initialize the list of game attempts
        }

        // Add a game attempt to the list
        public void AddAttempt(GameAttempt attempt)
        {
            if (attempt != null) // Check if the attempt is not null
            {
                Attempts.Add(attempt); // Add the attempt to the list
            }          
        }

        // Get the list of game attempts
        public List<GameAttempt> GetSortedAttempts()
        {
            return Attempts?.OrderByDescending(a => a.Date).ToList() ?? new List<GameAttempt>(); // Return the list of game attempts
        }

    }
}