using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordle
{
    public class GameAttempt
    {
        public string Word { get; set; } = string.Empty;
        public int GuessCount { get; set; }
        public DateTime DatePlayed { get; set; } = DateTime.Now;
        public List<string> GuessHistory { get; set; } = new List<string>();

        public GameAttempt(string word, int guessCount, List<string> guessHistory)
        {
            Word = word;
            GuessCount = guessCount;
            GuessHistory = guessHistory;
        }

        public GameAttempt() { }
    }
}
