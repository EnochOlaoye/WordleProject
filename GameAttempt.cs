using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordle
{
    public class GameAttempt
    {
        public string Word { get; set; }
        public int GuessCount { get; set; }
        public DateTime DatePlayed { get; set; }
        public List<string> GuessHistory { get; set; }

        public GameAttempt()
        {
            DatePlayed = DateTime.Now;
            GuessHistory = new List<string>();
        }

        public GameAttempt(string word, int guesses, List<string> history)
        {
            Word = word;
            GuessCount = guesses;
            GuessHistory = history ?? new List<string>();
            DatePlayed = DateTime.Now;
        }
    }
}
