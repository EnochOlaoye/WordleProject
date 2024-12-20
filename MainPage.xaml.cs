﻿using System.Net.Http;
using System.IO;
using Microsoft.Maui.Controls;

namespace Wordle
{
    public partial class MainPage : ContentPage
    {
        int count = 0;  
        private const string ThemePreferenceKey = "AppTheme"; // Add this constant for the preferences key
        private List<string> wordList;
        private const string WordsFileName = "words.txt";
        private const string WordsUrl = "https://raw.githubusercontent.com/DonH-ITS/jsonfiles/main/words.txt";
        // Add these fields at the top of the MainPage class
        private bool isDarkMode;
        private readonly Color darkModeBackground = Colors.Black;
        private readonly Color darkModeForeground = Colors.White;
        private readonly Color lightModeBackground = Colors.White;
        private readonly Color lightModeForeground = Colors.Black;
        private string targetWord;
        private readonly Color correctColor = Colors.Green;       // Letter is correct and in right position
        private readonly Color presentColor = Colors.Yellow;      // Letter exists but in wrong position
        private readonly Color incorrectColor = Colors.Gray;      // Letter is not in the word


        public MainPage()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("Starting word load...");
            //LoadWordsAsync();

            Task.Run(async () => await LoadWordsAsync()).Wait();  // Force synchronous wait       

            // Load saved theme preference
            isDarkMode = Preferences.Default.Get(ThemePreferenceKey, false); // false is the default value
            ApplyTheme();
        }

        private void OnThemeToggleClicked(object sender, EventArgs e)
        {
            isDarkMode = !isDarkMode;

            // Save the new theme preference
            Preferences.Default.Set(ThemePreferenceKey, isDarkMode);

            ApplyTheme();
        }

        private void ApplyTheme()
        {
            // Sets the page background color based on theme
            // If dark mode, uses black; if light mode, uses white
            BackgroundColor = isDarkMode ? darkModeBackground : lightModeBackground;

            // Loops through all Entry controls (the letter boxes)
            // Updates their text color based on theme
            foreach (var entry in GetAllEntries())
            {
                entry.TextColor = isDarkMode ? darkModeForeground : lightModeForeground;
                entry.BackgroundColor = Colors.Transparent;
            }

            // Loops through all Border controls (the boxes around the letters)
            // Updates their border color based on theme
            foreach (var border in GetAllBorders())
            {
                border.Stroke = isDarkMode ? darkModeForeground : Colors.Gray;
            }

            // Updates the Results label text color based on theme
            ResultLabel.TextColor = isDarkMode ? darkModeForeground : lightModeForeground;

            // Controls visibility and color of the "Light Mode" label
            // Shows when in dark mode (because you can switch to it)
            LightModeLabel.IsVisible = !isDarkMode;
            LightModeLabel.TextColor = isDarkMode ? darkModeForeground : lightModeForeground;

            // Controls visibility and color of the "Dark Mode" label
            // Shows when in light mode (because you can switch to it)
            DarkModeLabel.IsVisible = isDarkMode;
            DarkModeLabel.TextColor = isDarkMode ? darkModeForeground : lightModeForeground;

            // Updates the theme toggle button image
            // In dark mode: shows lightbulb.png (to indicate you can switch to light)
            // In light mode: shows darkbulb.png (to indicate you can switch to dark)
            ThemeButton.Source = isDarkMode ? "lightbulb.png" : "darkbulb.png";
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
            // Debug message to verify the button click
            System.Diagnostics.Debug.WriteLine("Submit button clicked!");
            ResultLabel.Text = "Button clicked!";  // Visual feedback


            string letter1 = GetEntryText($"Row{count + 1}Letter1");
            string letter2 = GetEntryText($"Row{count + 1}Letter2");
            string letter3 = GetEntryText($"Row{count + 1}Letter3");
            string letter4 = GetEntryText($"Row{count + 1}Letter4");
            string letter5 = GetEntryText($"Row{count + 1}Letter5");

            // Combine all letters into a single word
            string guess = $"{letter1}{letter2}{letter3}{letter4}{letter5}";

            System.Diagnostics.Debug.WriteLine($"Guess word: {guess}");


            if (string.IsNullOrWhiteSpace(guess) || guess.Length != 5)
            {
                // Display error message if any letter is missing
                ResultLabel.Text = "Please fill in all letters.";
                return;
            }

            count++;

            // Check the guess
            CheckGuess(guess.ToUpper());
        }

        // Helper method to get Entry text
        private string GetEntryText(string entryName)
        {
            var entry = this.FindByName<Entry>(entryName);
            return entry?.Text?.Trim() ?? "";
        }

        // Event handler that triggers whenever text changes in an Entry control
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Cast sender to Entry at the beginning of the method
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

                // Fix: Use entry.AutomationId for the switch statement
                switch (entry.AutomationId)
                {
                    case "Row1Letter1":
                        Row1Letter2?.Focus();
                        break;
                    case "Row1Letter2":
                        Row1Letter3?.Focus();
                        break;
                    case "Row1Letter3":
                        Row1Letter4?.Focus();
                        break;
                    case "Row1Letter4":
                        Row1Letter5?.Focus();
                        break;
                        // Add cases for other rows if needed
                }
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

                //wordList = content.Split('\n')
                wordList = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(w => w.Trim().ToUpper())
                                .Where(w => w.Length == 5)
                                .ToList();

                System.Diagnostics.Debug.WriteLine($"Processed {wordList.Count} words");

                // You might want to show a message when words are loaded
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
                entry.Text = ""; // Clear the entry when focused
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
                ResultLabel.Text = $"Well done! You found the word in {count} {(count == 1 ? "guess" : "guesses")}!";
                DisableAllEntries(); // Optional: disable input after win
            }
            else if (count >= 6)
            {
                // Loss condition
                ResultLabel.Text = $"Bad luck! The word was {targetWord}";
                DisableAllEntries(); // Optional: disable input after loss
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

            // Clear all entries and reset their colors
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
                        entry.TextColor = isDarkMode ? darkModeForeground : lightModeForeground;
                    }

                    if (border != null)
                    {
                        border.BackgroundColor = Colors.Transparent;
                        border.Stroke = isDarkMode ? darkModeForeground : Colors.Gray;
                    }
                }
            }

            // Select a new random word
            SelectRandomWord();

            // Reset the result label
            ResultLabel.Text = "New game started!";

            // Focus the first entry
            var firstEntry = this.FindByName<Entry>("Row1Letter1");
            firstEntry?.Focus();
        }

    }

}
