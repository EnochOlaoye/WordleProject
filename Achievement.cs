using System.Text.Json;

namespace Wordle;

public class Achievement
{
    // Constructor to initialize the title and description of the achievement
    public Achievement(string title, string description)
    {

        Title = title ?? throw new ArgumentNullException(nameof(title)); // Title of the achievement
        Description = description ?? throw new ArgumentNullException(nameof(description)); // Description of the achievement
        IsUnlocked = false; // Set to false as you start off with no Achievements
        UnlockedDate = null; // Date and time when the achievement is unlocked
    }

    // Title of the achivement
    public string Title { get; set; }

    // Description of the achievement, explaining what it involves
    public string Description { get; set; }

    // Represents whether it has been achieved or not.
    public bool IsUnlocked { get; set; }

    // Saves date and time when the achievement gets unlocked. 
    public DateTime? UnlockedDate { get; set; }
}

// Public class to Manage and organizes the achievement
public class AchievementManager
{
    private const string ACHIEVEMENTS_FILE = "achievements.json"; // Constant string file name to save to JSON format
    
    // List that holds default set of Achievements
    private static List<Achievement> _achievements = new()
    {
        // Achievement for winning first game
        new Achievement(
            "First Win",
            "Win your first game"
        ),

        // Achievement for winning in under 30 seconds
        new Achievement(
            "Speed Demon",
            "Win in under 30 seconds"
        ),

        // Achievement for winning in one guess
        new Achievement(
            "Perfect Game",
            "Win in one guess"
        )
    };

    // To check Achievements
    public static async Task CheckAchievements(bool won, int guesses, TimeSpan gameTime)
    {
        // Loads Achievement Data
        await LoadAchievements();

        // Set to false as you start off with no Achievements
        var unlockedAny = false;

        // If statement to Check First Win
        if (won && !_achievements[0].IsUnlocked)
        {
            // Updates Achievements for every game you win
            _achievements[0].IsUnlocked = true;
            _achievements[0].UnlockedDate = DateTime.Now;
            unlockedAny = true;
            // Updates your Achievement and displays Message in main page 
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Achievement Unlocked!",
                    $"🏆 {_achievements[0].Title}\n{_achievements[0].Description}",
                    "OK");
            }
        }

        // If statement Check Speed Demon if you can complete game in under 30 seconds
        if (won && gameTime.TotalSeconds < 30 && !_achievements[1].IsUnlocked)
        {
            _achievements[1].IsUnlocked = true;
            _achievements[1].UnlockedDate = DateTime.Now;
            unlockedAny = true;
            // Updates your Achievement and displays Message in main page 
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Achievement Unlocked! You completed Wordle in under 30 seconds",
                    $"🏆 {_achievements[1].Title}\n{_achievements[1].Description}",
                    "OK");
            }
        }

        // Check to see if you had Perfect Game in one guess
        if (won && guesses == 1 && !_achievements[2].IsUnlocked)
        {
            _achievements[2].IsUnlocked = true;
            _achievements[2].UnlockedDate = DateTime.Now;
            unlockedAny = true;
            // Updates your Achievement and displays Message in main page 
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Achievement Unlocked! You completed Wordle in one guess",
                    $"🏆 {_achievements[2].Title}\n{_achievements[2].Description}",
                    "OK");
            }
        }

        // if any achievements were unlocked, save the updated list
        if (unlockedAny)
        {
            // Save the updated list of Achievements
            await SaveAchievements();
        }
    }

    // Load Achievements Method static async Task stores the Achievements
    private static async Task LoadAchievements()
    {
        // Load Achievements from JSON file
        try
        {
            // Path to save Achievements
            string path = Path.Combine(FileSystem.AppDataDirectory, ACHIEVEMENTS_FILE);
            // Check if file exists
            if (File.Exists(path))
            {
                // Read the file and store it in JSON format
                string json = await File.ReadAllTextAsync(path); // Read the file
                var loadedAchievements = JsonSerializer.Deserialize<List<Achievement>>(json); // Deserialize the JSON
                // If loadedAchievements is not null, assign its value to the _achievements list
                if (loadedAchievements != null)
                {
                    _achievements = loadedAchievements; // Assign the value to the _achievements list
                }
            }
        }
        catch (Exception)
        {
            System.Diagnostics.Debug.WriteLine("Error loading achievements");
        }
    }

    // Save Achievements Method static async Task stores the Achievements
    private static async Task SaveAchievements()
    {
        try
        {
            string path = Path.Combine(FileSystem.AppDataDirectory, ACHIEVEMENTS_FILE); // Path to save Achievements
            string json = JsonSerializer.Serialize(_achievements);  // Serialize the list of Achievements
            await File.WriteAllTextAsync(path, json); // Write the JSON to the file
        }
        catch (Exception)
        {
            System.Diagnostics.Debug.WriteLine("Error saving achievements"); 
        }
    }
}