using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using XMBLauncher.Models;
using XMBLauncher.Services;
using XMBLauncher.Views;

namespace XMBLauncher.ViewModels;

public class XmbViewModel
{
    private readonly Canvas gameCanvas;

    private readonly TextBlock selectedGameText;
    private readonly TextBlock selectedGameDeveloper;
    private readonly TextBlock selectedGameGenre;

    private readonly TextBlock selectedGamePlaytime;
    private readonly TextBlock selectedGameLaunchCount;
    private readonly TextBlock selectedGameLastPlayed;

    private readonly Canvas categoryGrid;

    private readonly GameLibraryService libraryService;
    private readonly GameImportService importService;
    private readonly IGDBService igdbService;

    private readonly List<Game> games;

    private readonly List<GameTile> gameTiles =
        new List<GameTile>();

    private readonly string[] categories =
    {
        "Games",
        "Settings"
    };

    private readonly string[] categoryIcons =
    {
        "Games.png",
        "Settings.png"
    };

    private int selectedCategoryIndex = 0;
    private int selectedGameIndex = 0;

    private const double CategoryIconSize = 145;
    private const double CategorySpacing = 220;

    private const double GameTileWidth = 230;
    private const double GameTileHeight = 190;

    private const double SelectedScale = 0.92;
    private const double AdjacentScale = 0.52;
    private const double FarScale = 0.40;

    private const double SelectedGameY = 205;
    private const double PreviousGameY = -170;
    private const double NextGameY = 375;
    private const double FarGameSpacing = 90;

    private const double GameAnimationMilliseconds = 300;

    public int SelectedCategoryIndex =>
        selectedCategoryIndex;

    public int LastCategoryDirection
    {
        get;
        private set;
    }

    public XmbViewModel(
        Canvas gameCanvas,
        TextBlock selectedGameText,
        TextBlock selectedGameDeveloper,
        TextBlock selectedGameGenre,
        TextBlock selectedGamePlaytime,
        TextBlock selectedGameLaunchCount,
        TextBlock selectedGameLastPlayed,
        Canvas categoryGrid)
    {
        this.gameCanvas =
            gameCanvas;

        this.selectedGameText =
            selectedGameText;

        this.selectedGameDeveloper =
            selectedGameDeveloper;

        this.selectedGameGenre =
            selectedGameGenre;

        this.selectedGamePlaytime =
            selectedGamePlaytime;

        this.selectedGameLaunchCount =
            selectedGameLaunchCount;

        this.selectedGameLastPlayed =
            selectedGameLastPlayed;

        this.categoryGrid =
            categoryGrid;

        libraryService =
            new GameLibraryService();

        importService =
            new GameImportService();

        igdbService =
            new IGDBService(
                App.IGDBSettings.ClientId,
                App.IGDBSettings.ClientSecret);

        games =
            libraryService.LoadGames();

        RefreshLayout();
    }

    public void HandleKey(
        Key key)
    {
        switch (key)
        {
            case Key.Left:

                if (selectedCategoryIndex > 0)
                {
                    selectedCategoryIndex--;

                    LastCategoryDirection = -1;

                    RefreshLayout(true);
                }

                break;

            case Key.Right:

                if (selectedCategoryIndex <
                    categories.Length - 1)
                {
                    selectedCategoryIndex++;

                    LastCategoryDirection = 1;

                    RefreshLayout(true);
                }

                break;

            case Key.Up:

                if (selectedCategoryIndex == 0)
                {
                    if (selectedGameIndex > 0)
                    {
                        selectedGameIndex--;

                        RefreshLayout(true);
                    }
                }

                break;

            case Key.Down:

                if (selectedCategoryIndex == 0)
                {
                    if (selectedGameIndex <
                        games.Count - 1)
                    {
                        selectedGameIndex++;

                        RefreshLayout(true);
                    }
                }

                break;

            case Key.Enter:

                if (selectedCategoryIndex == 0)
                {
                    LaunchSelectedGame();
                }

                break;

            case Key.I:

                if (selectedCategoryIndex == 0)
                {
                    ImportGame();
                }

                break;

            case Key.Delete:

                if (selectedCategoryIndex == 0)
                {
                    RemoveSelectedGame();
                }

                break;

            case Key.Escape:

                break;
        }
    }

