using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordle
{
    public class GameAttempt
    {
        public DateTime Timestamp { get; set; }
        public string CorrectWord { get; set; }
        public int GuessCount { get; set; }
        public List<string> GuessHistory { get; set; }

        // Add parameterless constructor for JSON deserialization
        public GameAttempt()
        {
            GuessHistory = new List<string>();
        }

        // Keep the existing constructor
        public GameAttempt(string word, int guesses, List<string> history)
        {
            Timestamp = DateTime.Now;
            CorrectWord = word;
            GuessCount = guesses;
            GuessHistory = history ?? new List<string>();
        }
    }
}
