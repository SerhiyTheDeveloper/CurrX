using CurrX.Infrastructure;
using CurrX.Models;
using CurrX.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CurrX
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RateEntry? _currentRateEntry;
        private readonly RateDatabaseService _databaseService;
        private readonly string _databasePath;
        private int? _selectedSavedId;
        private readonly string _sourceUrl = "https://bank.gov.ua/NBUStatService/v1/statdirectory/exchangenew?json";
        public MainWindow()
        {
            InitializeComponent();

            SetupComponents();

            _databasePath = AppPaths.GetDatabasePath();
            _databaseService = new RateDatabaseService(_databasePath);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetStatus("Підготовка бази даних...");
            try
            {
                await _databaseService.InitializeAsync();
                await RefreshSavedItemsAsync();

                SetBusy(false);
                SetStatus($"База даних готова");
            }
            catch (SqliteException ex)
            {
                SetStatus("Не вдалося створити або відкрити базу даних.", isError: true);
                System.Diagnostics.Debug.WriteLine(ex);
            }
            catch (Exception ex)
            {
                SetStatus("Сталася помилка під час підготовки бази даних.", isError: true);
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async Task RefreshSavedItemsAsync()
        {
            List<SavedRateEntry> items = await _databaseService.GetAllAsync();

            savedGrid.ItemsSource = null;
            savedGrid.ItemsSource = items;
            savedGrid.SelectedItem = null;

            ClearSavedSelection();
            countLabel.Text = $"{items.Count}";
        }

        private void ClearSavedSelection()
        {
            _selectedSavedId = null;
            notesTextBox.Clear();
            notesTextBox.IsEnabled = false;
            updateButton.IsEnabled = false;
            deleteButton.IsEnabled = false;
        }

        private void SetupComponents()
        {
            currencyCombo.Items.Add(new CurrencyItem { Name = "Долар США", Code = "USD" });
            currencyCombo.Items.Add(new CurrencyItem { Name = "Євро", Code = "EUR" });
            currencyCombo.Items.Add(new CurrencyItem { Name = "Злотий", Code = "PLN" });
            currencyCombo.Items.Add(new CurrencyItem { Name = "Фунт стерлінгів", Code = "GBP" });
            currencyCombo.Items.Add(new CurrencyItem { Name = "Чеська крона", Code = "CZK" });
            currencyCombo.SelectedIndex = 0;
        }

        private void SetStatus(string text, bool isError = false)
        {
            statusLabel.Text = text;
            statusBorder.Background = (Brush)FindResource(isError ? "DangerSoftBrush" : "AccentSoftBrush");
            statusLabel.Foreground = (Brush)FindResource(isError ? "DangerBrush" : "AccentDarkBrush");
        }

        private void SetBusy(bool isBusy)
        {
            bool hasCurrentReading = _currentRateEntry is not null;
            bool hasSelectedSavedItem = _selectedSavedId is not null;
            loadButton.IsEnabled = !isBusy;
            openButton.IsEnabled = !isBusy;
            saveButton.IsEnabled = !isBusy && hasCurrentReading;
            saveToDatabaseButton.IsEnabled = !isBusy && hasCurrentReading;
            refreshDatabaseButton.IsEnabled = !isBusy;
            savedGrid.IsEnabled = !isBusy;
            notesTextBox.IsEnabled = !isBusy && hasSelectedSavedItem;
            updateButton.IsEnabled = !isBusy && hasSelectedSavedItem;
            deleteButton.IsEnabled = !isBusy && hasSelectedSavedItem;
        }

        private async void loadButton_Click(object sender, RoutedEventArgs e)
        {
            SetBusy(true);
            SetStatus("Завантаження...");

            try
            {
                if (currencyCombo.SelectedItem is CurrencyItem selectedCurrency)
                {
                    string url = $"https://bank.gov.ua/NBUStatService/v1/statdirectory/exchangenew?json&valcode={selectedCurrency.Code}&date={DateTime.Now:yyyyMMdd}";
                    _currentRateEntry = await RateService.LoadRateAsync(url);
                    ShowRateEntry(_currentRateEntry);
                    SetStatus("Завантаження завершено.");
                }
            }
            catch (HttpRequestException)
            {
                SetStatus("Не вдалося з’єднатися із сервером.", isError: true);
            }
            catch (TaskCanceledException)
            {
                SetStatus("Сервер не відповів за відведений час.", isError: true);
            }
            catch (JsonException)
            {
                SetStatus("Сервер повернув дані неочікуваного формату.", isError: true);
            }
            catch (Exception ex)
            {
                SetStatus("Сталася непередбачена помилка.", isError: true);
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ShowRateEntry(RateEntry rateEntry)
        {
            nameLabel.Text = rateEntry.Currency;
            rateLabel.Text = rateEntry.Rate.ToString();
            codeLabel.Text = rateEntry.Code;
            dateLabel.Text = rateEntry.ExchangeDate;
        }

        private async void saveButton_Click(object sender, RoutedEventArgs e) 
        {
            if (_currentRateEntry is null)
            {
                SetStatus("Спочатку завантажте або відкрийте дані.");
                return;
            }

            SaveFileDialog dialog = new()
            {
                Title = "Зберегти дані про погоду",
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json",
                AddExtension = true,
                FileName = $"weather-{DateTime.Now:yyyy-MM-dd}.json"
            };

            if (dialog.ShowDialog() != true) 
            {
                SetStatus("Збереження скасовано.");
                return;
            }

            SetBusy(true);
            SetStatus("Збереження файла...");

            try
            {
                RateEntrySnapshot snapshot = new()
                {
                    DataSource = "Національний банк України",
                    SourceUrl = _sourceUrl,
                    FormatVersion = "1.0",
                    SavedAt = DateTimeOffset.Now,
                    RateEntry = _currentRateEntry
                };
                await JsonFileService.SaveAsync(
                dialog.FileName, snapshot);
                SetStatus($"Файл збережено: {Path.GetFileName(dialog.FileName)}");
            }
            catch (UnauthorizedAccessException)
            {
                SetStatus("Немає дозволу на запис у вибране місце.", isError: true);
            }
            catch (IOException)
            {
                SetStatus("Не вдалося записати файл. Можливо, він уже відкритий.", isError: true);
            }
            catch (Exception ex)
            {
                SetStatus("Не вдалося зберегти файл.", isError: true);
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void openButton_Click(object sender, RoutedEventArgs e) 
        {
            OpenFileDialog dialog = new()
            {
                Title = "Відкрити дані про погоду",
                Filter = "JSON files (*.json)|*.json",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
            {
                SetStatus("Відкриття скасовано.");
                return;
            }

            SetBusy(true);
            SetStatus("Відкриття файла...");

            try
            {
                RateEntrySnapshot snapshot = await JsonFileService
                    .LoadAsync<RateEntrySnapshot>(dialog.FileName);
                if (snapshot.FormatVersion != "1.0")
                {
                    SetStatus("Файл має невідому версію формату.", isError: true);
                    return;
                }
                RateEntry? entry = snapshot.RateEntry ?? throw new JsonException(
                    "У файлі відсутній об’єкт RateEntry.");
                _currentRateEntry = entry;
                ShowRateEntry(entry);

                string localSavedTime = snapshot.SavedAt
                .ToLocalTime()
                .ToString("g");
                statusLabel.Text =
                $"Відкрито {Path.GetFileName(dialog.FileName)}. " +
                $"Збережено: {localSavedTime}.";
            }
            catch (JsonException)
            {
                SetStatus("Файл не є коректним файлом даних цього застосунку.", isError: true);
            }
            catch (UnauthorizedAccessException)
            {
                SetStatus("Немає дозволу на читання вибраного файла.", isError: true);
            }
            catch (IOException)
            {
                SetStatus("Не вдалося прочитати файл.", isError: true);
            }
            catch (Exception ex)
            {
                SetStatus("Не вдалося відкрити файл.", isError: true);
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void saveToDatabaseButton_Click(object sender, RoutedEventArgs e) 
        {
            if (_currentRateEntry is null)
            {
                SetStatus("Спочатку завантажте або відкрийте дані.");
                return;
            }
            if (await _databaseService.ExistsAsync(_currentRateEntry.Code, _currentRateEntry.ExchangeDate))
            {
                SetStatus("Такий запис уже збережено.", isError: true);
                return;
            }
            SavedRateEntry entity = new()
            {
                Currency = _currentRateEntry.Currency,
                Rate = _currentRateEntry.Rate,
                Code = _currentRateEntry.Code,
                ExchangeDate = _currentRateEntry.ExchangeDate,
                SourceUrl = _sourceUrl,
                SavedAtUtc = DateTime.UtcNow,
                Notes = "",
            };
            SetBusy(true);
            SetStatus("Збереження в базі даних...");
            try
            {
                int newId = await _databaseService.AddAsync(entity);
                await RefreshSavedItemsAsync();
                SetStatus($"Запис збережено в базі. Id = {newId}.");
            }
            catch (DbUpdateException ex)
            {
                SetStatus("Не вдалося зберегти запис у базі даних.", isError: true);
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void refreshDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            SetBusy(true);
            SetStatus("Оновлення списку...");
            try
            {
                await RefreshSavedItemsAsync();
                SetStatus("Список оновлено.");
            }
            catch (SqliteException ex)
            {
                SetStatus("Не вдалося прочитати базу даних.", isError: true);
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void savedGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) 
        {
            if (savedGrid.SelectedItem is not SavedRateEntry item)
            {
                ClearSavedSelection();
                return;
            }
            _selectedSavedId = item.Id;
            notesTextBox.Text = item.Notes;
            notesTextBox.IsEnabled = true;
            updateButton.IsEnabled = true;
            deleteButton.IsEnabled = true;
            statusLabel.Text =
            $"Вибрано запис Id = {item.Id}, валюта: {item.Currency}.";
        }

        private async void updateButton_Click(object sender, RoutedEventArgs e) 
        {
            if (_selectedSavedId is null)
            {
                SetStatus("Оберіть запис у таблиці.", isError: true);
                return;
            }
            string notes = notesTextBox.Text.Trim();
            if (notes.Length > 500)
            {
                SetStatus("Примітка не може перевищувати 500 символів.", isError: true);
                return;
            }
            int id = _selectedSavedId.Value;

            SetBusy(true);
            SetStatus("Збереження змін...");
            try
            {
                bool updated = await _databaseService.UpdateAsync(id, notes);
                if (!updated)
                {
                    SetStatus("Запис уже не існує.", isError: true);
                    await RefreshSavedItemsAsync();
                    return;
                }
                await RefreshSavedItemsAsync();
                SetStatus($"Запис Id = {id} оновлено.");
            }
            catch (DbUpdateException ex)
            {
                SetStatus("Не вдалося оновити запис.", isError: true);
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void deleteButton_Click(object sender, RoutedEventArgs e) 
        {
            if (_selectedSavedId is null)
            {
                SetStatus("Оберіть запис для видалення.", isError: true);
                return;
            }
            int id = _selectedSavedId.Value;
            MessageBoxResult answer = MessageBox.Show(
                $"Видалити запис Id = {id}?",
                "Підтвердження",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer != MessageBoxResult.Yes)
            {
                SetStatus("Видалення скасовано.");
                return;
            }

            SetBusy(true);
            SetStatus("Видалення запису...");
            try
            {
                bool deleted = await _databaseService.DeleteAsync(id);
                if (!deleted)
                {
                    SetStatus("Запис уже не існує.", isError: true);
                    await RefreshSavedItemsAsync();
                    return;
                }
                await RefreshSavedItemsAsync();
                SetStatus($"Запис Id = {id} видалено.");
            }
            catch (DbUpdateException ex)
            {
                SetStatus("Не вдалося видалити запис.", isError: true);
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }
    }
}