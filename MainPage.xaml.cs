using System.Net.Http;
using System.IO;
using Microsoft.Maui.Controls;
using System.Text.Json;
using System.Text;
using System.Diagnostics.Metrics;
using Microsoft.Maui.Devices;

namespace Wordle
{
    public partial class MainPage : ContentPage
    {
        // Variables
        int count = 0; // Counter for the current row
        private const string ThemePreferenceKey = "IsDarkMode"; // Key for theme preference
        private List<string>? wordList; // List of valid words
        private const string WordsFileName = "words.txt"; // Local file name
        private const string WordsUrl = "https://raw.githubusercontent.com/DonH-ITS/jsonfiles/main/words.txt"; // Remote file URL
        private readonly Color darkModeBackground = Colors.Black; // Dark mode background color
        private readonly Color darkModeForeground = Colors.White; // Dark mode foreground color
        private readonly Color lightModeBackground = Colors.White;  // Light mode background color
        private readonly Color lightModeForeground = Colors.Black; // Light mode foreground color
        private string targetWord = string.Empty; // The target word to guess
        private readonly Color correctColor = Colors.Green;       // Letter is correct and in right position
        private readonly Color presentColor = Colors.Yellow;      // Letter exists but in wrong position
        private readonly Color incorrectColor = Colors.Gray;      // Letter is not in the word
        private bool won = false; // Flag to track if the game was won
        private DateTime gameStartTime; // Track game start time
        private bool isDailyChallenge = false; // Flag to track daily challenge mode
        private const string DAILY_CHALLENGE_KEY = "DailyChallenge_"; // Key for daily challenge

        // Theme preference property
        public static bool IsDarkMode
        {
            get => Preferences.Default.Get("IsDarkMode", false);
            set => Preferences.Default.Set("IsDarkMode", value);
        }

        private GameDifficulty currentDifficulty = GameDifficulty.Medium; // Default difficulty

        private int hintsRemaining = 2; // Available hints
        private bool usedHintLastTurn = false; // Prevent consecutive hint usage

        
        public MainPage()
        {
            InitializeComponent(); // Initialize the UI
            ShowStartPrompt(); // Display the prompt to start the game or action
        }

