using System.Text.Json;

namespace Wordle;

public class Achievement
{
    public Achievement(string title, string description)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        IsUnlocked = false;
        UnlockedDate = null;
    }

    public string Title { get; set; }
    public string Description { get; set; }
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockedDate { get; set; }
}

public class AchievementManager
{
    private const string ACHIEVEMENTS_FILE = "achievements.json";
    private static List<Achievement> _achievements = new()
    {
        new Achievement(
            "First Win",
            "Win your first game"
        ),
        new Achievement(
            "Speed Demon",
            "Win in under 30 seconds"
        ),
        new Achievement(
            "Perfect Game",
            "Win in one guess"
        )
    };

    public static async Task CheckAchievements(bool won, int guesses, TimeSpan gameTime)
    {
        await LoadAchievements();
        var unlockedAny = false;

        // Check First Win
        if (won && !_achievements[0].IsUnlocked)
        {
            _achievements[0].IsUnlocked = true;
            _achievements[0].UnlockedDate = DateTime.Now;
            unlockedAny = true;
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Achievement Unlocked!",
                    $"🏆 {_achievements[0].Title}\n{_achievements[0].Description}",
                    "OK");
            }
        }

        // Check Speed Demon
        if (won && gameTime.TotalSeconds < 30 && !_achievements[1].IsUnlocked)
        {
            _achievements[1].IsUnlocked = true;
            _achievements[1].UnlockedDate = DateTime.Now;
            unlockedAny = true;
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Achievement Unlocked!",
                    $"🏆 {_achievements[1].Title}\n{_achievements[1].Description}",
                    "OK");
            }
        }

        // Check Perfect Game
        if (won && guesses == 1 && !_achievements[2].IsUnlocked)
        {
            _achievements[2].IsUnlocked = true;
            _achievements[2].UnlockedDate = DateTime.Now;
            unlockedAny = true;
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Achievement Unlocked!",
                    $"🏆 {_achievements[2].Title}\n{_achievements[2].Description}",
                    "OK");
            }
        }

        if (unlockedAny)
        {
            await SaveAchievements();
        }
    }

    private static async Task LoadAchievements()
    {
        try
        {
            string path = Path.Combine(FileSystem.AppDataDirectory, ACHIEVEMENTS_FILE);
            if (File.Exists(path))
            {
                string json = await File.ReadAllTextAsync(path);
                var loadedAchievements = JsonSerializer.Deserialize<List<Achievement>>(json);
                if (loadedAchievements != null)
                {
                    _achievements = loadedAchievements;
                }
            }
        }
        catch (Exception)
        {
            System.Diagnostics.Debug.WriteLine("Error loading achievements");
        }
    }

    private static async Task SaveAchievements()
    {
        try
        {
            string path = Path.Combine(FileSystem.AppDataDirectory, ACHIEVEMENTS_FILE);
            string json = JsonSerializer.Serialize(_achievements);
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception)
        {
            System.Diagnostics.Debug.WriteLine("Error saving achievements");
        }
    }
}