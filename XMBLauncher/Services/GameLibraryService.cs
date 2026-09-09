using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using XMBLauncher.Models;

namespace XMBLauncher.Services;

public class GameLibraryService
{
    private readonly string libraryDirectory;
    private readonly string libraryFilePath;

    public GameLibraryService()
    {
        libraryDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "XMBLauncher");

        libraryFilePath =
            Path.Combine(
                libraryDirectory,
                "games.json");
    }

    public List<Game> LoadGames()
    {
        try
        {
            if (!File.Exists(libraryFilePath))
            {
                return new List<Game>();
            }


            string json =
                File.ReadAllText(
                    libraryFilePath);


            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<Game>();
            }


            List<Game>? games =
                JsonSerializer.Deserialize<List<Game>>(
                    json);


            return games ??
                   new List<Game>();
        }
        catch
        {
      

            return new List<Game>();
        }
    }

    public void SaveGames(
        List<Game> games)
    {
        Directory.CreateDirectory(
            libraryDirectory);


        JsonSerializerOptions options =
            new JsonSerializerOptions
            {
                WriteIndented = true
            };


        string json =
            JsonSerializer.Serialize(
                games,
                options);


        File.WriteAllText(
            libraryFilePath,
            json);
    }


    public void AddGame(
        Game game,
        List<Game> games)
    {
        games.Add(game);

        SaveGames(games);
    }

    public void RemoveGame(
        Game game,
        List<Game> games)
    {
        games.Remove(game);

        SaveGames(games);
    }
}