        // Method to initialize the game
        private async void ShowStartPrompt()
        {
            // Check if the player has played before
            try
            {
                string choice; // Variable to store the user's choice
                // Do while loop to display the action sheet until the user selects an option
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

                    // If the user does not select an option, display an alert
                    if (string.IsNullOrEmpty(choice)) 
                    {
                        await DisplayAlert("Required", "Please select an option to continue", "OK");
                    }

                } while (string.IsNullOrEmpty(choice)); //While Loops until the user selects an option

                // Select difficulty before proceeding (except for Daily Challenge)
                if (choice != "Daily Challenge")
                {
                    string difficultyChoice = await DisplayActionSheet( // Select Difficulty 
                        "Select Difficulty",
                        null,  // No cancel option
                        null,  // No destruction option
                        "Easy Mode (6 guesses, with hints)",
                        "Medium Mode (Standard 6 guesses)",
                        "Hard Mode (4 guesses, must reuse correct letters)");

                    switch (difficultyChoice) // Switch statement to set the difficulty level
                    {
                        case "Easy Mode (6 guesses, with hints)":
                            currentDifficulty = GameDifficulty.Easy; // Set the difficulty to Easy
                            count = 1; // Set the count to 1
                            MaxGuesses = 6; // Set the maximum number of guesses to 6
                            break;
                        case "Medium Mode (Standard 6 guesses)":
                            currentDifficulty = GameDifficulty.Medium; // Set the difficulty to Medium
                            count = 1; // Set the count to 1
                            MaxGuesses = 6; // Set the maximum number of guesses to 6
                            break;
                        case "Hard Mode (4 guesses, must reuse correct letters)":
                            currentDifficulty = GameDifficulty.Hard; // Set the difficulty to Hard
                            count = 1; // Set the count to 1
                            MaxGuesses = 4; // Set the maximum number of guesses to 4
                            break;
                    }                  
                }

                // Load the words
                await LoadWordsAsync();

                // Swtich statement Handle different game modes
                switch (choice)
                {
                    case "Daily Challenge":
                        await StartDailyChallenge(); // Start the daily challenge
                        break;

                    case "Load Saved Game":
                        var players = await Player.GetExistingPlayers(); // Get existing players

                        // If there are no saved games, display an alert
                        if (players.Count == 0)
                        {
                            await DisplayAlert("No Saved Games", "No saved games found. Starting new game.", "OK");
                            await CreateNewPlayer(); // Create a new player
                        }
                        
                        else
                        {
                            string selectedPlayer = await DisplayActionSheet( // Display options to select a player
                                "Select Saved Game",
                                "Cancel",
                                null,
                                players.ToArray()); // Display the list of players

                            // If the user selects a player, load the saved game
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
                                // If the user cancels, create a new player
                                await CreateNewPlayer();
                            }
                        }
                        break;

                    case "Start New Game":
                        string newName = await DisplayPromptAsync( // Prompt the user to enter a name
                            "New Game",
                            "Enter player name:",
                            maxLength: 20,
                            keyboard: Keyboard.Text);

                        // If the user enters a name, save the player name
                        if (!string.IsNullOrEmpty(newName)) 
                        {
                            await Player.SavePlayerName(newName); // Save the player name
                            var newSave = new SaveGame // Create a new save game
                            {
                                GamesPlayed = 0, // Set the number of games played to 0
                                GamesWon = 0, // Set the number of games won to 0
                                CurrentStreak = 0, // Set the current streak to 0
                                MaxStreak = 0, // Set the maximum streak to 0
                                GuessDistribution = new Dictionary<int, int>(), // Create a new dictionary for the guess distribution
                                History = new PlayerHistory { PlayerName = newName } // Create new player history
                            };
                            newSave.Save(newName); // Save the new game

                            var newHistory = new PlayerHistory { PlayerName = newName }; // Create new player history
                            string historyPath = Path.Combine(FileSystem.AppDataDirectory, $"{newName}_history.json"); // Set the path for the history file
                            await File.WriteAllTextAsync(historyPath, JsonSerializer.Serialize(newHistory)); // Write the history file
                        }
                        break;

                    case "Continue Previous Game":
                        var currentPlayer = await Player.GetPlayerName(); // Get the current player 

                        // If there is no current player, display alert
                        if (string.IsNullOrEmpty(currentPlayer))
                        {
                            await DisplayAlert("No Previous Game", "No previous game found. Starting new game.", "OK");
                            await CreateNewPlayer(); // Create a new player
                        }
                        else
                        {
                            var currentSave = await SaveGame.Load(currentPlayer); // Load the current save game
                            await DisplayAlert("Game Continued",
                                $"Welcome back, {currentPlayer}!\n" +
                                $"Games Won: {currentSave.GamesWon}\n" +
                                $"Current Streak: {currentSave.CurrentStreak}",
                                "OK");
                        }
                        break;
                }

                // Initialize the game
                MainThread.BeginInvokeOnMainThread(() => // Begin invoke on main thread
                {
                    ResetAllEntries(); // Reset all entries
                    ApplyTheme(); // Apply the theme
                    Row1Letter1?.Focus(); // Focus on the first letter
                    if (choice != "Daily Challenge") // If the choice is not daily challenge
                    {
                        ResultLabel.Text = $"Playing on {currentDifficulty} Mode"; // Display the difficulty mode
                    }
                });

                UpdateUIForDifficulty(); // Updates UI based on selected difficulty

            }

