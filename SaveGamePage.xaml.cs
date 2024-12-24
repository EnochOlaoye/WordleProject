using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace Wordle;

public partial class SaveGamePage : ContentPage
{
    private readonly Color darkModeBackground = Colors.Black;
    private readonly Color darkModeForeground = Colors.White;
    private readonly Color lightModeBackground = Colors.White;
    private readonly Color lightModeForeground = Colors.Black;
    private SaveGame currentSave = new SaveGame();

    public SaveGamePage()
    {
        InitializeComponent();
        ApplyTheme();
        _ = LoadProgressAsync();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        ApplyTheme();
        System.Diagnostics.Debug.WriteLine($"SaveGamePage navigated to - theme is {(MainPage.IsDarkMode ? "Dark" : "Light")}");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        string playerName = await Player.GetPlayerName();
        PlayerNameLabel.Text = $"Player: {playerName}";
        PlayerNameLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;
        await LoadProgressAsync();
    }

    private void OnThemeToggleClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Theme toggle clicked");
        MainPage.IsDarkMode = !MainPage.IsDarkMode;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        System.Diagnostics.Debug.WriteLine($"Applying theme: {(MainPage.IsDarkMode ? "Dark" : "Light")}");

        // Set page background
        this.BackgroundColor = MainPage.IsDarkMode ? darkModeBackground : lightModeBackground;

        // Update "Your Progress" title label
        var progressTitle = this.FindByName<Label>("YourProgressLabel");
        if (progressTitle != null)
        {
            progressTitle.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;
        }

        // Update title labels
        GamesPlayedTitle.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;
        GamesWonTitle.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;
        CurrentStreakTitle.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;
        MaxStreakTitle.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;

        // Update value labels
        GamesPlayedLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;
        GamesWonLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;
        CurrentStreakLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;
        MaxStreakLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;

        // Update theme controls - Fixed the visibility logic
        LightModeLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;
        DarkModeLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;
        LightModeLabel.IsVisible = MainPage.IsDarkMode;  // Show Light Mode option when in Dark Mode
        DarkModeLabel.IsVisible = !MainPage.IsDarkMode;  // Show Dark Mode option when in Light Mode

        // Update theme button
        ThemeButton.Source = MainPage.IsDarkMode ? "lightbulb.png" : "darkbulb.png";

        // Add history label to theme
        HistoryLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;

        // Update player name label
        PlayerNameLabel.TextColor = MainPage.IsDarkMode ? darkModeForeground : lightModeForeground;

        System.Diagnostics.Debug.WriteLine("Theme applied to all elements");
    }

    private async Task LoadProgressAsync()
    {
        try
        {
            string currentPlayer = await Player.GetPlayerName();
            currentSave = await SaveGame.Load(currentPlayer);

            GamesPlayedLabel.Text = currentSave.GamesPlayed.ToString();
            GamesWonLabel.Text = currentSave.GamesWon.ToString();
            double winPercentage = currentSave.GamesPlayed > 0
                ? (double)currentSave.GamesWon / currentSave.GamesPlayed * 100
                : 0;
            WinPercentageLabel.Text = $"Win %: {winPercentage:F1}%";
            CurrentStreakLabel.Text = currentSave.CurrentStreak.ToString();
            MaxStreakLabel.Text = currentSave.MaxStreak.ToString();

            if (currentSave.History != null)
            {
                HistoryList.ItemsSource = currentSave.History.GetSortedAttempts();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading progress: {ex.Message}");
            await DisplayAlert("Error", "Unable to load game progress", "OK");
        }
    }

    private async void OnBackToGameClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void LoadProgress()
    {
        try
        {
            string currentPlayer = await Player.GetPlayerName();
            var save = await SaveGame.Load(currentPlayer);

            GamesPlayedLabel.Text = save.GamesPlayed.ToString();
            GamesWonLabel.Text = save.GamesWon.ToString();
            CurrentStreakLabel.Text = save.CurrentStreak.ToString();
            MaxStreakLabel.Text = save.MaxStreak.ToString();

            if (save.History != null)
            {
                HistoryList.ItemsSource = save.History.GetSortedAttempts();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading progress: {ex.Message}");
        }
    }

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

    private double CalculateWinPercentage()
    {
        if (currentSave.GamesPlayed == 0) return 0;
        return (double)currentSave.GamesWon / currentSave.GamesPlayed * 100;
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnManagePlayersClicked(object sender, EventArgs e)
    {
        try
        {
            var players = await Player.GetExistingPlayers();
            if (players.Count == 0)
            {
                await DisplayAlert("No Saved Games", "There are no saved games to manage.", "OK");
                return;
            }

            string action = await DisplayActionSheet(
                "Select Player to Delete",
                "Cancel",
                null,
                players.ToArray());

            if (!string.IsNullOrEmpty(action) && action != "Cancel")
            {
                bool confirm = await DisplayAlert(
                    "Delete Save Game",
                    $"Are you sure you want to delete {action}'s save game? This cannot be undone.",
                    "Yes, Delete",
                    "Cancel");

                if (confirm)
                {
                    string currentPlayer = await Player.GetPlayerName();

                    // Delete save game file
                    string savePath = Path.Combine(FileSystem.AppDataDirectory, $"{action}_save.json");
                    if (File.Exists(savePath))
                        File.Delete(savePath);

                    // Delete history file
                    string historyPath = Path.Combine(FileSystem.AppDataDirectory, $"{action}_history.json");
                    if (File.Exists(historyPath))
                        File.Delete(historyPath);

                    await DisplayAlert("Success", $"Deleted save game for {action}", "OK");

                    // If current player was deleted, create new save
                    if (action == currentPlayer)
                    {
                        var newSave = new SaveGame
                        {
                            GamesPlayed = 0,
                            GamesWon = 0,
                            CurrentStreak = 0,
                            MaxStreak = 0,
                            GuessDistribution = new Dictionary<int, int>(),
                            History = new PlayerHistory { PlayerName = currentPlayer }
                        };
                        newSave.Save(currentPlayer);
                        await LoadProgressAsync(); // Reload the page
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error managing players: {ex.Message}");
            await DisplayAlert("Error", "Failed to manage saved games", "OK");
        }
    }

}