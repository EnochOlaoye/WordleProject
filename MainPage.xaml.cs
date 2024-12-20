using System.Net.Http;
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

        public MainPage()
        {
            InitializeComponent();
            LoadWordsAsync();

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
            // Increment attempt counter
            count++;

            // Get the current row's letters based on attempt count
            string letter1 = GetEntryText($"Row{count}Letter1");
            string letter2 = GetEntryText($"Row{count}Letter2");
            string letter3 = GetEntryText($"Row{count}Letter3");
            string letter4 = GetEntryText($"Row{count}Letter4");
            string letter5 = GetEntryText($"Row{count}Letter5");

            // Combine all letters into a single word
            string guess = $"{letter1}{letter2}{letter3}{letter4}{letter5}";

            // Validate that all Entry controls have a value
            // IsNullOrWhiteSpace checks for null, empty, or whitespace-only strings
            if (string.IsNullOrWhiteSpace(guess) || guess.Length != 5)
            {
                // Display error message if any letter is missing
                ResultLabel.Text = "Please fill in all letters.";
                count--; // Decrement count if validation fails
            }
            else
            {
                // If all letters are present, create the guess word and display it
 
                ResultLabel.Text = $"Attempt {count}: You entered '{guess}'";
            }

            // Announce the result for screen readers (accessibility feature)
            SemanticScreenReader.Announce(ResultLabel.Text);
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

                // Check if file exists locally
                if (!File.Exists(localPath))
                {
                    // Download file if it doesn't exist
                    using (var client = new HttpClient())
                    {
                        string words = await client.GetStringAsync(WordsUrl);
                        await File.WriteAllTextAsync(localPath, words);
                    }
                }

                // Read from local file
                string content = await File.ReadAllTextAsync(localPath);
                //wordList = content.Split('\n')
                wordList = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(w => w.Trim().ToUpper())
                                .Where(w => w.Length == 5)
                                .ToList();

                // You might want to show a message when words are loaded
                if (wordList.Count > 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ResultLabel.Text = $"Ready to play! ({wordList.Count} words loaded)";
                    });
                }
                else
                {
                    throw new Exception("No valid words were loaded");
                }
            }
            catch (Exception ex)
            {
                // Handle any errors
                ResultLabel.Text = "Error loading words. Please check your internet connection.";
                // You might want to log the error: Console.WriteLine(ex.Message);
                // Update UI on the main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ResultLabel.Text = "Error loading words. Please check your internet connection.";
                });
                Console.WriteLine($"Error loading words: {ex.Message}");
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
    }

}
