using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using XMBLauncher.Services;
using XMBLauncher.ViewModels;

namespace XMBLauncher.Views;

public partial class XmbView : UserControl
{
    private readonly XmbViewModel viewModel;

    private readonly DualSenseControllerService dualSenseController;

    private int previousCategoryIndex = 0;

    public XmbView()
    {
        InitializeComponent();

        viewModel =
            new XmbViewModel(
                GameCanvas,
                SelectedGameText,
                SelectedGameDeveloper,
                SelectedGameGenre,
                SelectedGamePlaytime,
                SelectedGameLaunchCount,
                SelectedGameLastPlayed,
                CategoryGrid);

        DataContext = viewModel;

        dualSenseController =
            new DualSenseControllerService();

        dualSenseController.ActionTriggered +=
            DualSenseController_ActionTriggered;
    }

    private void XmbView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        Focusable = true;
        Focus();

        try
        {
            string videoPath =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Assets",
                    "Images",
                    "Background.mp4");

            if (File.Exists(videoPath))
            {
                BackgroundVideo.Source =
                    new Uri(
                        videoPath,
                        UriKind.Absolute);

                BackgroundVideo.Play();
            }
        }
        catch
        {
        }

        dualSenseController.Start();

        Dispatcher.BeginInvoke(
            new Action(() =>
            {
                viewModel.RefreshLayout();

                previousCategoryIndex =
                    viewModel.SelectedCategoryIndex;

                UpdateCategoryVisuals(false);
                UpdateGameColumnVisuals(false);

                Focus();
            }),
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void DualSenseController_ActionTriggered(
        object? sender,
        DualSenseAction action)
    {
        Dispatcher.Invoke(
            () =>
            {
                Key key =
                    action switch
                    {
                        DualSenseAction.Up =>
                            Key.Up,

                        DualSenseAction.Down =>
                            Key.Down,

                        DualSenseAction.Left =>
                            Key.Left,

                        DualSenseAction.Right =>
                            Key.Right,

                        DualSenseAction.Launch =>
                            Key.Enter,

                        DualSenseAction.Back =>
                            Key.Escape,

                        DualSenseAction.Import =>
                            Key.I,

                        DualSenseAction.Remove =>
                            Key.Delete,

                        _ =>
                            Key.None
                    };

                if (key == Key.None)
                {
                    return;
                }

                HandleNavigationKey(key);
            });
    }

    private void HandleNavigationKey(
        Key key)
    {
        int oldCategoryIndex =
            viewModel.SelectedCategoryIndex;

        viewModel.HandleKey(
            key);

        int newCategoryIndex =
            viewModel.SelectedCategoryIndex;

        if (oldCategoryIndex != newCategoryIndex)
        {
            int direction;

            if (newCategoryIndex >
                oldCategoryIndex)
            {
                direction = 1;
            }
            else
            {
                direction = -1;
            }

            UpdateCategoryVisuals(
                true,
                direction);

            UpdateGameColumnVisuals(
                true);
        }

        previousCategoryIndex =
            newCategoryIndex;
    }

    private void BackgroundVideo_MediaOpened(
        object sender,
        RoutedEventArgs e)
    {
        BackgroundVideo.Play();
    }

    private void BackgroundVideo_MediaFailed(
        object sender,
        ExceptionRoutedEventArgs e)
    {
        MessageBox.Show(
            $"Background video failed to load:\n\n{e.ErrorException?.Message}",
            "Video Error");
    }

    private void BackgroundVideo_MediaEnded(
        object sender,
        RoutedEventArgs e)
    {
        BackgroundVideo.Position =
            TimeSpan.Zero;

        BackgroundVideo.Play();
    }

    private void XmbView_KeyDown(
        object sender,
        KeyEventArgs e)
    {
        HandleNavigationKey(
            e.Key);

        e.Handled = true;
    }

    private void UpdateCategoryVisuals(
        bool animate,
        int direction = 0)
    {
        if (CategoryGridTranslate == null ||
            CategoryGridScale == null)
        {
            return;
        }

        CategoryGridTranslate.BeginAnimation(
            TranslateTransform.XProperty,
            null);

        CategoryGridTranslate.BeginAnimation(
            TranslateTransform.YProperty,
            null);

        CategoryGridScale.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            null);

        CategoryGridScale.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            null);

        CategoryGrid.BeginAnimation(
            UIElement.OpacityProperty,
            null);

        if (!animate)
        {
            CategoryGridTranslate.X = 0;
            CategoryGridTranslate.Y = 0;

            CategoryGridScale.ScaleX = 1.0;
            CategoryGridScale.ScaleY = 1.0;

            CategoryGrid.Opacity = 1.0;

            return;
        }

        double startingOffset;

        if (direction > 0)
        {
            startingOffset = 55;
        }
        else
        {
            startingOffset = -55;
        }

        CategoryGridTranslate.X =
            startingOffset;

        CategoryGridTranslate.Y =
            10;

        CategoryGridScale.ScaleX =
            0.92;

        CategoryGridScale.ScaleY =
            0.92;

        CategoryGrid.Opacity =
            0.0;

        Duration duration =
            new Duration(
                TimeSpan.FromMilliseconds(280));

        CubicEase ease =
            new CubicEase
            {
                EasingMode =
                    EasingMode.EaseOut
            };

        DoubleAnimation slideAnimation =
            new DoubleAnimation
            {
                From =
                    startingOffset,

                To =
                    0,

                Duration =
                    duration,

                EasingFunction =
                    ease
            };

        CategoryGridTranslate.BeginAnimation(
            TranslateTransform.XProperty,
            slideAnimation);

        DoubleAnimation verticalAnimation =
            new DoubleAnimation
            {
                From = 10,
                To = 0,
                Duration = duration,
                EasingFunction = ease
            };

        CategoryGridTranslate.BeginAnimation(
            TranslateTransform.YProperty,
            verticalAnimation);

        DoubleAnimation scaleXAnimation =
            new DoubleAnimation
            {
                From = 0.92,
                To = 1.0,
                Duration = duration,
                EasingFunction = ease
            };

        CategoryGridScale.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            scaleXAnimation);

        DoubleAnimation scaleYAnimation =
            new DoubleAnimation
            {
                From = 0.92,
                To = 1.0,
                Duration = duration,
                EasingFunction = ease
            };

        CategoryGridScale.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            scaleYAnimation);

        DoubleAnimation opacityAnimation =
            new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration =
                    new Duration(
                        TimeSpan.FromMilliseconds(220)),
                EasingFunction = ease
            };

        CategoryGrid.BeginAnimation(
            UIElement.OpacityProperty,
            opacityAnimation);
    }

    private void UpdateGameColumnVisuals(
        bool animate)
    {
        int categoryIndex =
            viewModel.SelectedCategoryIndex;

        bool isGamesCategory =
            categoryIndex == 0;

        if (!isGamesCategory)
        {
            GameCanvas.Visibility =
                Visibility.Collapsed;

            GameCanvas.Opacity =
                1.0;

            SelectedGameInfo.Visibility =
                Visibility.Collapsed;

            if (GameCanvasTranslate != null)
            {
                GameCanvasTranslate.BeginAnimation(
                    TranslateTransform.XProperty,
                    null);

                GameCanvasTranslate.BeginAnimation(
                    TranslateTransform.YProperty,
                    null);

                GameCanvasTranslate.X = 0;
                GameCanvasTranslate.Y = 0;
            }

            return;
        }

        GameCanvas.Visibility =
            Visibility.Visible;

        GameCanvas.Opacity =
            1.0;

        SelectedGameInfo.Visibility =
            Visibility.Visible;

        viewModel.RefreshLayout();

        if (GameCanvasTranslate == null)
        {
            return;
        }

        if (!animate)
        {
            GameCanvasTranslate.BeginAnimation(
                TranslateTransform.XProperty,
                null);

            GameCanvasTranslate.BeginAnimation(
                TranslateTransform.YProperty,
                null);

            GameCanvasTranslate.X = 0;
            GameCanvasTranslate.Y = 0;

            ApplyGamePopAnimation(false);

            return;
        }

        Duration slideDuration =
            new Duration(
                TimeSpan.FromMilliseconds(250));

        CubicEase slideEase =
            new CubicEase
            {
                EasingMode =
                    EasingMode.EaseOut
            };

        DoubleAnimation slideAnimation =
            new DoubleAnimation
            {
                From =
                    GameCanvasTranslate.X,

                To =
                    0,

                Duration =
                    slideDuration,

                EasingFunction =
                    slideEase
            };

        GameCanvasTranslate.BeginAnimation(
            TranslateTransform.XProperty,
            slideAnimation);

        ApplyGamePopAnimation(true);
    }

    private void ApplyGamePopAnimation(
        bool animate)
    {
        if (GameCanvasScale == null)
        {
            return;
        }

        GameCanvasScale.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            null);

        GameCanvasScale.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            null);

        if (!animate)
        {
            GameCanvasScale.ScaleX = 1.0;
            GameCanvasScale.ScaleY = 1.0;

            return;
        }

        GameCanvasScale.ScaleX = 0.88;
        GameCanvasScale.ScaleY = 0.88;

        Duration popDuration =
            new Duration(
                TimeSpan.FromMilliseconds(300));

        CubicEase popEase =
            new CubicEase
            {
                EasingMode =
                    EasingMode.EaseOut
            };

        DoubleAnimation scaleXAnimation =
            new DoubleAnimation
            {
                From = 0.88,
                To = 1.0,
                Duration = popDuration,
                EasingFunction = popEase
            };

        DoubleAnimation scaleYAnimation =
            new DoubleAnimation
            {
                From = 0.88,
                To = 1.0,
                Duration = popDuration,
                EasingFunction = popEase
            };

        GameCanvasScale.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            scaleXAnimation);

        GameCanvasScale.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            scaleYAnimation);
    }

    public void StopController()
    {
        dualSenseController.Stop();
    }

    private void UserControl_Unloaded(
        object sender,
        RoutedEventArgs e)
    {
        dualSenseController.Stop();
    }

    protected override void OnInitialized(
        EventArgs e)
    {
        base.OnInitialized(e);

        Unloaded +=
            UserControl_Unloaded;
    }

    protected override void OnVisualParentChanged(
        DependencyObject? oldParent)
    {
        base.OnVisualParentChanged(
            oldParent);

        if (VisualParent == null)
        {
            dualSenseController.Stop();
        }
    }
}