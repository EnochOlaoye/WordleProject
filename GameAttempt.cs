using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordle
{
    public enum GameDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    public class GameAttempt
    {
        public string Word { get; set; }
        public int GuessCount { get; set; }
        public bool IsWin { get; set; }
        public DateTime Date { get; set; }
        public GameDifficulty Difficulty { get; set; }
        public List<string> Guesses { get; set; } = new List<string>();

        // Default constructor
        public GameAttempt()
        {
            Date = DateTime.Now;
            Guesses = new List<string>();
        }

        // Constructor with parameters
        public GameAttempt(string word, int guesses, List<string> history)
        {
            Word = word;
            GuessCount = guesses;
            Guesses = history ?? new List<string>();
            Date = DateTime.Now;
            IsWin = true;
        }
    }
}