    public void RefreshLayout()
    {
        RefreshLayout(false);
    }

    public void RefreshLayout(
        bool animate)
    {
        UpdateCategoryPositions();

        UpdateGamePositions(
            animate);

        UpdateSelectedGameInfo();
    }

    private void UpdateCategoryPositions()
    {
        categoryGrid.Children.Clear();

        double gridWidth =
            categoryGrid.ActualWidth;

        if (gridWidth <= 0)
        {
            gridWidth =
                categoryGrid.Width;
        }

        double centerX =
            gridWidth / 2.0;

        for (int i = 0;
             i < categories.Length;
             i++)
        {
            bool isSelected =
                i == selectedCategoryIndex;

            StackPanel categoryPanel =
                new StackPanel
                {
                    Width = 180,
                    Height = 180,
                    Orientation =
                        Orientation.Vertical,

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    VerticalAlignment =
                        VerticalAlignment.Top
                };

            Image icon =
                new Image
                {
                    Width =
                        CategoryIconSize,

                    Height =
                        CategoryIconSize,

                    Stretch =
                        Stretch.Uniform,

                    Opacity =
                        isSelected
                            ? 1.0
                            : 0.55,

                    HorizontalAlignment =
                        HorizontalAlignment.Center
                };

            icon.Source =
                LoadCategoryIcon(
                    categoryIcons[i]);

            TextBlock text =
                new TextBlock
                {
                    Text =
                        categories[i],

                    Foreground =
                        new SolidColorBrush(
                            Color.FromArgb(
                                255,
                                216,
                                238,
                                255)),

                    FontSize =
                        isSelected
                            ? 16
                            : 13,

                    FontWeight =
                        FontWeights.Light,

                    Opacity =
                        isSelected
                            ? 1.0
                            : 0.55,

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    TextAlignment =
                        TextAlignment.Center,

                    Margin =
                        new Thickness(
                            0,
                            4,
                            0,
                            0)
                };

            categoryPanel.Children.Add(
                icon);

            categoryPanel.Children.Add(
                text);

            categoryGrid.Children.Add(
                categoryPanel);

            double offset =
                (i - selectedCategoryIndex)
                * CategorySpacing;

            double x =
                centerX
                -
                (categoryPanel.Width / 2.0)
                +
                offset;

            Canvas.SetLeft(
                categoryPanel,
                x);

            Canvas.SetTop(
                categoryPanel,
                0);
        }
    }

    private BitmapImage? LoadCategoryIcon(
        string fileName)
    {
        try
        {
            Uri resourceUri =
                new Uri(
                    $"pack://application:,,,/Assets/Icons/{fileName}",
                    UriKind.Absolute);

            BitmapImage image =
                new BitmapImage();

            image.BeginInit();

            image.UriSource =
                resourceUri;

            image.CacheOption =
                BitmapCacheOption.OnLoad;

            image.EndInit();

            image.Freeze();

            return image;
        }
        catch
        {
            return null;
        }
    }

    private void UpdateGamePositions(
        bool animate)
    {
        if (selectedCategoryIndex != 0)
        {
            gameCanvas.Visibility =
                Visibility.Collapsed;

            return;
        }

        gameCanvas.Visibility =
            Visibility.Visible;

        EnsureGameTiles();

        for (int i = 0;
             i < games.Count;
             i++)
        {
            GameTile tile =
                gameTiles[i];

            int relativeIndex =
                i - selectedGameIndex;

            double scale;
            double targetY;

            if (relativeIndex == 0)
            {
                scale =
                    SelectedScale;

                targetY =
                    SelectedGameY;
            }
            else if (relativeIndex == -1)
            {
                scale =
                    AdjacentScale;

                targetY =
                    PreviousGameY;
            }
            else if (relativeIndex == 1)
            {
                scale =
                    AdjacentScale;

                targetY =
                    NextGameY;
            }
            else if (relativeIndex < -1)
            {
                scale =
                    FarScale;

                targetY =
                    PreviousGameY
                    +
                    (
                        (relativeIndex + 1)
                        * FarGameSpacing
                    );
            }
            else
            {
                scale =
                    FarScale;

                targetY =
                    NextGameY
                    +
                    (
                        (relativeIndex - 1)
                        * FarGameSpacing
                    );
            }

            double x =
                (gameCanvas.Width -
                 GameTileWidth) / 2.0;

            AnimateTile(
                tile,
                x,
                targetY,
                scale,
                animate);
        }
    }

