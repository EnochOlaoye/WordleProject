using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordle
{
    public class GameAttempt
    {
        public string CorrectWord { get; set; }
        public int AttemptCount { get; set; }
        public List<string> GuessHistory { get; set; }
        public DateTime Timestamp { get; set; }

        public GameAttempt(string correctWord, int attemptCount, List<string> guessHistory)
        {
            CorrectWord = correctWord ?? string.Empty;
            AttemptCount = attemptCount;
            GuessHistory = guessHistory ?? new List<string>();
            Timestamp = DateTime.Now;
        }

        // Add parameterless constructor for deserialization
        public GameAttempt()
        {
            CorrectWord = string.Empty;
            GuessHistory = new List<string>();
            Timestamp = DateTime.Now;
        }
    }
}
