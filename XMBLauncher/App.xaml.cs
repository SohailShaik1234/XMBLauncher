using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using XMBLauncher.Models;
using XMBLauncher.Services;

namespace XMBLauncher;

public partial class App : Application
{
    public static IGDBSettings IGDBSettings { get; private set; } = new();


    protected override void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);


        LoadIGDBSettings();
    }


    private static void LoadIGDBSettings()
    {
        string settingsPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "appsettings.json");


        if (!File.Exists(settingsPath))
        {
            MessageBox.Show(
                "appsettings.json could not be found.\n\n" +
                $"Expected location:\n{settingsPath}",
                "Configuration Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }


        try
        {
            string json =
                File.ReadAllText(
                    settingsPath);


            using JsonDocument document =
                JsonDocument.Parse(json);


            if (!document.RootElement.TryGetProperty(
                    "IGDB",
                    out JsonElement igdbElement))
            {
                throw new Exception(
                    "The IGDB section is missing " +
                    "from appsettings.json.");
            }


            if (igdbElement.TryGetProperty(
                    "ClientId",
                    out JsonElement clientIdElement))
            {
                IGDBSettings.ClientId =
                    clientIdElement.GetString() ?? "";
            }


            if (igdbElement.TryGetProperty(
                    "ClientSecret",
                    out JsonElement clientSecretElement))
            {
                IGDBSettings.ClientSecret =
                    clientSecretElement.GetString() ?? "";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not read appsettings.json.\n\n" +
                ex.Message,
                "Configuration Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}