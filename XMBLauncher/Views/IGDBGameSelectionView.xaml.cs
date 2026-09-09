using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using XMBLauncher.Models;
using XMBLauncher.Services;

namespace XMBLauncher.Views;

public partial class IGDBGameSelectionView : Window
{
    private readonly IGDBService igdbService;

    private readonly string originalSearchTerm;


    public IGDBGameResult? SelectedGame { get; private set; }


    public IGDBGameSelectionView(
        string searchTerm,
        List<IGDBGameResult> results)
    {
        InitializeComponent();


        originalSearchTerm =
            searchTerm;


        igdbService =
            new IGDBService(
                App.IGDBSettings.ClientId,
                App.IGDBSettings.ClientSecret);


        SearchText.Text =
            $"Search results for \"{searchTerm}\"";


        ResultCountText.Text =
            $"{results.Count} game(s) found";


        ResultsList.ItemsSource =
            results;


        ManualSearchTextBox.Text =
            searchTerm;
    }


    private async void SearchButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string searchTerm =
            ManualSearchTextBox.Text.Trim();


        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            MessageBox.Show(
                "Please enter a game name to search for.",
                "Search IGDB",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }


        await SearchGames(searchTerm);
    }


    private async void ManualSearchTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
       
    }


    private async Task SearchGames(
        string searchTerm)
    {
        try
        {
            SearchText.Text =
                $"Searching IGDB for \"{searchTerm}\"...";


            ResultCountText.Text =
                "Searching...";


            ResultsList.ItemsSource =
                null;


            List<IGDBGameResult> results =
                await igdbService.SearchGamesAsync(
                    searchTerm);


            SearchText.Text =
                $"Search results for \"{searchTerm}\"";


            ResultCountText.Text =
                $"{results.Count} game(s) found";


            ResultsList.ItemsSource =
                results;


            if (results.Count == 0)
            {
                MessageBox.Show(
                    $"No games were found on IGDB for:\n\n" +
                    $"{searchTerm}\n\n" +
                    "Try another search term.",
                    "No Results",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            SearchText.Text =
                $"Search failed for \"{searchTerm}\"";


            ResultCountText.Text =
                "Unable to retrieve results";


            MessageBox.Show(
                $"Could not search IGDB.\n\n{ex.Message}",
                "IGDB Search Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    private void SelectButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;


        if (button.DataContext
            is not IGDBGameResult game)
        {
            return;
        }


        SelectedGame =
            game;


        DialogResult = true;

        Close();
    }


    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SelectedGame = null;

        DialogResult = false;

        Close();
    }
}