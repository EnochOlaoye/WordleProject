using System.Collections.ObjectModel;
using System.Text.Json;

namespace Wordle
{
    public class GameAttemptWord
    {
        public string Word { get; set; }
        public int GuessCount { get; set; }
        public DateTime DatePlayed { get; set; }
        public List<string> GuessHistory { get; set; }
        public GameDifficulty Difficulty { get; set; }

        public GameAttemptWord(string word, int guesses, List<string> history, GameDifficulty difficulty)
        {
            Word = word;
            GuessCount = guesses;
            GuessHistory = history ?? new List<string>();
            DatePlayed = DateTime.Now;
            Difficulty = difficulty;
        }
    }

    public class PlayerHistory
    {
        public string PlayerName { get; set; }
        private List<GameAttempt> Attempts { get; set; } = new List<GameAttempt>();

        public PlayerHistory()
        {
            Attempts = new List<GameAttempt>();
        }

        public void AddAttempt(GameAttempt attempt)
        {
            if (attempt != null)
            {
                Attempts.Add(attempt);
            }
        }

        public List<GameAttempt> GetSortedAttempts()
        {
            return Attempts?.OrderByDescending(a => a.Date).ToList() ?? new List<GameAttempt>();
        }
    }
}