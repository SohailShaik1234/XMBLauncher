using Microsoft.Win32;
using System;
using System.Windows;
using XMBLauncher.Models;
using XMBLauncher.Services;

namespace XMBLauncher.Views;

public partial class ImportGameView : Window
{
    private readonly GameImportService importService;

    public GameExecutableInfo? ExecutableInfo { get; private set; }

    public ImportGameView()
    {
        InitializeComponent();

        importService =
            new GameImportService();
    }


    private void BrowseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenFileDialog dialog =
            new OpenFileDialog
            {
                Title = "Select Game Executable",

                Filter =
                    "Game Executable (*.exe)|*.exe",

                Multiselect = false
            };


        bool? result =
            dialog.ShowDialog();


        if (result != true)
            return;


        try
        {
            ExecutableInfo =
                importService.ReadExecutable(
                    dialog.FileName);


            DisplayExecutableInfo(
                ExecutableInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Import Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    private void DisplayExecutableInfo(
        GameExecutableInfo info)
    {
        ExecutablePathTextBox.Text =
            info.ExecutablePath;

        ProductNameText.Text =
            string.IsNullOrWhiteSpace(
                info.ProductName)
                ? "Unknown"
                : info.ProductName;

        CompanyNameText.Text =
            string.IsNullOrWhiteSpace(
                info.CompanyName)
                ? "Unknown"
                : info.CompanyName;

        VersionText.Text =
            string.IsNullOrWhiteSpace(
                info.FileVersion)
                ? "Unknown"
                : info.FileVersion;

        DescriptionText.Text =
            string.IsNullOrWhiteSpace(
                info.Description)
                ? "Unknown"
                : info.Description;
    }


    private void ContinueButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ExecutableInfo == null)
        {
            MessageBox.Show(
                "Please select a game executable first.",
                "Import Game",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }


        DialogResult = true;

        Close();
    }


    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;

        Close();
    }
}