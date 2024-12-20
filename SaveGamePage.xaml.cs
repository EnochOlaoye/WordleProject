using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Wordle;

public partial class SaveGamePage : ContentPage
{
    private readonly Color darkModeBackground = Colors.Black;
    private readonly Color darkModeForeground = Colors.White;
    private readonly Color lightModeBackground = Colors.White;
    private readonly Color lightModeForeground = Colors.Black;

    public SaveGamePage()
    {
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine($"SaveGamePage: Theme is {(MainPage.IsDarkMode ? "Dark" : "Light")}");
        ApplyTheme();
        LoadProgress();
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

    private async void LoadProgress()
    {
        var save = await SaveGame.Load();

        GamesPlayedLabel.Text = save.GamesPlayed.ToString();
        GamesWonLabel.Text = save.GamesWon.ToString();
        CurrentStreakLabel.Text = save.CurrentStreak.ToString();
        MaxStreakLabel.Text = save.MaxStreak.ToString();

        // Bind history directly
        HistoryList.ItemsSource = save.History.GetSortedAttempts();
    }

    private async void OnBackToGameClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}