    private void EnsureGameTiles()
    {
        while (gameTiles.Count < games.Count)
        {
            int index =
                gameTiles.Count;

            GameTile tile =
                new GameTile();

            tile.DataContext =
                games[index];

            tile.RenderTransformOrigin =
                new Point(
                    0.5,
                    0.5);

            TransformGroup transform =
                new TransformGroup();

            ScaleTransform scale =
                new ScaleTransform(
                    1,
                    1);

            TranslateTransform translate =
                new TranslateTransform(
                    0,
                    0);

            transform.Children.Add(
                scale);

            transform.Children.Add(
                translate);

            tile.RenderTransform =
                transform;

            gameTiles.Add(
                tile);

            gameCanvas.Children.Add(
                tile);

            Canvas.SetLeft(
                tile,
                (gameCanvas.Width -
                 GameTileWidth) / 2.0);

            Canvas.SetTop(
                tile,
                SelectedGameY);

            scale.ScaleX = 0;
            scale.ScaleY = 0;
        }

        while (gameTiles.Count > games.Count)
        {
            int lastIndex =
                gameTiles.Count - 1;

            GameTile tile =
                gameTiles[lastIndex];

            gameCanvas.Children.Remove(
                tile);

            gameTiles.RemoveAt(
                lastIndex);
        }

        for (int i = 0;
             i < games.Count;
             i++)
        {
            gameTiles[i].DataContext =
                games[i];
        }
    }

    private void AnimateTile(
        GameTile tile,
        double targetX,
        double targetY,
        double targetScale,
        bool animate)
    {
        Canvas.SetLeft(
            tile,
            targetX);

        if (!animate)
        {
            StopAnimation(tile);

            Canvas.SetTop(
                tile,
                targetY);

            SetTileScale(
                tile,
                targetScale);

            return;
        }

        Duration duration =
            new Duration(
                TimeSpan.FromMilliseconds(
                    GameAnimationMilliseconds));

        CubicEase ease =
            new CubicEase
            {
                EasingMode =
                    EasingMode.EaseOut
            };

        double currentY =
            Canvas.GetTop(tile);

        if (double.IsNaN(currentY))
        {
            currentY =
                targetY;
        }

        DoubleAnimation yAnimation =
            new DoubleAnimation
            {
                From =
                    currentY,

                To =
                    targetY,

                Duration =
                    duration,

                EasingFunction =
                    ease,

                FillBehavior =
                    FillBehavior.Stop
            };

        yAnimation.Completed +=
            (sender, args) =>
            {
                Canvas.SetTop(
                    tile,
                    targetY);
            };

        tile.BeginAnimation(
            Canvas.TopProperty,
            yAnimation);

        TransformGroup? transform =
            tile.RenderTransform
            as TransformGroup;

        if (transform == null)
        {
            return;
        }

        ScaleTransform? scale =
            transform.Children[0]
            as ScaleTransform;

        if (scale == null)
        {
            return;
        }

        scale.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            null);

