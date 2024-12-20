using System;
using System.Text.Json;
namespace Wordle
{

    public class Player
    {
        public string Name { get; set; }

        public Player()
        {
            Name = string.Empty;
        }
        public static async Task<string> GetPlayerName()
        {
            string path = Path.Combine(FileSystem.AppDataDirectory, "player.json");

            if (File.Exists(path))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(path);
                    var player = JsonSerializer.Deserialize<Player>(json);
                    return player?.Name ?? string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }
            return string.Empty;
        }
        public static async Task SavePlayerName(string name)
        {
            var player = new Player { Name = name };
            string json = JsonSerializer.Serialize(player);
            string path = Path.Combine(FileSystem.AppDataDirectory, "player.json");
            await File.WriteAllTextAsync(path, json);
        }
    }
}

