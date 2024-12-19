using System.Net.Http;
using System.IO;
using Microsoft.Maui.Controls;

namespace Wordle
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        private List<string> wordList;
        private const string WordsFileName = "words.txt";
        private const string WordsUrl = "https://raw.githubusercontent.com/DonH-ITS/jsonfiles/main/words.txt";
        private bool isDarkMode;
        private readonly Color darkModeBackground = Colors.Black;
        private readonly Color darkModeForeground = Colors.White;
        private readonly Color lightModeBackground = Colors.White;
        private readonly Color lightModeForeground = Colors.Black;

        public MainPage()
        {
            InitializeComponent();
            LoadWordsAsync();

            // Initialize theme based on system preference
            isDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;
            ApplyTheme();
        }

        private void OnThemeToggleClicked(object sender, EventArgs e)
        {
            isDarkMode = !isDarkMode;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            // Set the background and text colors for the page
            BackgroundColor = isDarkMode ? darkModeBackground : lightModeBackground;

            // Update all Entry controls
            foreach (var entry in GetAllEntries())
            {
                entry.TextColor = isDarkMode ? darkModeForeground : lightModeForeground;
                entry.BackgroundColor = Colors.Transparent;
            }

            // Update all Borders
            foreach (var border in GetAllBorders())
            {
                border.Stroke = isDarkMode ? darkModeForeground : Colors.Gray;
            }

            // Update other UI elements
            ResultLabel.TextColor = isDarkMode ? darkModeForeground : lightModeForeground;
            SubmitButton.TextColor = isDarkMode ? darkModeForeground : lightModeForeground;

            // Update theme toggle button (if you have an icon)
            ThemeToggleButton.Source = isDarkMode ? "lightbulb_on.png" : "lightbulb.png";
        }

        private IEnumerable<Entry> GetAllEntries()
        {
            // Helper method to find all Entry controls
            var entries = new List<Entry>();
            for (int row = 1; row <= 6; row++)
            {
                for (int col = 1; col <= 5; col++)
                {
                    var entry = FindByName<Entry>($"Row{row}Letter{col}");
                    if (entry != null)
                        entries.Add(entry);
                }
            }
            return entries;
        }

        private IEnumerable<Border> GetAllBorders()
        {
            // Helper method to find all Border controls
            var borders = new List<Border>();
            for (int row = 1; row <= 6; row++)
            {
                for (int col = 1; col <= 5; col++)
                {
                    var border = FindByName<Border>($"Border{row}Letter{col}");
                    if (border != null)
                        borders.Add(border);
                }
            }
            return borders;

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