        scale.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            null);

        double currentScale =
            scale.ScaleX;

        DoubleAnimation scaleXAnimation =
            new DoubleAnimation
            {
                From =
                    currentScale,

                To =
                    targetScale,

                Duration =
                    duration,

                EasingFunction =
                    ease
            };

        DoubleAnimation scaleYAnimation =
            new DoubleAnimation
            {
                From =
                    currentScale,

                To =
                    targetScale,

                Duration =
                    duration,

                EasingFunction =
                    ease
            };

        scale.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            scaleXAnimation);

        scale.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            scaleYAnimation);
    }

    private void StopAnimation(
        GameTile tile)
    {
        tile.BeginAnimation(
            Canvas.TopProperty,
            null);

        TransformGroup? transform =
            tile.RenderTransform
            as TransformGroup;

        if (transform == null)
        {
            return;
        }

        ScaleTransform? scale =
            transform.Children[0]
            as ScaleTransform;

        scale?.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            null);

        scale?.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            null);
    }

    private void SetTileScale(
        GameTile tile,
        double scaleValue)
    {
        TransformGroup? transform =
            tile.RenderTransform
            as TransformGroup;

        if (transform == null)
        {
            return;
        }

        ScaleTransform? scale =
            transform.Children[0]
            as ScaleTransform;

        if (scale == null)
        {
            return;
        }

        scale.ScaleX =
            scaleValue;

        scale.ScaleY =
            scaleValue;
    }

    private void UpdateSelectedGameInfo()
    {
        if (selectedCategoryIndex != 0 ||
            games.Count == 0 ||
            selectedGameIndex < 0 ||
            selectedGameIndex >= games.Count)
        {
            selectedGameText.Text =
                "";

            selectedGameDeveloper.Text =
                "";

            selectedGameGenre.Text =
                "";

            selectedGamePlaytime.Text =
                "";

            selectedGameLaunchCount.Text =
                "";

            selectedGameLastPlayed.Text =
                "";

            return;
        }

        Game selectedGame =
            games[selectedGameIndex];

        selectedGameText.Text =
            selectedGame.Name;

        selectedGameDeveloper.Text =
            string.IsNullOrWhiteSpace(
                selectedGame.Developer)
                ? ""
                : selectedGame.Developer;

        selectedGameGenre.Text =
            string.IsNullOrWhiteSpace(
                selectedGame.Genre)
                ? ""
                : selectedGame.Genre;

        selectedGamePlaytime.Text =
            "PLAYTIME  " +
            FormatPlaytime(
                selectedGame.PlayTimeSeconds);

        selectedGameLaunchCount.Text =
            "LAUNCHES  " +
            selectedGame.LaunchCount;

        selectedGameLastPlayed.Text =
            "LAST PLAYED  " +
            FormatLastPlayed(
                selectedGame.LastPlayed);
    }

    private string FormatPlaytime(
        long seconds)
    {
        if (seconds < 60)
        {
            return "0m";
        }

        TimeSpan playtime =
            TimeSpan.FromSeconds(
                seconds);

        if (playtime.TotalHours >= 1)
        {
            return
                $"{(int)playtime.TotalHours}h " +
                $"{playtime.Minutes:D2}m";
        }

        return
            $"{playtime.Minutes}m";
    }

    private string FormatLastPlayed(
        DateTime? lastPlayed)
    {
        if (!lastPlayed.HasValue)
        {
            return "Never";
        }

        DateTime date =
            lastPlayed.Value.Date;

        DateTime today =
            DateTime.Now.Date;

        if (date == today)
        {
            return "Today";
        }

        if (date ==
            today.AddDays(-1))
        {
            return "Yesterday";
        }

        return
            lastPlayed.Value.ToString(
                "dd MMM yyyy");
    }

    private void LaunchSelectedGame()
    {
        if (games.Count == 0)
        {
            return;
        }

        if (selectedGameIndex < 0 ||
            selectedGameIndex >= games.Count)
        {
            return;
        }

        Game game =
            games[selectedGameIndex];

        if (string.IsNullOrWhiteSpace(
                game.ExecutablePath))
        {
            MessageBox.Show(
                "This game does not have an executable path.",
                "Unable to Launch",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (!File.Exists(
                game.ExecutablePath))
        {
            MessageBox.Show(
                "The game's executable could not be found:\n\n"
                + game.ExecutablePath,
                "Unable to Launch",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
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
                Process.Start(
                    startInfo);

            if (process == null)
            {
                MessageBox.Show(
                    "The game process could not be started.",
                    "Launch Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            game.LaunchCount++;

            game.LastPlayed =
                DateTime.Now;

            libraryService.SaveGames(
                games);

            UpdateSelectedGameInfo();

            process.EnableRaisingEvents =
                true;

            process.Exited +=
                (sender, e) =>
                {
                    RecordPlaytime(
                        process,
                        game);
                };
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "The game could not be launched.\n\n"
                + ex.Message,
                "Launch Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RecordPlaytime(
        Process process,
        Game game)
    {
        try
        {
            DateTime startTime =
                process.StartTime;

            DateTime exitTime =
                process.ExitTime;

            TimeSpan sessionDuration =
                exitTime -
                startTime;

            if (sessionDuration.TotalSeconds < 0)
            {
                return;
            }

            long sessionSeconds =
                (long)
                Math.Round(
                    sessionDuration.TotalSeconds);

            game.PlayTimeSeconds +=
                sessionSeconds;

            libraryService.SaveGames(
                games);

            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    UpdateSelectedGameInfo();
                });
        }
        catch
        {
           
        }
        finally
        {
            process.Dispose();
        }
    }

    private async void ImportGame()
    {
        Microsoft.Win32.OpenFileDialog dialog =
            new Microsoft.Win32.OpenFileDialog
            {
                Title =
                    "Select Game Executable",

                Filter =
                    "Executable Files (*.exe)|*.exe",

                Multiselect =
                    false
            };

        bool? result =
            dialog.ShowDialog();

        if (result != true)
        {
            return;
        }

        try
        {
            GameExecutableInfo executableInfo =
                importService.ReadExecutable(
                    dialog.FileName);

            string gameName =
                !string.IsNullOrWhiteSpace(
                    executableInfo.ProductName)
                    ? executableInfo.ProductName
                    : executableInfo.FileNameWithoutExtension;

            if (string.IsNullOrWhiteSpace(
                    gameName))
            {
                MessageBox.Show(
                    "Could not determine the game name from the executable.",
                    "Import Game",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            Mouse.OverrideCursor =
                Cursors.Wait;

            List<IGDBGameResult> results =
                await igdbService.SearchGamesAsync(
                    gameName);

            Mouse.OverrideCursor =
                null;

            IGDBGameSelectionView selectionView =
                new IGDBGameSelectionView(
                    gameName,
                    results);

            Window? owner =
                Window.GetWindow(
                    gameCanvas);

            if (owner != null)
            {
                selectionView.Owner =
                    owner;
            }

            bool? selectionResult =
                selectionView.ShowDialog();

            if (selectionResult != true ||
                selectionView.SelectedGame == null)
            {
                return;
            }

            IGDBGameResult selectedIGDBGame =
                selectionView.SelectedGame;

            Game game =
                new Game
                {
                    Name =
                        selectedIGDBGame.Name,

                    Developer =
                        selectedIGDBGame.Developer,

                    Publisher =
                        selectedIGDBGame.Publisher,

                    Genre =
                        selectedIGDBGame.Genre,

                    ReleaseDate =
                        selectedIGDBGame.FirstReleaseDate,

                    Description =
                        selectedIGDBGame.Summary,

                    ExecutablePath =
                        executableInfo.ExecutablePath,

                    InstallDirectory =
                        executableInfo.Directory,

                    CoverPath =
                        selectedIGDBGame.CoverUrl,

                    BackgroundPath =
                        selectedIGDBGame.BackgroundUrl,

                    IGDBId =
                        selectedIGDBGame.Id.ToString(),

                    IsImported =
                        true,

                    PlayTimeSeconds =
                        0,

                    LastPlayed =
                        null,

                    LaunchCount =
                        0
                };

            libraryService.AddGame(
                game,
                games);

            selectedGameIndex =
                games.Count - 1;

            RefreshLayout(true);

            MessageBox.Show(
                $"'{game.Name}' was added to your library using IGDB metadata.",
                "Game Imported",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Mouse.OverrideCursor =
                null;

            MessageBox.Show(
                "The game could not be imported.\n\n"
                + ex.Message,
                "Import Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RemoveSelectedGame()
    {
        if (games.Count == 0)
        {
            return;
        }

        if (selectedGameIndex < 0 ||
            selectedGameIndex >= games.Count)
        {
            return;
        }

        Game game =
            games[selectedGameIndex];

        MessageBoxResult result =
            MessageBox.Show(
                $"Remove '{game.Name}' from your library?",
                "Remove Game",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        libraryService.RemoveGame(
            game,
            games);

        if (selectedGameIndex >= games.Count)
        {
            selectedGameIndex =
                Math.Max(
                    0,
                    games.Count - 1);
        }

        RefreshLayout(true);
    }
}