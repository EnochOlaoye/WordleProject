using System.Net.Http;
using System.IO;
using Microsoft.Maui.Controls;
using System.Text.Json;
using System.Text;

namespace Wordle
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        private const string ThemePreferenceKey = "IsDarkMode";
        //private List<string> wordList;
        private List<string>? wordList;
        private const string WordsFileName = "words.txt";
        private const string WordsUrl = "https://raw.githubusercontent.com/DonH-ITS/jsonfiles/main/words.txt";
        private readonly Color darkModeBackground = Colors.Black;
        private readonly Color darkModeForeground = Colors.White;
        private readonly Color lightModeBackground = Colors.White;
        private readonly Color lightModeForeground = Colors.Black;
        //private string targetWord;
        private string targetWord = string.Empty;
        private readonly Color correctColor = Colors.Green;       // Letter is correct and in right position
        private readonly Color presentColor = Colors.Yellow;      // Letter exists but in wrong position
        private readonly Color incorrectColor = Colors.Gray;      // Letter is not in the word
        private bool won = false;
        private DateTime gameStartTime;
        private bool isDailyChallenge = false;
        private const string DAILY_CHALLENGE_KEY = "DailyChallenge_";

        // Add this static property
        public static bool IsDarkMode
        {
            get => Preferences.Default.Get("IsDarkMode", false);
            set => Preferences.Default.Set("IsDarkMode", value);
        }

        /*private enum GameDifficulty
        {
            Easy,
            Medium,
            Hard
        }*/

        private GameDifficulty currentDifficulty = GameDifficulty.Medium;

        private int hintsRemaining = 2; // Track available hints
        private bool usedHintLastTurn = false; // Prevent consecutive hint usage

        public MainPage()
        {
            InitializeComponent();
            ShowStartPrompt();
        }

        private async void ShowStartPrompt()
        {
            try
            {
                string choice;
                do
                {
                    choice = await DisplayActionSheet(
                        "Welcome to Wordle!",
                        null,  // No cancel option
                        null,  // No destruction option
                        "Continue Previous Game",
                        "Load Saved Game",
                        "Start New Game",
                        "Daily Challenge");

                    if (string.IsNullOrEmpty(choice))
                    {
                        await DisplayAlert("Required", "Please select an option to continue", "OK");
                    }

                } while (string.IsNullOrEmpty(choice));

                // Select difficulty before proceeding (except for Daily Challenge)
                if (choice != "Daily Challenge")
                {
                    string difficultyChoice = await DisplayActionSheet(
                        "Select Difficulty",
                        null,  // No cancel option
                        null,  // No destruction option
                        "Easy Mode (6 guesses, with hints)",
                        "Medium Mode (Standard 6 guesses)",
                        "Hard Mode (4 guesses, must reuse correct letters)");

                    switch (difficultyChoice)
                    {
                        case "Easy Mode (6 guesses, with hints)":
                            currentDifficulty = GameDifficulty.Easy;
                            count = 1;
                            MaxGuesses = 6;
                            break;
                        case "Medium Mode (Standard 6 guesses)":
                            currentDifficulty = GameDifficulty.Medium;
                            count = 1;
                            MaxGuesses = 6;
                            break;
                        case "Hard Mode (4 guesses, must reuse correct letters)":
                            currentDifficulty = GameDifficulty.Hard;
                            count = 1;
                            MaxGuesses = 4;
                            break;
                    }
                }

                // Initialize game first
                await LoadWordsAsync();

                // Handle different game modes
                switch (choice)
                {
                    case "Daily Challenge":
                        await StartDailyChallenge();
                        break;

                    case "Load Saved Game":
                        var players = await Player.GetExistingPlayers();
                        if (players.Count == 0)
                        {
                            await DisplayAlert("No Saved Games", "No saved games found. Starting new game.", "OK");
                            await CreateNewPlayer();
                        }
                        else
                        {
                            string selectedPlayer = await DisplayActionSheet(
                                "Select Saved Game",
                                "Cancel",
                                null,
                                players.ToArray());

                            if (!string.IsNullOrEmpty(selectedPlayer) && selectedPlayer != "Cancel")
                            {
                                await Player.SavePlayerName(selectedPlayer);
                                var save = await SaveGame.Load(selectedPlayer);
                                await DisplayAlert("Game Loaded",
                                    $"Welcome back, {selectedPlayer}!\n" +
                                    $"Games Won: {save.GamesWon}\n" +
                                    $"Current Streak: {save.CurrentStreak}",
                                    "OK");
                            }
                            else
                            {
                                // If user cancels loading, default to new game
                                await CreateNewPlayer();
                            }
                        }
                        break;

                    case "Start New Game":
                        string newName = await DisplayPromptAsync(
                            "New Game",
                            "Enter player name:",
                            maxLength: 20,
                            keyboard: Keyboard.Text);

                        if (!string.IsNullOrEmpty(newName))
                        {
                            await Player.SavePlayerName(newName);
                            var newSave = new SaveGame
                            {
                                GamesPlayed = 0,
                                GamesWon = 0,
                                CurrentStreak = 0,
                                MaxStreak = 0,
                                GuessDistribution = new Dictionary<int, int>(),
                                History = new PlayerHistory { PlayerName = newName }
                            };
                            newSave.Save(newName);

                            var newHistory = new PlayerHistory { PlayerName = newName };
                            string historyPath = Path.Combine(FileSystem.AppDataDirectory, $"{newName}_history.json");
                            await File.WriteAllTextAsync(historyPath, JsonSerializer.Serialize(newHistory));
                        }
                        break;

                    case "Continue Previous Game":
                        var currentPlayer = await Player.GetPlayerName();
                        if (string.IsNullOrEmpty(currentPlayer))
                        {
                            await DisplayAlert("No Previous Game", "No previous game found. Starting new game.", "OK");
                            await CreateNewPlayer();
                        }
                        else
                        {
                            var currentSave = await SaveGame.Load(currentPlayer);
                            await DisplayAlert("Game Continued",
                                $"Welcome back, {currentPlayer}!\n" +
                                $"Games Won: {currentSave.GamesWon}\n" +
                                $"Current Streak: {currentSave.CurrentStreak}",
                                "OK");
                        }
                        break;
                }

                // Update UI
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ResetAllEntries();
                    ApplyTheme();
                    Row1Letter1?.Focus();
                    if (choice != "Daily Challenge")
                    {
                        ResultLabel.Text = $"Playing on {currentDifficulty} Mode";
                    }
                });

                UpdateUIForDifficulty();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ShowStartPrompt: {ex.Message}");
                await DisplayAlert("Error", "Failed to start game", "OK");
            }
        }

        private async Task ResetGameState()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Reset game counter
                count = 1;

                // Reset all entries and borders
                for (int row = 1; row <= 6; row++)
                {
                    for (int col = 1; col <= 5; col++)
                    {
                        var entry = this.FindByName<Entry>($"Row{row}Letter{col}");
                        var border = this.FindByName<Border>($"Border{row}Letter{col}");

                        if (entry != null)
                        {
                            entry.Text = "";
                            entry.IsEnabled = true;

                            // Set text colors
                            if (row == 1 && col == 1)
                            {
                                entry.TextColor = IsDarkMode ? Colors.White : Colors.Black;
                            }
                            else
                            {
                                entry.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground;
                            }
                        }

                        if (border != null)
                        {
                            border.BackgroundColor = Colors.Transparent;
                            border.Stroke = Colors.Gray;
                        }
                    }
                }

                // Apply theme
                ApplyTheme();

                // Focus first entry
                Row1Letter1?.Focus();

                // Reset result label
                ResultLabel.Text = "Results will appear here";
            });
        }

        private void DisableGameControls()
        {
            // Disable all entry controls and buttons until player chooses
            for (int row = 1; row <= 6; row++)
            {
                for (int col = 1; col <= 5; col++)
                {
                    var entry = this.FindByName<Entry>($"Row{row}Letter{col}");
                    if (entry != null)
                    {
                        entry.IsEnabled = false;
                    }
                }
            }
            SubmitButton.IsEnabled = false;
        }

        private void EnableGameControls()
        {
            // Re-enable all entry controls and buttons
            for (int row = 1; row <= 6; row++)
            {
                for (int col = 1; col <= 5; col++)
                {
                    var entry = this.FindByName<Entry>($"Row{row}Letter{col}");
                    if (entry != null)
                    {
                        entry.IsEnabled = true;
                    }
                }
            }
            SubmitButton.IsEnabled = true;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplyTheme();
        }

        private async Task CheckAndLoginPlayer()
        {
            try
            {
                var existingPlayers = await Player.GetExistingPlayers();
                string currentPlayer = await Player.GetPlayerName();

                if (existingPlayers.Any())
                {
                    // Show options dialog
                    string choice = await DisplayActionSheet(
                        "Welcome to Wordle!",
                        "Cancel",
                        null,
                        $"Continue as {currentPlayer}",
                        "Start as New Player");

                    if (choice == "Start as New Player")
                    {
                        // Create new player
                        string newName = await DisplayPromptAsync(
                            "New Player",
                            "Enter your name:",
                            maxLength: 20,
                            keyboard: Keyboard.Text);

                        if (string.IsNullOrEmpty(newName))
                        {
                            newName = "Player";
                        }

                        // Create new player with fresh stats
                        await Player.SavePlayerName(newName);

                        // Reset all game statistics
                        var newSave = new SaveGame
                        {
                            GamesPlayed = 0,
                            GamesWon = 0,
                            CurrentStreak = 0,
                            MaxStreak = 0,
                            GuessDistribution = new Dictionary<int, int>(),
                            History = new PlayerHistory { PlayerName = newName }
                        };
                        newSave.Save(newName);

                        // Create fresh history file
                        var newHistory = new PlayerHistory { PlayerName = newName };
                        string historyPath = Path.Combine(FileSystem.AppDataDirectory, $"{newName}_history.json");
                        await File.WriteAllTextAsync(historyPath, JsonSerializer.Serialize(newHistory));

                        System.Diagnostics.Debug.WriteLine($"Created new player: {newName} with fresh stats");
                    }
                    else if (choice == $"Continue as {currentPlayer}")
                    {
                        // Continue with existing player and their stats
                        System.Diagnostics.Debug.WriteLine($"Continuing as existing player: {currentPlayer}");
                    }
                }
                else
                {
                    // First time launch - create new player
                    await CreateNewPlayer();
                }

                // Refresh the game state
                await InitializeGame();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CheckAndLoginPlayer: {ex.Message}");
                await DisplayAlert("Error", "Failed to setup player", "OK");
            }
        }

        private async Task CreateNewPlayer()
        {
            string name = await DisplayPromptAsync(
                "Welcome to Wordle!",
                "Please enter your name:",
                maxLength: 20,
                keyboard: Keyboard.Text);

            if (string.IsNullOrEmpty(name))
            {
                name = "Player";
            }

            await Player.SavePlayerName(name);

            // Create fresh save game data
            var newSave = new SaveGame
            {
                GamesPlayed = 0,
                GamesWon = 0,
                CurrentStreak = 0,
                MaxStreak = 0,
                GuessDistribution = new Dictionary<int, int>(),
                History = new PlayerHistory { PlayerName = name }
            };
            newSave.Save(name);

            // Create fresh history
            var newHistory = new PlayerHistory { PlayerName = name };
            string historyPath = Path.Combine(FileSystem.AppDataDirectory, $"{name}_history.json");
            await File.WriteAllTextAsync(historyPath, JsonSerializer.Serialize(newHistory));

            System.Diagnostics.Debug.WriteLine($"New player created: {name}");
        }

        private async Task SwitchPlayer()
        {
            var existingPlayers = await Player.GetExistingPlayers();
            existingPlayers.Add("New Player");

            string result = await DisplayActionSheet(
                "Choose Player",
                "Cancel",
                null,
                existingPlayers.ToArray());

            if (result == "New Player")
            {
                await CreateNewPlayer();
            }
            else if (!string.IsNullOrEmpty(result) && result != "Cancel")
            {
                await Player.SavePlayerName(result);
                var save = await SaveGame.Load(result);
                await DisplayAlert("Player Switched",
                    $"Welcome back, {result}!\n" +
                    $"Games Won: {save.GamesWon}",
                    "OK");
            }
        }

        // Add this to your toolbar/menu
        private async void OnSwitchPlayerClicked(object sender, EventArgs e)
        {
            await SwitchPlayer();
        }

        private void OnThemeToggleClicked(object sender, EventArgs e)
        {
            IsDarkMode = !IsDarkMode;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            // Set page background
            BackgroundColor = IsDarkMode ? darkModeBackground : lightModeBackground;

            //Update title color with theme
            TitleLabel.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground;

            // Update theme labels
            LightModeLabel.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground;
            DarkModeLabel.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground;
            LightModeLabel.IsVisible = !IsDarkMode;
            DarkModeLabel.IsVisible = IsDarkMode;

            // Update theme button
            ThemeButton.Source = IsDarkMode ? "lightbulb.png" : "darkbulb.png";

            // Update all other labels and controls
            ResultLabel.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground;

            // Force update first box
            Row1Letter1.TextColor = IsDarkMode ? Colors.White : Colors.Black;

            // Update all other entries
            for (int row = 1; row <= 6; row++)
            {
                for (int col = 1; col <= 5; col++)
                {
                    if (row == 1 && col == 1) continue; // Skip first box

                    var entry = this.FindByName<Entry>($"Row{row}Letter{col}");
                    if (entry != null)
                    {
                        entry.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground;
                    }
                }
            }
            // Update SearchBar colors
            WordSearchBar.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground;
            WordSearchBar.PlaceholderColor = IsDarkMode ? Colors.Gray : Colors.DarkGray;
            WordSearchBar.BackgroundColor = IsDarkMode ? Colors.DarkGray : Colors.LightGray;
        }

        // Method to get all Entry controls from the UI
        private IEnumerable<Entry> GetAllEntries()
        {
            // Create a new list to store all Entry controls
            var entries = new List<Entry>();

            // Loop through all 6 rows
            for (int row = 1; row <= 6; row++)
            {
                // Loop through all 5 columns in each row
                for (int col = 1; col <= 5; col++)
                {
                    // Construct the Entry control name based on row and column (e.g., "Row1Letter1")
                    string entryName = $"Row{row}Letter{col}";

                    // Find the Entry control by its name and cast it to Entry type
                    var entry = this.FindByName(entryName) as Entry;

                    // If the Entry was found (not null), add it to our list
                    if (entry != null)
                        entries.Add(entry);
                }
            }
            // Return the complete list of Entry controls
            return entries;
        }

        // Method to get all Border controls from the UI
        private IEnumerable<Border> GetAllBorders()
        {
            // Create a new list to store all Border controls
            var borders = new List<Border>();

            // Loop through all 6 rows
            for (int row = 1; row <= 6; row++)
            {
                // Loop through all 5 columns in each row
                for (int col = 1; col <= 5; col++)
                {
                    // Construct the Border control name based on row and column (e.g., "Border1Letter1")
                    string borderName = $"Border{row}Letter{col}";

                    // Find the Border control by its name and cast it to Border type
                    var border = this.FindByName(borderName) as Border;

                    // If the Border was found (not null), add it to our list
                    if (border != null)
                        borders.Add(border);
                }
            }
            // Return the complete list of Border controls
            return borders;
        }

        // Event handler that triggers when the user clicks the Submit button
        private void OnSubmitGuessClicked(object sender, EventArgs e)
        {
            try
            {
                // Call OnGuessSubmitted directly
                OnGuessSubmitted(sender, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnSubmitGuessClicked: {ex.Message}");
            }
        }

        // Helper method to get Entry text
        private string GetEntryText(string entryName)
        {
            var entry = this.FindByName<Entry>(entryName);
            return entry?.Text ?? "";
        }

        // Event handler that triggers whenever text changes in an Entry control
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is not Entry entry)
                    return;

                string input = e.NewTextValue?.ToUpper() ?? "";

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Special handling for first box
                    if (entry == Row1Letter1)
                    {
                        entry.TextColor = IsDarkMode ? Colors.White : Colors.Black;
                    }
                    else
                    {
                        entry.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground;
                    }
                });

                if (input.Length > 0)
                {
                    if (!char.IsLetter(input[0]))
                    {
                        entry.Text = "";
                        return;
                    }

                    entry.Text = input[0].ToString();

                    if (string.IsNullOrEmpty(entry.AutomationId) || entry.AutomationId.Length < 10)
                        return;

                    if (int.TryParse(entry.AutomationId.Substring(3, 1), out int rowNum) &&
                        int.TryParse(entry.AutomationId.Substring(9, 1), out int letterNum))
                    {
                        if (letterNum < 5)
                        {
                            var nextEntry = this.FindByName<Entry>($"Row{rowNum}Letter{letterNum + 1}");
                            nextEntry?.Focus();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnTextChanged: {ex.Message}");
            }
        }

        // Method to load or download words
        private async Task LoadWordsAsync()
        {
            try
            {
                string localPath = Path.Combine(FileSystem.AppDataDirectory, WordsFileName);
                System.Diagnostics.Debug.WriteLine($"Attempting to load words from: {localPath}");

                // Check if file exists locally
                if (!File.Exists(localPath))
                {
                    System.Diagnostics.Debug.WriteLine("File not found locally, downloading...");
                    // Download file if it doesn't exist
                    using (var client = new HttpClient())
                    {
                        string words = await client.GetStringAsync(WordsUrl);
                        System.Diagnostics.Debug.WriteLine($"Downloaded {words.Length} characters");
                        await File.WriteAllTextAsync(localPath, words);
                        System.Diagnostics.Debug.WriteLine("File downloaded and saved");
                    }
                }

                // Read from local file
                string content = await File.ReadAllTextAsync(localPath);
                System.Diagnostics.Debug.WriteLine($"Read {content.Length} characters from file");

                wordList = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(w => w.Trim().ToUpper())
                                 .Where(w => w.Length == 5)
                                 .ToList();

                System.Diagnostics.Debug.WriteLine($"Processed {wordList.Count} words");

                if (wordList.Count > 0)
                {
                    SelectRandomWord();
                    System.Diagnostics.Debug.WriteLine($"Selected target word: {targetWord}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No words were loaded!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // Add this method to check if a word is valid
        private bool IsValidWord(string word)
        {
            return wordList?.Contains(word.ToUpper()) ?? false;
        }

        private void OnEntryFocused(object sender, FocusEventArgs e)
        {
            if (sender is Entry entry)
            {
                entry.Text = "";
                if (entry == Row1Letter1)
                {
                    entry.TextColor = IsDarkMode ? Colors.White : Colors.Black;
                }
                else
                {
                    entry.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground;
                }
            }
        }

        private void OnEntryCompleted(object sender, EventArgs e)
        {
            if (sender is Entry currentEntry)
            {
                // Move to next entry based on the current entry's name
                switch (currentEntry.AutomationId)
                {
                    case "Row1Letter1":
                        Row1Letter2.Focus();
                        break;
                    case "Row1Letter2":
                        Row1Letter3.Focus();
                        break;
                    case "Row1Letter3":
                        Row1Letter4.Focus();
                        break;
                    case "Row1Letter4":
                        Row1Letter5.Focus();
                        break;
                    case "Row1Letter5":
                        // Optionally trigger submit button here
                        break;
                }
            }
        }

        private void SelectRandomWord()
        {
            if (wordList != null && wordList.Count > 0)
            {
                Random random = new Random();
                int index = random.Next(wordList.Count);
                targetWord = wordList[index].ToUpper();
                System.Diagnostics.Debug.WriteLine($"Selected word: {targetWord}"); // For testing
            }
        }

        private void CheckGuess(string guess)
        {
            guess = guess.ToUpper();
            var targetChars = targetWord.ToCharArray();
            var guessChars = guess.ToCharArray();

            System.Diagnostics.Debug.WriteLine($"Checking guess: {guess} against target: {targetWord}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Get the current row's entries and borders
                for (int i = 0; i < 5; i++)
                {
                    var border = this.FindByName<Border>($"Border{count}Letter{i + 1}");
                    var entry = this.FindByName<Entry>($"Row{count}Letter{i + 1}");

                    if (border != null && entry != null)
                    {
                        if (guessChars[i] == targetChars[i])
                        {
                            // Correct letter in correct position (green)
                            border.BackgroundColor = correctColor;
                            entry.TextColor = Colors.White;
                        }
                        else if (targetWord.Contains(guessChars[i]))
                        {
                            // Letter exists in word but wrong position (yellow)
                            border.BackgroundColor = presentColor;
                            entry.TextColor = Colors.White;
                        }
                        else
                        {
                            // Letter not in word (gray)
                            border.BackgroundColor = incorrectColor;
                            entry.TextColor = Colors.White;
                        }

                        // Disable the entry after setting colors
                        entry.IsEnabled = false;
                    }
                }
            });
        }

        // Helper method to disable all entries after game ends
        private void DisableAllEntries()
        {
            for (int row = 1; row <= 6; row++)
            {
                for (int col = 1; col <= 5; col++)
                {
                    var entry = this.FindByName<Entry>($"Row{row}Letter{col}");
                    if (entry != null)
                    {
                        entry.IsEnabled = false;
                    }
                }
            }
        }

        private void OnNewGameClicked(object sender, EventArgs e)
        {
            // Reset game state
            count = 1;

            // Reset all entries and their colors
            ResetAllEntries();

            // Select a new random word
            SelectRandomWord();

            // Reset the result label
            ResultLabel.Text = "New game started!";
        }

        private async void EndGame(bool isWon)
        {
            try
            {
                won = isWon;

                // Get current player name
                string currentPlayer = await Player.GetPlayerName();
                var save = await SaveGame.Load(currentPlayer);
                save.GamesPlayed++;

                if (isWon)
                {
                    save.GamesWon++;
                    save.CurrentStreak++;
                    save.MaxStreak = Math.Max(save.MaxStreak, save.CurrentStreak);
                }
                else
                {
                    save.CurrentStreak = 0;
                }

                // Create and add the game attempt
                var attempt = new GameAttempt
                {
                    Word = targetWord,
                    GuessCount = count,
                    IsWin = isWon,
                    Date = DateTime.Now,
                    Difficulty = currentDifficulty,
                    Guesses = GetGuessHistory()
                };

                // Ensure History exists
                if (save.History == null)
                {
                    save.History = new PlayerHistory { PlayerName = currentPlayer };
                }

                // Add attempt to history
                save.History.AddAttempt(attempt);

                // Save both the game stats and history
                save.Save(currentPlayer);

                if (isWon)
                {
                    ResultLabel.Text = $"Well done! You found the word in {count} {(count == 1 ? "guess" : "guesses")}!";
                }
                else
                {
                    ResultLabel.Text = $"Bad luck! The word was {targetWord}";
                }

                DisableAllEntries();

                // Check achievements
                var gameTime = DateTime.Now - gameStartTime;
                await AchievementManager.CheckAchievements(isWon, count, gameTime);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EndGame: {ex.Message}");
                await DisplayAlert("Error", "Failed to save game progress", "OK");
            }
        }

        // Helper method to get guess history
        private List<string> GetGuessHistory()
        {
            var guesses = new List<string>();
            for (int row = 1; row <= count; row++)
            {
                string guess = "";
                for (int col = 1; col <= 5; col++)
                {
                    var entry = this.FindByName<Entry>($"Row{row}Letter{col}");
                    if (entry != null)
                    {
                        guess += entry.Text?.ToUpper() ?? "";
                    }
                }
                if (guess.Length == 5)
                {
                    guesses.Add(guess);
                }
            }
            return guesses;
        }

        private async void OnViewProgressClicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("View Progress clicked");
                if (Navigation != null)
                {
                    var savePage = new SaveGamePage();
                    await Navigation.PushAsync(savePage);
                    System.Diagnostics.Debug.WriteLine("Navigation completed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Navigation is null");
                    await DisplayAlert("Error", "Navigation not available", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                await DisplayAlert("Error", $"Navigation failed: {ex.Message}", "OK");
            }
        }

        // Special handlers for the first box
        private void OnFirstBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is not Entry entry)
                    return;

                string input = e.NewTextValue?.ToUpper() ?? "";

                if (input.Length > 0)
                {
                    if (!char.IsLetter(input[0]))
                    {
                        entry.Text = "";
                        return;
                    }

                    entry.Text = input[0].ToString();

                    // Force color update
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        entry.TextColor = IsDarkMode ? Colors.White : Colors.Black;
                    });

                    // Move to next box
                    Row1Letter2?.Focus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in first box text changed: {ex.Message}");
            }
        }

        private void OnFirstBoxFocused(object sender, FocusEventArgs e)
        {
            // Cast sender to Entry explicitly
            var entry = (Entry)sender;
            entry.Text = string.Empty;
            entry.TextColor = IsDarkMode ? Colors.White : Colors.Black;
            System.Diagnostics.Debug.WriteLine($"First box focused - color set to: {entry.TextColor}");
        }

        private async Task ShowRecentPlayers()
        {
            var players = await Player.GetExistingPlayers();
            var recentPlayers = players.Take(5).ToList(); // Show last 5 players

            string result = await DisplayActionSheet(
                "Recent Players",
                "Cancel",
                null,
                recentPlayers.ToArray());

            if (!string.IsNullOrEmpty(result) && result != "Cancel")
            {
                await Player.SavePlayerName(result);
                await DisplayAlert("Success", $"Switched to {result}", "OK");
            }
        }

        private void OnGuessSubmitted(object sender, EventArgs e)
        {
            try
            {
                string guess = "";
                bool isValid = true;

                // Debug output
                System.Diagnostics.Debug.WriteLine($"Current row: {count}");

                // First check if all entries have values
                for (int i = 1; i <= 5; i++)
                {
                    var entry = this.FindByName<Entry>($"Row{count}Letter{i}");
                    var text = entry?.Text?.Trim() ?? "";

                    System.Diagnostics.Debug.WriteLine($"Entry {i}: '{text}'");

                    if (string.IsNullOrEmpty(text))
                    {
                        isValid = false;
                        break;
                    }
                    guess += text.ToUpper();
                }

                if (!isValid)
                {
                    DisplayAlert("Invalid", "Please enter all letters", "OK");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Complete guess: '{guess}'");

                // Process valid guess using the class-level color variables
                for (int i = 0; i < 5; i++)
                {
                    var entry = this.FindByName<Entry>($"Row{count}Letter{i + 1}");
                    var border = this.FindByName<Border>($"Border{count}Letter{i + 1}");
                    var currentLetter = guess[i];

                    if (currentLetter == targetWord[i])
                    {
                        border.BackgroundColor = correctColor;  // Using class-level variable
                        entry.TextColor = Colors.White;
                    }
                    else if (targetWord.Contains(currentLetter))
                    {
                        border.BackgroundColor = presentColor;  // Using class-level variable
                        entry.TextColor = Colors.White;
                    }
                    else
                    {
                        border.BackgroundColor = incorrectColor;  // Using class-level variable
                        entry.TextColor = Colors.White;
                    }

                    entry.IsEnabled = false;
                }

                if (guess == targetWord)
                {
                    EndGame(true);
                    return;
                }

                if (count >= MaxGuesses)  // Use MaxGuesses instead of hardcoded 6
                {
                    EndGame(false);
                    return;
                }

                count++;
                var nextEntry = this.FindByName<Entry>($"Row{count}Letter1");
                if (nextEntry != null)
                {
                    nextEntry.Focus();
                }

                // Reset consecutive hint flag when a guess is submitted
                usedHintLastTurn = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnGuessSubmitted: {ex.Message}");
            }
        }

        private void FocusNextRow()
        {
            try
            {
                // Focus the first entry of the next row
                var nextEntry = this.FindByName<Entry>($"Row{count}Letter1");
                nextEntry?.Focus();
                System.Diagnostics.Debug.WriteLine($"Focusing next row: Row{count}Letter1");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error focusing next row: {ex.Message}");
            }
        }

        private async Task InitializeGame()
        {
            try
            {
                await LoadWordsAsync();
                SelectRandomWord();
                count = 1;

                // Reset all entries
                ResetAllEntries();

                System.Diagnostics.Debug.WriteLine($"Game initialized with word: {targetWord}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing game: {ex.Message}");
            }
        }

        private void ResetAllEntries()
        {
            // Reset all entries and their colors
            for (int row = 1; row <= 6; row++)
            {
                for (int col = 1; col <= 5; col++)
                {
                    var entry = this.FindByName<Entry>($"Row{row}Letter{col}");
                    var border = this.FindByName<Border>($"Border{row}Letter{col}");

                    if (entry != null)
                    {
                        entry.Text = "";
                        entry.IsEnabled = true;

                        // Special handling for first box
                        if (entry == Row1Letter1)
                        {
                            entry.TextColor = IsDarkMode ? Colors.White : Colors.Black;
                        }
                        else
                        {
                            entry.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground;
                        }
                    }

                    if (border != null)
                    {
                        border.BackgroundColor = Colors.Transparent;
                        border.Stroke = Colors.Gray;
                    }
                }
            }

            // Focus the first entry
            Row1Letter1?.Focus();

            // Reset hints if in Easy mode
            if (currentDifficulty == GameDifficulty.Easy)
            {
                ResetHints();
            }
        }

        // Add property for maximum guesses
        private int MaxGuesses { get; set; } = 6;

        private async void OnHintButtonClicked(object sender, EventArgs e)
        {
            if (currentDifficulty != GameDifficulty.Easy)
            {
                await DisplayAlert("Hints Disabled", "Hints are only available in Easy Mode", "OK");
                return;
            }

            if (hintsRemaining <= 0)
            {
                await DisplayAlert("No Hints", "You've used all your hints for this game!", "OK");
                return;
            }

            if (usedHintLastTurn)
            {
                await DisplayAlert("Hint Blocked", "You can't use hints on consecutive guesses", "OK");
                return;
            }

            try
            {
                // Find a random position that hasn't been revealed yet
                var availablePositions = new List<int>();
                for (int i = 0; i < 5; i++)  // Now check all positions
                {
                    var entry = this.FindByName<Entry>($"Row{count}Letter{i + 1}");
                    if (entry?.Text != targetWord[i].ToString())
                    {
                        availablePositions.Add(i);
                    }
                }

                if (availablePositions.Count > 0)
                {
                    Random random = new Random();
                    int position = availablePositions[random.Next(availablePositions.Count)];

                    var entry = this.FindByName<Entry>($"Row{count}Letter{position + 1}");
                    var border = this.FindByName<Border>($"Border{count}Letter{position + 1}");

                    if (entry != null && border != null)
                    {
                        entry.Text = targetWord[position].ToString();
                        entry.IsEnabled = false;
                        entry.TextColor = Colors.White;
                        border.BackgroundColor = correctColor;

                        hintsRemaining--;
                        usedHintLastTurn = true;

                        HintButton.Text = $"Get Hint ({hintsRemaining} left)";
                        await DisplayAlert("Hint Used", $"Letter {position + 1} is '{targetWord[position]}'\nHints remaining: {hintsRemaining}", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("No More Hints", "All letters have been revealed!", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error using hint: {ex.Message}");
                await DisplayAlert("Error", "Failed to show hint", "OK");
            }
        }

        private void UpdateUIForDifficulty()
        {
            switch (currentDifficulty)
            {
                case GameDifficulty.Easy:
                    MaxGuesses = 6;
                    hintsRemaining = 2;
                    HintButton.IsVisible = true;
                    ResultLabel.Text = $"Easy Mode: {hintsRemaining} hints remaining";
                    break;
                case GameDifficulty.Medium:
                    MaxGuesses = 6;
                    hintsRemaining = 0;
                    HintButton.IsVisible = false;
                    ResultLabel.Text = "Medium Mode: Standard rules";
                    break;
                case GameDifficulty.Hard:
                    MaxGuesses = 4;
                    hintsRemaining = 0;
                    HintButton.IsVisible = false;
                    ResultLabel.Text = "Hard Mode: Must use correct letters";
                    break;
            }
        }

        private void ResetHints()
        {
            hintsRemaining = 2;
            usedHintLastTurn = false;
            if (HintButton != null)
            {
                HintButton.Text = $"Get Hint ({hintsRemaining} left)";
            }
        }

        private async void OnExitButtonClicked(object sender, EventArgs e)
        {
            bool shouldExit = await DisplayAlert(
                "Exit Game",
                "Are you sure you want to exit the game?",
                "Yes",
                "No");

            if (shouldExit)
            {
                Application.Current?.Quit();
            }
        }

        private async void OnShareResultsClicked(object sender, EventArgs e)
        {
            try
            {
                var shareText = GenerateShareText();
                await Share.RequestAsync(new ShareTextRequest
                {
                    Text = shareText,
                    Title = "My Wordle Results"
                });
            }
            catch (Exception)  // Removed 'ex' since it wasn't being used
            {
                await DisplayAlert("Error", "Failed to share results", "OK");
                System.Diagnostics.Debug.WriteLine("Error sharing results");
            }
        }

        private string GenerateShareText()
        {
            var result = new StringBuilder();
            result.AppendLine($"Wordle {(won ? count : "X")}/6");

            // Get current row patterns
            for (int row = 1; row <= count; row++)
            {
                for (int col = 1; col <= 5; col++)
                {
                    var border = this.FindByName<Border>($"Border{row}Letter{col}");
                    if (border.BackgroundColor == correctColor)
                        result.Append("🟩"); // Green
                    else if (border.BackgroundColor == presentColor)
                        result.Append("🟨"); // Yellow
                    else
                        result.Append("⬛"); // Black/Gray
                }
                result.AppendLine();
            }

            return result.ToString();
        }

        private async Task StartDailyChallenge()
        {
            try
            {
                // Check if already played today
                string todayKey = $"{DAILY_CHALLENGE_KEY}{DateTime.Today:yyyyMMdd}";
                bool alreadyPlayed = Preferences.Default.Get(todayKey, false);

                if (alreadyPlayed)
                {
                    await DisplayAlert("Daily Challenge",
                        "You've already completed today's challenge!\nCome back tomorrow for a new word.",
                        "OK");
                    return;
                }

                // Reset game state for new challenge
                isDailyChallenge = true;
                await ResetGameState();

                // Set daily word using date as seed
                targetWord = GetDailyWord();

                // Mark as played
                Preferences.Default.Set(todayKey, true);

                await DisplayAlert("Daily Challenge",
                    "Starting today's challenge!\nYou only get one attempt per day - make it count!",
                    "OK");

                // Update UI to show it's a daily challenge
                ResultLabel.Text = "🌟 Daily Challenge Mode 🌟";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in StartDailyChallenge: {ex.Message}");
                await DisplayAlert("Error", "Failed to start daily challenge", "OK");
            }
        }

        private string GetDailyWord()
        {
            // Use today's date as seed for consistent random number
            var today = DateTime.Today;
            var seed = today.Year * 10000 + today.Month * 100 + today.Day;
            var random = new Random(seed);

            // This ensures everyone gets the same word on the same day
            return wordList[random.Next(wordList.Count)].ToUpper();
        }

        private string GetRandomWord()
        {
            Random random = new Random();
            return wordList[random.Next(wordList.Count)];

        }

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(e.NewTextValue))
                {
                    // Hide results if search is empty
                    ResultLabel.Text = "Results will appear here";
                    return;
                }

                // Search for matching words
                var searchText = e.NewTextValue.ToUpper();
                var matchingWords = wordList
                    .Where(word => word.Contains(searchText))
                    .Take(5)  // Limit to first 5 matches
                    .ToList();

                if (matchingWords.Any())
                {
                    ResultLabel.Text = $"Matching words:\n{string.Join(", ", matchingWords)}";
                }
                else
                {
                    ResultLabel.Text = "No matching words found";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                ResultLabel.Text = "Error searching words";
            }
        }
    }

}