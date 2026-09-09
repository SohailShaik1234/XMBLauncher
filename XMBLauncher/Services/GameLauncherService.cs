using System;
using System.Diagnostics;
using System.IO;
using XMBLauncher.Models;

namespace XMBLauncher.Services;

public class GameLauncherService
{
    private readonly GameLibraryService libraryService;


    public GameLauncherService()
    {
        libraryService =
            new GameLibraryService();
    }


    public bool LaunchGame(
        Game game,
        System.Collections.Generic.List<Game> games)
    {
        if (game == null)
        {
            return false;
        }


       
        if (string.IsNullOrWhiteSpace(
                game.ExecutablePath))
        {
            throw new InvalidOperationException(
                "This game does not have an executable path.");
        }


        if (!File.Exists(
                game.ExecutablePath))
        {
            throw new FileNotFoundException(
                "The game's executable could not be found.",
                game.ExecutablePath);
        }


        ProcessStartInfo startInfo =
            new ProcessStartInfo
            {
                FileName =
                    game.ExecutablePath,

                WorkingDirectory =
                    game.InstallDirectory,

                UseShellExecute =
                    true
            };

        Process? process =
            Process.Start(startInfo);


        if (process == null)
        {
            return false;
        }

        game.LaunchCount++;

        game.LastPlayed =
            DateTime.Now;


        libraryService.SaveGames(
            games);


        DateTime startTime =
            DateTime.Now;


        process.EnableRaisingEvents =
            true;


        process.Exited +=
            (sender, e) =>
            {
                TrackGameExit(
                    process,
                    game,
                    games,
                    startTime);
            };


        return true;
    }

    private void TrackGameExit(
        Process process,
        Game game,
        System.Collections.Generic.List<Game> games,
        DateTime startTime)
    {
        try
        {
            DateTime endTime =
                DateTime.Now;


            TimeSpan sessionDuration =
                endTime - startTime;


            if (sessionDuration.TotalSeconds < 0)
            {
                sessionDuration =
                    TimeSpan.Zero;
            }

            long sessionSeconds =
                (long)
                Math.Round(
                    sessionDuration.TotalSeconds);

            game.PlayTimeSeconds +=
                sessionSeconds;


            libraryService.SaveGames(
                games);
        }
        catch
        {
            
        }
        finally
        {
            process.Dispose();
        }
    }
}