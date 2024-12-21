using System.Net.Http;
using System.IO;
using Microsoft.Maui.Controls;

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

        // Add this static property
        public static bool IsDarkMode
        {
            get => Preferences.Default.Get("IsDarkMode", false);
            set => Preferences.Default.Set("IsDarkMode", value);
        }

        public MainPage()
        {
            InitializeComponent();
            _ = InitializeGame();
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
                        border.Stroke = IsDarkMode ? darkModeForeground : Colors.Gray;
                    }
                }
            }

            // Focus the first entry
            Row1Letter1?.Focus();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await CheckAndLoginPlayer();
                ApplyTheme();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex.Message}");
            }
        }

        private async Task CheckAndLoginPlayer()
        {
            try
            {
                string currentPlayer = await Player.GetPlayerName();

                if (string.IsNullOrEmpty(currentPlayer))
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
                    System.Diagnostics.Debug.WriteLine($"New player created: {name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Existing player loaded: {currentPlayer}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CheckAndLoginPlayer: {ex.Message}");
            }
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
                var save = await SaveGame.Load();
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
            BackgroundColor = IsDarkMode ? darkModeBackground : lightModeBackground;

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

                // Special handling for first box
                if (entry == Row1Letter1)
                {
                    entry.TextColor = IsDarkMode ? Colors.White : Colors.Black;
                    System.Diagnostics.Debug.WriteLine($"First box color set to: {entry.TextColor}");
                }
                else
                {
                    entry.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground;
                }

                if (input.Length > 0)
                {
                    if (!char.IsLetter(input[0]))
                    {
                        entry.Text = "";
                        return;
                    }

                    entry.Text = input[0].ToString();

                    if (string.IsNullOrEmpty(entry.AutomationId) || entry.AutomationId.Length < 10)
                    {
                        return;
                    }

                    if (int.TryParse(entry.AutomationId.Substring(3, 1), out int rowNum) &&
                        int.TryParse(entry.AutomationId.Substring(9, 1), out int letterNum))
                    {
                        if (letterNum < 5)
                        {
                            var nextEntry = this.FindByName<Entry>($"Row{rowNum}Letter{letterNum + 1}");
                            if (nextEntry != null)
                            {
                                nextEntry.Focus();
                            }
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

            // Add debug output
            System.Diagnostics.Debug.WriteLine($"Checking guess: {guess} against target: {targetWord}");

            // Get the current row's borders based on attempt count
            for (int i = 0; i < 5; i++)
            {
                var border = this.FindByName<Border>($"Border{count}Letter{i + 1}");
                var entry = this.FindByName<Entry>($"Row{count}Letter{i + 1}");

                if (guessChars[i] == targetChars[i])
                {
                    // Correct letter in correct position (green)
                    border.BackgroundColor = correctColor;
                    System.Diagnostics.Debug.WriteLine($"Letter {guessChars[i]} at position {i} is correct");
                }
                else if (targetWord.Contains(guessChars[i]))
                {
                    // Letter exists in word but wrong position (yellow)
                    border.BackgroundColor = presentColor;
                    System.Diagnostics.Debug.WriteLine($"Letter {guessChars[i]} at position {i} is present");
                }
                else
                {
                    // Letter not in word (gray)
                    border.BackgroundColor = incorrectColor;
                    System.Diagnostics.Debug.WriteLine($"Letter {guessChars[i]} at position {i} is incorrect");
                }

                // Make text white for better visibility on colored backgrounds
                entry.TextColor = Colors.White;
            }

            // Check if the guess is correct or if game is over
            if (guess == targetWord)
            {
                // Win condition
                EndGame(true);
            }
            else if (count >= 6)
            {
                // Loss condition
                EndGame(false);
            }
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
            count = 0;

            // Reset all entries and their colors
            ResetAllEntries();

            // Select a new random word
            SelectRandomWord();

            // Reset the result label
            ResultLabel.Text = "New game started!";
        }

        private async void EndGame(bool won)
        {
            try
            {
                var save = await SaveGame.Load();
                save.GamesPlayed++;

                if (won)
                {
                    save.GamesWon++;
                    save.CurrentStreak++;
                    save.MaxStreak = Math.Max(save.MaxStreak, save.CurrentStreak);
                }
                else
                {
                    save.CurrentStreak = 0;
                }

                save.Save();

                // Add game attempt to history
                save.History.AddAttempt(new GameAttempt(targetWord, count, GetGuessHistory()));

                if (won)
                {
                    ResultLabel.Text = $"Well done! You found the word in {count} {(count == 1 ? "guess" : "guesses")}!";
                }
                else
                {
                    ResultLabel.Text = $"Bad luck! The word was {targetWord}";
                }
                DisableAllEntries();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EndGame: {ex.Message}");
            }
        }

        private List<string> GetGuessHistory()
        {
            var history = new List<string>();
            for (int row = 1; row <= count; row++)
            {
                string guess = GetEntryText($"Row{row}Letter1") +
                              GetEntryText($"Row{row}Letter2") +
                              GetEntryText($"Row{row}Letter3") +
                              GetEntryText($"Row{row}Letter4") +
                              GetEntryText($"Row{row}Letter5");
                history.Add(guess.ToUpper());
            }
            return history;
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
            System.Diagnostics.Debug.WriteLine($"New player created: {name}");
        }

        private void OnGuessSubmitted(object sender, EventArgs e)
        {
            try
            {
                // Get the current row's entries
                var currentRowEntries = new List<Entry>();
                var currentRowBorders = new List<Border>();

                for (int i = 1; i <= 5; i++)
                {
                    var entry = this.FindByName<Entry>($"Row{count}Letter{i}");
                    var border = this.FindByName<Border>($"Border{count}Letter{i}");
                    if (entry != null && border != null)
                    {
                        currentRowEntries.Add(entry);
                        currentRowBorders.Add(border);
                    }
                }

                // Validate input
                if (currentRowEntries.Any(e => string.IsNullOrEmpty(e.Text)))
                {
                    DisplayAlert("Invalid", "Please enter all letters", "OK");
                    return;
                }

                string guess = string.Join("", currentRowEntries.Select(e => e.Text)).ToUpper();

                // Same color logic for all rows including first row
                for (int i = 0; i < 5; i++)
                {
                    var entry = currentRowEntries[i];
                    var border = currentRowBorders[i];

                    if (guess[i] == targetWord[i])
                    {
                        border.BackgroundColor = Colors.Green;
                        entry.TextColor = Colors.White;
                    }
                    else if (targetWord.Contains(guess[i]))
                    {
                        border.BackgroundColor = Colors.Orange;
                        entry.TextColor = Colors.White;
                    }
                    else
                    {
                        border.BackgroundColor = Colors.Gray;
                        entry.TextColor = Colors.White;
                    }

                    entry.IsEnabled = false;
                }

                if (guess == targetWord)
                {
                    EndGame(true);
                    return;
                }

                if (count == 6)
                {
                    EndGame(false);
                    return;
                }

                count++;
                FocusNextRow();
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
    }

}




