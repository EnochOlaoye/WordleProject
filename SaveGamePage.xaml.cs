using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace Wordle;

// SaveGamePage class to manage the game progress and player statistics
public partial class SaveGamePage : ContentPage
{
    private readonly Color darkModeBackground = Colors.Black; // Set the dark mode background color
    private readonly Color darkModeForeground = Colors.White; // Set the dark mode foreground color
    private readonly Color lightModeBackground = Colors.White; // Set the light mode background color
    private readonly Color lightModeForeground = Colors.Black;  // Set the light mode foreground color
    private SaveGame currentSave = new SaveGame(); // Initialize the current save game object 

    // Default constructor to initialize the SaveGamePage
    public SaveGamePage()
    {
        InitializeComponent(); // Initialize the component of the page 
        ApplyTheme(); // Apply the theme to the page elements 
        _ = LoadProgressAsync(); // Load the game progress asynchronously
    }

    // Method to handle the navigation to the SaveGamePage
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args); // Call the base class OnNavigatedTo method 
        ApplyTheme(); // Apply the theme to the page elements 
        System.Diagnostics.Debug.WriteLine($"SaveGamePage navigated to - theme is {(MainPage.IsDarkMode ? "Dark" : "Light")}"); // Output the theme mode to the console
    }

    // Method to handle the appearance of the SaveGamePage
    protected override async void OnAppearing()
    {
        base.OnAppearing(); // Call the base class OnAppearing method
        string playerName = await Player.GetPlayerName(); // Get the current player name 
        PlayerNameLabel.Text = $"Player: {playerName}"; // Set the player name label
        PlayerNameLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground; // Set the player name label color based on the theme pefrence
        await LoadProgressAsync(); // Load the game progress asynchronously
    }

    // Method to handle the theme toggle button click event
    private void OnThemeToggleClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Theme toggle clicked"); // Output the theme toggle click event to the console
        MainPage.IsDarkMode = !MainPage.IsDarkMode; // Pick the application's theme between light and dark mode
        ApplyTheme(); // Apply the theme to the page elements 
    }

    // Method to apply the theme to the page elements 
    private void ApplyTheme()
    {
        System.Diagnostics.Debug.WriteLine($"Applying theme: {(MainPage.IsDarkMode ? "Dark" : "Light")}");

        // Set page background
        this.BackgroundColor = MainPage.IsDarkMode ? darkModeBackground : lightModeBackground;

        // Update "Your Progress" title label
        var progressTitle = this.FindByName<Label>("YourProgressLabel");
        if (progressTitle != null)
        {
            progressTitle.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground; // Set the progress title label color based on the theme preference
        }

        // Update title labels
        GamesPlayedTitle.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground; // Set the games played title label color based on the theme preference
        GamesWonTitle.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground; // Set the games won title label color based on the theme preference
        CurrentStreakTitle.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground; // Set the current streak title label color based on the theme preference
        MaxStreakTitle.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground; // Set the max streak title label color based on the theme preference

        // Update value labels
        GamesPlayedLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground; // Set the games played label color based on the theme preference
        GamesWonLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground; // Set the games won label color based on the theme preference
        CurrentStreakLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground; // Set the current streak label color based on the theme preference
        MaxStreakLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground; // Set the max streak label color based on the theme preference

        // Update theme controls - Fixed the visibility logic
        LightModeLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground; // Set the light mode label color based on the theme preference
        DarkModeLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground; // Set the dark mode label color based on the theme preference
        LightModeLabel.IsVisible = MainPage.IsDarkMode;  // Show Light Mode option when in Dark Mode
        DarkModeLabel.IsVisible = !MainPage.IsDarkMode;  // Show Dark Mode option when in Light Mode

        // Set the theme button image based on the theme preference
        ThemeButton.Source = MainPage.IsDarkMode ? "lightbulb.png" : "darkbulb.png";

        // Set the history label color based on the theme preference
        HistoryLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;

        // Set the player name label color based on the theme preference
        PlayerNameLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground; 

        System.Diagnostics.Debug.WriteLine("Theme applied to all elements"); // Output the theme application to the console
    }

    // Method to load the game progress asynchronously
    private async Task LoadProgressAsync()
    {
        // Try to load the game progress
        try
        {
            string currentPlayer = await Player.GetPlayerName(); // Get the current player name
            currentSave = await SaveGame.Load(currentPlayer); // Load the save game for the current player

            GamesPlayedLabel.Text = currentSave.GamesPlayed.ToString(); // Set the games played label
            GamesWonLabel.Text = currentSave.GamesWon.ToString(); // Set the games won label
            double winPercentage = currentSave.GamesPlayed > 0 // Calculate the win percentage
                ? (double)currentSave.GamesWon / currentSave.GamesPlayed * 100
                : 0;
            WinPercentageLabel.Text = $"Win %: {winPercentage:F1}%"; // Set the win percentage label
            CurrentStreakLabel.Text = currentSave.CurrentStreak.ToString(); // Set the current streak label
            MaxStreakLabel.Text = currentSave.MaxStreak.ToString(); // Set the max streak label

            // Display the history list if available
            if (currentSave.History != null)
            {
                HistoryList.ItemsSource = currentSave.History.GetSortedAttempts(); // Set the history list items source
            }
        }

        // Catch any exceptions and display an alert message
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading progress: {ex.Message}"); // Output the error message to the console
            await DisplayAlert("Error", "Unable to load game progress", "OK"); // Display an alert message
        }       
    }

    // Method to handle the back to game button click event
    private async void OnBackToGameClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync(); // Navigate back to the game page
    }

    // Method to handle the LoadProgress button click event
    private async void LoadProgress()
    {
        // Try to load the game progress
        try
        {
            string currentPlayer = await Player.GetPlayerName(); // Get the current player name
            var save = await SaveGame.Load(currentPlayer); // Load the save game for the current player

            GamesPlayedLabel.Text = save.GamesPlayed.ToString(); // Set the games played label
            GamesWonLabel.Text = save.GamesWon.ToString(); // Set the games won label
            CurrentStreakLabel.Text = save.CurrentStreak.ToString(); // Set the current streak label
            MaxStreakLabel.Text = save.MaxStreak.ToString(); // Set the max streak label

            // Calculate the win percentage
            if (save.History != null)
            {
                HistoryList.ItemsSource = save.History.GetSortedAttempts(); // Set the history list items source
            }
        }

        // Catch any exceptions and display an alert message
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading progress: {ex.Message}"); // Output the error message to the console
            await DisplayAlert("Error", "Failed to load game progress", "OK"); // Display an alert message
        }        
    }

    // Method to Load the Save Data asynchronously 
    private async void LoadSaveData()
    {
        try
        {
            // Get current player name
            string currentPlayer = await Player.GetPlayerName();

            // Load save data for current player
            currentSave = await SaveGame.Load(currentPlayer);

            // Update UI with player stats
            GamesPlayedLabel.Text = $"Games Played: {currentSave.GamesPlayed}";
            GamesWonLabel.Text = $"Games Won: {currentSave.GamesWon}";
            WinPercentageLabel.Text = $"Win %: {CalculateWinPercentage():F1}%";
            CurrentStreakLabel.Text = $"Current Streak: {currentSave.CurrentStreak}";
            MaxStreakLabel.Text = $"Max Streak: {currentSave.MaxStreak}";
        }
              
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Failed to load save data", "OK");
            System.Diagnostics.Debug.WriteLine($"Error loading save data: {ex.Message}");
        }
    }

    // Method to Calculate the Win Percentage
    private double CalculateWinPercentage()
    {
        if (currentSave.GamesPlayed == 0) return 0; // Return 0 if no games played
        return (double)currentSave.GamesWon / currentSave.GamesPlayed * 100; // Calculate the win percentage 
    }

    // Method to hanlde onBackButtonClicked event 
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync(); // Navigate back to the previous page
    }

    // Method to handle the Manage Players button click event
    private async void OnManagePlayersClicked(object sender, EventArgs e)
    {
        // Try to manage the players 
        try
        {
            var players = await Player.GetExistingPlayers(); // Get the existing players
            if (players.Count == 0) // Check if there are no saved games
            {
                await DisplayAlert("No Saved Games", "There are no saved games to manage.", "OK"); // Display an alert message
                return; 
            }            

            string action = await DisplayActionSheet( // Display an action sheet to select a player to delete
                "Select Player to Delete",
                "Cancel",
                null,
                players.ToArray()); // Set the action sheet options to the player names

            if (!string.IsNullOrEmpty(action) && action != "Cancel") // If cancel is clicked display an alert message
            {
                bool confirm = await DisplayAlert( // Display an alert message to confirm the deletion
                    "Delete Save Game",
                    $"Are you sure you want to delete {action}'s save game? This cannot be undone.",
                    "Yes, Delete",
                    "Cancel");

                if (confirm) // If the deletion is confirmed
                {
                    string currentPlayer = await Player.GetPlayerName(); // Get the current player name

                    // Delete save game file
                    string savePath = Path.Combine(FileSystem.AppDataDirectory, $"{action}_save.json");
                    if (File.Exists(savePath))
                        File.Delete(savePath);

                    // Delete history file
                    string historyPath = Path.Combine(FileSystem.AppDataDirectory, $"{action}_history.json");
                    if (File.Exists(historyPath))
                        File.Delete(historyPath);

                    await DisplayAlert("Success", $"Deleted save game for {action}", "OK");

                    // If current player was deleted, switch to another player or create new one
                    if (action == currentPlayer)
                    {
                        // Get remaining players
                        var remainingPlayers = await Player.GetExistingPlayers();

                        if (remainingPlayers.Count > 0)
                        {
                            // Switch to first available player
                            await Player.SavePlayerName(remainingPlayers[0]);
                            var existingSave = await SaveGame.Load(remainingPlayers[0]);
                            await LoadProgressAsync();
                            await DisplayAlert("Player Switched",
                                $"Switched to existing player: {remainingPlayers[0]}", "OK");
                        }
                        else
                        {
                            // Create new default player
                            string newPlayer = "Player";
                            await Player.SavePlayerName(newPlayer);
                            var newSave = new SaveGame
                            {
                                GamesPlayed = 0,
                                GamesWon = 0,
                                CurrentStreak = 0,
                                MaxStreak = 0,
                                GuessDistribution = new Dictionary<int, int>(),
                                History = new PlayerHistory { PlayerName = newPlayer }
                            };
                            newSave.Save(newPlayer); // Save the new player
                            await LoadProgressAsync(); // Load the progress asynchronously
                            await DisplayAlert("New Player Created",
                                "Created new default player as no other saves exist", "OK");
                        }
                    }
                }
            }
        }

        // Catch any exceptions and display an alert message
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error managing players: {ex.Message}");
            await DisplayAlert("Error", "Failed to manage saved games", "OK");
        }
    }

}