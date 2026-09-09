namespace XMBLauncher.Models;

public class Game
{
    public string Name { get; set; } = "";

    public string Developer { get; set; } = "";

    public string Publisher { get; set; } = "";

    public string Genre { get; set; } = "";

    public string ReleaseDate { get; set; } = "";

    public string Description { get; set; } = "";

    public string ExecutablePath { get; set; } = "";

    public string InstallDirectory { get; set; } = "";

    public string CoverPath { get; set; } = "";

    public string BackgroundPath { get; set; } = "";

    public string IGDBId { get; set; } = "";

    public bool IsImported { get; set; }

    public long PlayTimeSeconds { get; set; }

    public DateTime? LastPlayed { get; set; }

    public int LaunchCount { get; set; }
}