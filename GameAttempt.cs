using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordle
{
    // Enumeration Sets Diffculties Level
    public enum GameDifficulty
    {
        Easy, // Easiest level
        Medium, // Moderate level
        Hard // Hardest level
    }

    // Public class to manage and organize the game attempts
    public class GameAttempt
    {
        public string Word { get; set; } // Word to guess
        public int GuessCount { get; set; } // Number of guesses
        public bool IsWin { get; set; } // Whether the game was won
        public DateTime Date { get; set; } // Date and time of the game
        public GameDifficulty Difficulty { get; set; } // Difficulty level of the game
        public List<string> Guesses { get; set; } = new List<string>(); // List of guesses made

        // Default constructor to initialize the date and list of guesses
        public GameAttempt()
        {
            Date = DateTime.Now; // Initialize the date and time
            Guesses = new List<string>(); // Initialize the list of guesses
        }

        // Constructor with parameters to set the word, number of guesses, and list of guesses
        public GameAttempt(string word, int guesses, List<string> history)
        {
            Word = word; // Set the word to guess
            GuessCount = guesses; // Set the number of guesses
            Guesses = history ?? new List<string>(); // Set the list of guesses
            Date = DateTime.Now; // Set the date and time
            IsWin = true; // Check to see if user has won the game
        }
    }
}