            catch (Exception ex) 
            {
                System.Diagnostics.Debug.WriteLine($"Error in ShowStartPrompt: {ex.Message}");
                await DisplayAlert("Error", "Failed to start game", "OK");
            }
        }

        private async Task ResetGameState() // Reset game
        {
            await MainThread.InvokeOnMainThreadAsync(() => 
            {
                count = 1; // Set the count to 1

                // Reset all entries and borders
                for (int row = 1; row <= 6; row++)
                {
                    for (int col = 1; col <= 5; col++)
                    {
                        var entry = this.FindByName<Entry>($"Row{row}Letter{col}"); // Find the entry
                        var border = this.FindByName<Border>($"Border{row}Letter{col}"); // Find the border

                        if (entry != null) 
                        {
                            entry.Text = "";
                            entry.IsEnabled = true;

                            // Set text colors
                            if (row == 1 && col == 1) // If the row is 1 and the column is 1
                            {
                                entry.TextColor = IsDarkMode ? Colors.White : Colors.Black; // Set the text color to white if dark mode is enabled, otherwise black.
                            }

                            else // If the row is not 1 and the column is not 1
                            {
                                entry.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground; // Set the text color based on the current mode dark mode or light mode.
                            }
                        }

                        if (border != null) // If the border is not null
                        {
                            border.BackgroundColor = Colors.Transparent; // Set the background color to transparent
                            border.Stroke = Colors.Gray; // Set the stroke color to gray
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

        private void DisableGameControls() // Disable game controls method sets the game controls to false
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
            SubmitButton.IsEnabled = false; // Submit button is disabled until all entries are filled
        }

        private void EnableGameControls() // Enable game controls method sets the game controls to true
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
            SubmitButton.IsEnabled = true; // Submit button is enabled when all entries are filled
        }

        protected override void OnAppearing() // On appearing method
        {
            base.OnAppearing(); // Base on appearing method is called when the page appears 
            ApplyTheme(); // Apply the theme when the page appears
        }

        private async Task CheckAndLoginPlayer() // Check and login player method
        {
            try 
            {
                var existingPlayers = await Player.GetExistingPlayers(); // Get existing players from the player class
                string currentPlayer = await Player.GetPlayerName(); // Get the current player name from the player class

                if (existingPlayers.Any()) // If there are existing players
                {
                    // Display action sheet to choose player options
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
                            GamesPlayed = 0, // Set the number of games played to 0
                            GamesWon = 0, // Set the number of games won to 0
                            CurrentStreak = 0, // Set the current streak to 0
                            MaxStreak = 0, // Set the maximum streak to 0
                            GuessDistribution = new Dictionary<int, int>(), // Create a new dictionary for the guess distribution
                            History = new PlayerHistory { PlayerName = newName } // Create new player history
                        };
                        newSave.Save(newName); // Save the new game

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

        private async Task CreateNewPlayer() // Create new player method
        {
            string name = await DisplayPromptAsync( // Display prompt to enter player name
                "Welcome to Wordle!",
                "Please enter your name:",
                maxLength: 20,
                keyboard: Keyboard.Text);
            if (string.IsNullOrEmpty(name)) // If the name is empty
            {
                name = "Player"; // Set the name to Player
            }
            await Player.SavePlayerName(name); // Save the player name
            // Create fresh save game data
            var newSave = new SaveGame // Create a new save game
            {
                GamesPlayed = 0, // Set the number of games played to 0
                GamesWon = 0, // Set the number of games won to 0
                CurrentStreak = 0, // Set the current streak to 0
                MaxStreak = 0, // Set the maximum streak to 0
                GuessDistribution = new Dictionary<int, int>(), // Create a new dictionary for the guess distribution
                History = new PlayerHistory { PlayerName = name } // Create new player history
            };
            newSave.Save(name); // Save the new game
            // Create fresh history
            var newHistory = new PlayerHistory { PlayerName = name }; // Create new player history
            string historyPath = Path.Combine(FileSystem.AppDataDirectory, $"{name}_history.json"); // Set the path for the history file
            await File.WriteAllTextAsync(historyPath, JsonSerializer.Serialize(newHistory)); // Write the history file
            System.Diagnostics.Debug.WriteLine($"New player created: {name}"); // Display the new player created
        }

        private async Task SwitchPlayer() // Switch player method 
        {
            var existingPlayers = await Player.GetExistingPlayers(); // Gets existing players
            existingPlayers.Add("New Player"); // Adds new player

            string result = await DisplayActionSheet( // Display options to choose player
                "Choose Player",
                "Cancel",
                null,
                existingPlayers.ToArray()); // Displays the list of existing players and new player 

            if (result == "New Player")
            {
                await CreateNewPlayer(); // Creates a new player
            }
            else if (!string.IsNullOrEmpty(result) && result != "Cancel") // If the result is not empty and not cancel
            {
                await Player.SavePlayerName(result); // Saves the player name
                var save = await SaveGame.Load(result); // Loads the saved game
                await DisplayAlert("Player Switched",
                    $"Welcome back, {result}!\n" +
                    $"Games Won: {save.GamesWon}",
                    "OK");
            }
        }

        // Method to initialize the game 
        private async void OnSwitchPlayerClicked(object sender, EventArgs e)
        {
            await SwitchPlayer(); // Switch player
        }

        // Method to initialize the game after player selection 
        private void OnThemeToggleClicked(object sender, EventArgs e)
        {
            IsDarkMode = !IsDarkMode; // Toggle the theme mode property
            ApplyTheme(); // Apply the theme perferences 
        }

        private void ApplyTheme()
        {
            // Set page background
            BackgroundColor = IsDarkMode ? darkModeBackground : lightModeBackground;

            // Update title color with theme
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
                        entry.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground; // Set the text color based on the current mode dark mode or light mode.
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
            var entry = this.FindByName<Entry>(entryName); // Find the entry by name
            return entry?.Text ?? ""; // Return the entry text or empty string
        } 

        // Event handler that triggers whenever text changes in an Entry control
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is not Entry entry)
                    return;

                string input = e.NewTextValue?.ToUpper() ?? ""; // Get the new text value and convert to uppercase

                MainThread.BeginInvokeOnMainThread(() => // Begin invoke on main thread method and set the text color based on the mode
                {
                    // Special handling for first box
                    if (entry == Row1Letter1)
                    {
                        entry.TextColor = IsDarkMode ? Colors.White : Colors.Black; // Set the text color to white if dark mode is enabled, otherwise black.
                    }
                    else
                    {
                        entry.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground; // Set the text color based on the current mode dark mode or light mode.
                    }
                });

                if (input.Length > 0) // Input has to be greater than 0 
                {
                    if (!char.IsLetter(input[0])) // If the input is not a letter
                    {
                        entry.Text = ""; // Set the text to empty if not a letter
                        return;
                    }

                    entry.Text = input[0].ToString(); // Set the text to the first character of user input

                    // Move to next entry if current entry is filled
                    if (string.IsNullOrEmpty(entry.AutomationId) || entry.AutomationId.Length < 10) // If the entry is empty or the length is less than 10
                        return;

                    if (int.TryParse(entry.AutomationId.Substring(3, 1), out int rowNum) && // It tries to convert these characters into numbers starts counting from 0 to 9 
                        int.TryParse(entry.AutomationId.Substring(9, 1), out int letterNum)) // If both values are successfully parsed, proceed with the following logic.
                    {
                        if (letterNum < 5) // This checks if the letter number is less than 5
                        {
                            var nextEntry = this.FindByName<Entry>($"Row{rowNum}Letter{letterNum + 1}"); // Find the next entry
                            nextEntry?.Focus(); // Focus on the next entry
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnTextChanged: {ex.Message}");
            }
        }

        // Event handler that triggers when the user submits a guess
        private async Task LoadWordsAsync()
        {
            try
            {
                string localPath = Path.Combine(FileSystem.AppDataDirectory, WordsFileName); // Local path for the words file
                System.Diagnostics.Debug.WriteLine($"Attempting to load words from: {localPath}");

                // Check if file exists locally
                if (!File.Exists(localPath))
                {
                    System.Diagnostics.Debug.WriteLine("File not found locally, downloading...");
                    // Download file if it doesn't exist
                    using (var client = new HttpClient()) // Using HttpClient to download the file
                    {
                        string words = await client.GetStringAsync(WordsUrl); // Get the words from the URL
                        System.Diagnostics.Debug.WriteLine($"Downloaded {words.Length} characters"); // Display the number of characters downloaded
                        await File.WriteAllTextAsync(localPath, words); // Write the words to the local path
                        System.Diagnostics.Debug.WriteLine("File downloaded and saved"); // Display the message that the file was downloaded and saved
                    }
                }

                // Read the words from the local file
                string content = await File.ReadAllTextAsync(localPath); 
                System.Diagnostics.Debug.WriteLine($"Read {content.Length} characters from file"); // Display the number of characters read from the file

                wordList = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries) 
                                 .Select(w => w.Trim().ToUpper())
                                 .Where(w => w.Length == 5)
                                 .ToList();

                System.Diagnostics.Debug.WriteLine($"Processed {wordList.Count} words");

                if (wordList.Count > 0) // If the word list count has to be greater than 0
                {
                    SelectRandomWord(); // Select a random word from the list
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

        // This method to checks if a word is valid
        private bool IsValidWord(string word)
        {
            return wordList?.Contains(word.ToUpper()) ?? false; // Returns true if the word is in the list, otherwise false
        }

        private void OnEntryFocused(object sender, FocusEventArgs e) // On entry focused method
        {
            if (sender is Entry entry) // If the sender is an entry
            {
                entry.Text = ""; // Set the text to empty
                if (entry == Row1Letter1) // If the entry is the first letter in the first row
                {
                    entry.TextColor = IsDarkMode ? Colors.White : Colors.Black; // Set the text color to white if dark mode is enabled, otherwise black.
                }
                else // If the entry is not the first letter in the first row
                {
                    entry.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground; // Set the text color based on the current mode dark mode or light mode.
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

        // Select random word method to set the target word
        private void SelectRandomWord() 
        {
            if (wordList != null && wordList.Count > 0) // If the word list is not null and the count is greater than 0
            {
                Random random = new Random(); // Create a new random object
                int index = random.Next(wordList.Count); // Get a random index from the word list
                targetWord = wordList[index].ToUpper(); // Set the target word to the word at the random index
                System.Diagnostics.Debug.WriteLine($"Selected word: {targetWord}"); // Display the selected word
            }
        }

        // Method to check the guess
        private void CheckGuess(string guess)
        {
            guess = guess.ToUpper(); // Convert the guess to uppercase
            var targetChars = targetWord.ToCharArray(); // Convert the target word to a character array
            var guessChars = guess.ToCharArray(); // Convert the guess to a character array 

            System.Diagnostics.Debug.WriteLine($"Checking guess: {guess} against target: {targetWord}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Get the current row's entries and borders
                // for loop to iterate through the rows and columns
                for (int i = 0; i < 5; i++)
                {
                    var border = this.FindByName<Border>($"Border{count}Letter{i + 1}"); // Find the border by name and count
                    var entry = this.FindByName<Entry>($"Row{count}Letter{i + 1}"); // Find the entry by name and count

                    if (border != null && entry != null) // If the border and entry are not null and the entry text is not empty
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
            for (int row = 1; row <= 6; row++) // For loop to iterate through the rows and columns to disable the entries
            {
                for (int col = 1; col <= 5; col++) // For loop to iterate through the columns
                {
                    var entry = this.FindByName<Entry>($"Row{row}Letter{col}");
                    if (entry != null) // If the entry is not null
                    {
                        entry.IsEnabled = false; // Set the entry to false to disable it
                    }
                }
            }
        }

        // Helper method to reset all entries and their colors
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
                won = isWon; // Set the won flag to the isWon value

                // Get current player name
                string currentPlayer = await Player.GetPlayerName();
                var save = await SaveGame.Load(currentPlayer);
                save.GamesPlayed++;

                // Update streaks if game is won
                if (isWon)
                {
                    save.GamesWon++;
                    save.CurrentStreak++;
                    save.MaxStreak = Math.Max(save.MaxStreak, save.CurrentStreak);
                }

                // Update streaks if game is lost
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

                // Display result message based on win/loss
                if (isWon)
                {
                    ResultLabel.Text = $"Well done! You found the word in {count} {(count == 1 ? "guess" : "guesses")}!";
                }

                // If the game is lost Display the message
                else
                {
                    ResultLabel.Text = $"Bad luck! The word was {targetWord}";
                }

                // Disable all entries after game ends
                DisableAllEntries();

                // Check achievements
                var gameTime = DateTime.Now - gameStartTime;
                await AchievementManager.CheckAchievements(isWon, count, gameTime);
            }

            // Catch any exceptions and display an error message
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EndGame: {ex.Message}");
                await DisplayAlert("Error", "Failed to save game progress", "OK");
            }
        }

        // Helper method to get guess history
        private List<string> GetGuessHistory()
        {
            // Create a new list to store the guesses
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

                // Add the guess to the list if it's a valid length
                if (guess.Length == 5)
                {
                    guesses.Add(guess);
                }
            }
            return guesses;
        }

        // View progress button click event handler to navigate to the SaveGamePage
        private async void OnViewProgressClicked(object sender, EventArgs e)
        {
            // Try to navigate to the SaveGamePage
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

            // Catch any exceptions and display an error message
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}"); 
                await DisplayAlert("Error", $"Navigation failed: {ex.Message}", "OK"); 
            }
        }

        // Special handlers for the first box
        private void OnFirstBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            // Try to handle the first box text changed event
            try
            {
                if (sender is not Entry entry) // If the sender is not an entry
                    return;

                string input = e.NewTextValue?.ToUpper() ?? ""; // Get the new text value and convert to uppercase

                if (input.Length > 0) // input length has to be greater than 0
                {
                    if (!char.IsLetter(input[0])) // If the input is not a letter
                    {
                        entry.Text = ""; // returns empty string if not a letter
                        return;
                    }

                    entry.Text = input[0].ToString(); // Set the text to the first character of user input

                    // Force color update
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        entry.TextColor = IsDarkMode ? Colors.White : Colors.Black;
                    });

                    // Move to next box
                    Row1Letter2?.Focus();
                }
            }

            // Catch any exceptions and display an error message
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in first box text changed: {ex.Message}");
            }
        }

        // Special handlers for the first box 
        private void OnFirstBoxFocused(object sender, FocusEventArgs e)
        {
            // Set text to empty and color to white
            var entry = (Entry)sender;
            entry.Text = string.Empty; // Sets the text to empty
            entry.TextColor = IsDarkMode ? Colors.White : Colors.Black; // Sets the text color to white if dark mode is enabled, otherwise black.
            System.Diagnostics.Debug.WriteLine($"First box focused - color set to: {entry.TextColor}");
        }

        // Record of recent players
        private async Task ShowRecentPlayers()
        {
            var players = await Player.GetExistingPlayers(); // GetS existing players from the player class
            var recentPlayers = players.Take(5).ToList(); // Show last 5 players

            string result = await DisplayActionSheet( // Displays all current players and the option to cancel
                "Recent Players",
                "Cancel",
                null,
                recentPlayers.ToArray()); // Displays the list of recent players

            if (!string.IsNullOrEmpty(result) && result != "Cancel") // If the result is not empty and not cancel
            {
                await Player.SavePlayerName(result); // Saves the player name and result
                await DisplayAlert("Success", $"Switched to {result}", "OK"); // Displays the message that the player has been switched
            }
        }

        // Event handler for the recent players button
        private void OnGuessSubmitted(object sender, EventArgs e)
        {
            // Try to handle the guess submitted event
            try
            {
                string guess = ""; // Initialize the guess string
                bool isValid = true; // Set the isValid flag to true

                // Debug output
                System.Diagnostics.Debug.WriteLine($"Current row: {count}");

                // First check if all entries have values
                for (int i = 1; i <= 5; i++)
                {
                    var entry = this.FindByName<Entry>($"Row{count}Letter{i}");
                    var text = entry?.Text?.Trim() ?? "";

                    System.Diagnostics.Debug.WriteLine($"Entry {i}: '{text}'");

                    // Check if the entry is empty
                    if (string.IsNullOrEmpty(text))
                    {
                        isValid = false; 
                        break;
                    }
                    guess += text.ToUpper(); // Convert the text to uppercase
                }

                if (!isValid) // If the guess is not valid
                {
                    DisplayAlert("Invalid", "Please enter all letters", "OK"); // Display the alert that the guess is invalid
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Complete guess: '{guess}'");

                // Process valid guess using the class-level color variables
                for (int i = 0; i < 5; i++)
                {
                    var entry = this.FindByName<Entry>($"Row{count}Letter{i + 1}"); // Finds the entry by name and count
                    var border = this.FindByName<Border>($"Border{count}Letter{i + 1}"); // Finds the border by name and count
                    var currentLetter = guess[i]; // Gets the current letter from the guess

                    if (currentLetter == targetWord[i]) // If the current letter is equal to the target word it will set the border background color to green
                    {
                        border.BackgroundColor = correctColor;  // Set the border background color to green
                        entry.TextColor = Colors.White; 
                    } 
                    else if (targetWord.Contains(currentLetter)) // If the target word contains the current letter it will set the border background color to yellow
                    {
                        border.BackgroundColor = presentColor;  // Set the border background color to yellow
                        entry.TextColor = Colors.White;
                    }
                    else // Otherwise it will set the border background color to grey
                    {
                        border.BackgroundColor = incorrectColor; // Set the border background color to grey
                        entry.TextColor = Colors.White;
                    }

                    entry.IsEnabled = false; // Disable the entry after setting the colors
                }

                if (guess == targetWord) // If the guess is equal to the target word
                {
                    EndGame(true); // End the game with a win
                    return;
                }
                

                if (count >= MaxGuesses)  // MaxGuesses of 6
                {
                    EndGame(false); // End the game with a loss
                    return;
                }

                count++; // Increment the count
                var nextEntry = this.FindByName<Entry>($"Row{count}Letter1"); // Find the next entry
                if (nextEntry != null)
                {
                    nextEntry.Focus(); // Focus on the next entry
                }

                // Reset consecutive hint flag when a guess is submitted
                usedHintLastTurn = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnGuessSubmitted: {ex.Message}");
            }
        }

        // FocusNextRow method  
        private void FocusNextRow()
        {
            try
            {
                // Focus the first entry of the next row
                var nextEntry = this.FindByName<Entry>($"Row{count}Letter1");
                nextEntry?.Focus(); 
                System.Diagnostics.Debug.WriteLine($"Focusing next row: Row{count}Letter1");
            }

            // Catch any exceptions and display an error message
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error focusing next row: {ex.Message}");
            }
        }

        // Method to initialize the game
        private async Task InitializeGame()
        {
            // Reset game state
            try
            {
                await LoadWordsAsync(); // Load the words asynchronously
                SelectRandomWord(); // Select a random word from the list
                count = 1; // Set the count to 1

                // Reset all entries
                ResetAllEntries();

                System.Diagnostics.Debug.WriteLine($"Game initialized with word: {targetWord}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing game: {ex.Message}");
            }
        }

        // Method to reset all entries and their colors to the default state
        private void ResetAllEntries()
        {
            // Reset all entries and their colors
            for (int row = 1; row <= 6; row++)
            {
                for (int col = 1; col <= 5; col++) // For loop to iterate through the rows and columns
                {
                    var entry = this.FindByName<Entry>($"Row{row}Letter{col}");
                    var border = this.FindByName<Border>($"Border{row}Letter{col}");

                    if (entry != null)
                    {
                        entry.Text = ""; // Set the text to empty
                        entry.IsEnabled = true; // Set the entry to true to enable it

                        // Special handling for first box
                        if (entry == Row1Letter1)
                        {
                            entry.TextColor = IsDarkMode ? Colors.White : Colors.Black; // Set the text color to white if dark mode is enabled, otherwise black.
                        }
                        else
                        {
                            entry.TextColor = IsDarkMode ? darkModeForeground : lightModeForeground; // Set the text color based on the current mode dark mode or light mode.
                        }
                    }

                    if (border != null) // If the border is not null
                    {
                        border.BackgroundColor = Colors.Transparent; // Set the background color to transparent
                        border.Stroke = Colors.Gray; // Set the stroke color to gray
                    }
                }
            }

            // Focus the first entry
            Row1Letter1?.Focus();

            // Reset hints if in Easy mode
            if (currentDifficulty == GameDifficulty.Easy)
            {
                ResetHints(); // Reset the hints 
            }
        }

        // Add property for maximum guesses
        private int MaxGuesses { get; set; } = 6;

        // Add property for current game difficulty
        private async void OnHintButtonClicked(object sender, EventArgs e)
        {
            if (currentDifficulty != GameDifficulty.Easy) // If the current difficulty is not easy no hints will be available
            {
                await DisplayAlert("Hints Disabled", "Hints are only available in Easy Mode", "OK");
                return;
            }

            if (hintsRemaining <= 0) // If the hints remaining is less than or equal to 0
            {
                await DisplayAlert("No Hints", "You've used all your hints for this game!", "OK");
                return;
            }

            if (usedHintLastTurn) // If the hint was used last turn
            {
                await DisplayAlert("Hint Blocked", "You can't use hints on consecutive guesses", "OK");
                return;
            }

            try
            {
                // Create a list to store positions that haven't been revealed yet
                var availablePositions = new List<int>();

                // Iterate through the positions
                for (int i = 0; i < 5; i++)
                {
                    // Finds the Entry control dynamically by its name using the format "Row{count}Letter{i + 1}"
                    var entry = this.FindByName<Entry>($"Row{count}Letter{i + 1}");

                    // Check if the Entry's text does not match the corresponding character in the target word
                    if (entry?.Text != targetWord[i].ToString())
                    {
                        // If the text doesn't match, add the current position (index) to the list of available positions
                        availablePositions.Add(i);
                    }
                }

                // If there are available positions
                if (availablePositions.Count > 0)
                {
                    Random random = new Random(); // Create a new random object
                    int position = availablePositions[random.Next(availablePositions.Count)]; // Get a random position from the available positions

                    var entry = this.FindByName<Entry>($"Row{count}Letter{position + 1}"); 
                    var border = this.FindByName<Border>($"Border{count}Letter{position + 1}");

                    if (entry != null && border != null) // If the entry and border are not null
                    {
                        entry.Text = targetWord[position].ToString(); // Set the entry text to the target word at the current position to reveal the letter
                        entry.IsEnabled = false; // Disable the entry after revealing the letter
                        entry.TextColor = Colors.White; // Set the text color to white
                        border.BackgroundColor = correctColor; // Set the border background color to green

                        hintsRemaining--; // Decrement the hints remaining
                        usedHintLastTurn = true; // Set the used hint last turn flag to true

                        HintButton.Text = $"Get Hint ({hintsRemaining} left)";
                        await DisplayAlert("Hint Used", $"Letter {position + 1} is '{targetWord[position]}'\nHints remaining: {hintsRemaining}", "OK");
                    }
                }

                // If there are no available positions
                else
                {
                    await DisplayAlert("No More Hints", "All letters have been revealed!", "OK");
                }
            }

            // Catch any exceptions and display an error message
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error using hint: {ex.Message}");
                await DisplayAlert("Error", "Failed to show hint", "OK");
            }
        }

        // Method to Update the UI for the current difficulty
        private void UpdateUIForDifficulty()
        {
            switch (currentDifficulty) // Switch statement to check the current difficulty
            {
                case GameDifficulty.Easy: // If the current difficulty is easy
                    MaxGuesses = 6; // Set the maximum guesses to 6
                    hintsRemaining = 2; // Set the hints remaining to 2
                    HintButton.IsVisible = true; // Set the hint button to visible
                    ResultLabel.Text = $"Easy Mode: {hintsRemaining} hints remaining"; // Set the result label text to the easy mode with hints remaining
                    break;
                case GameDifficulty.Medium: // If the current difficulty is medium
                    MaxGuesses = 6; // Set the maximum guesses to 6
                    hintsRemaining = 0; // Set the hints remaining to 0
                    HintButton.IsVisible = false; // Set the hint button to false
                    ResultLabel.Text = "Medium Mode: Standard rules"; // Set the result label text to medium mode
                    break;
                case GameDifficulty.Hard: // If the current difficulty is hard
                    MaxGuesses = 4;     // Set the maximum guesses to 4
                    hintsRemaining = 0; // Set the hints remaining to 0
                    HintButton.IsVisible = false; // Set the hint button to false
                    ResultLabel.Text = "Hard Mode: Must use correct letters"; // Set the result label text to hard mode
                    break;
            }
        }

        // Method to reset the hints
        private void ResetHints()
        {
            hintsRemaining = 2; // Set the hints remaining to 2
            usedHintLastTurn = false; // Set the used hint last turn flag to false
            if (HintButton != null) // If the hint button is not null
            {
                HintButton.Text = $"Get Hint ({hintsRemaining} left)"; // Set the hint button text to get hint with the hints remaining
            }
        }

        // Method to exit Game State
        private async void OnExitButtonClicked(object sender, EventArgs e)
        {
            bool shouldExit = await DisplayAlert( // Display the alert to confirm if the user wants to exit the game
                "Exit Game",
                "Are you sure you want to exit the game?",
                "Yes",
                "No");

            if (shouldExit) // If the user confirms the exit
            {
                Application.Current?.Quit(); // Quit the application
            }
        }

        // Method to share results
        private async void OnShareResultsClicked(object sender, EventArgs e)
        {
            // Try to share the results
            try
            {
                var shareText = GenerateShareText(); // Generate the share text
                await Share.RequestAsync(new ShareTextRequest // Request to share the text
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
                // Get the correct and present colors for the current row
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
                result.AppendLine(); // Add a new line after each row
            }

            return result.ToString(); // Return the result as a string
        }

        // Option Method to Start Daily Challenge 
        private async Task StartDailyChallenge()
        {
            try
            {
                // Check if already played today
                string todayKey = $"{DAILY_CHALLENGE_KEY}{DateTime.Today:yyyyMMdd}";
                bool alreadyPlayed = Preferences.Default.Get(todayKey, false);

                if (alreadyPlayed) // If the daily challenge has already been played
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

        // Get Daily Word Method to get the word of the day
        private string GetDailyWord()
        {
            // Use today's date as seed for consistent random number
            var today = DateTime.Today;
            var seed = today.Year * 10000 + today.Month * 100 + today.Day;
            var random = new Random(seed);

            // This ensures everyone gets the same word on the same day
            return wordList[random.Next(wordList.Count)].ToUpper();
        }

        // Get Random Word Method to get a random word from the list
        private string GetRandomWord()
        { 
            Random random = new Random(); // Create a new random object
            return wordList[random.Next(wordList.Count)]; // Return a random word from the list using the random object created 
        }

        // onSearchTextChanged method to handle the search text changed event
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
                    .ToList(); // Convert to list

                // Display matching words
                if (matchingWords.Any())
                {
                    ResultLabel.Text = $"Matching words:\n{string.Join(", ", matchingWords)}";
                }

                // No matching words found
                else
                {
                    ResultLabel.Text = "No matching words found";
                }              
            }

            // Catch any exceptions and display an error message
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                ResultLabel.Text = "Error searching words";
            }
        }

    }

}