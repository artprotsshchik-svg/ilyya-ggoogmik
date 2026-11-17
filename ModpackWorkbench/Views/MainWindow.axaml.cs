using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ModpackWorkbench.Models;
using ModpackWorkbench.ViewModels;

namespace ModpackWorkbench.Views;

public partial class MainWindow : Window
{
    public static readonly IValueConverter TabBorderConverter = new TabActiveBorderConverter();

    private sealed class TabActiveBorderConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is true
                ? new SolidColorBrush(Color.Parse("#60A5FA"))
                : Brushes.Transparent;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Avalonia.Data.BindingOperations.DoNothing;
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        vm.OpenModDetailRequested += OnOpenModDetailRequested;
        vm.PickFolderAsync = PickFolderAsync;
        vm.CopyToClipboardAsync = CopyToClipboardAsync;
    }

    private async void OnOpenModDetailRequested(ModItem mod)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        var detailVm = vm.CreateModDetailViewModel(mod);
        var dlg = new ModDetailWindow
        {
            DataContext = detailVm
        };
        detailVm.CloseRequested += () => dlg.Close();
        await dlg.ShowDialog(this);
    }

    private async Task<string?> PickFolderAsync()
    {
        var topLevel = GetTopLevel(this);
        if (topLevel?.StorageProvider is null) return null;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Выберите папку mods",
            AllowMultiple = false
        });

        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

    private async Task CopyToClipboardAsync(string text)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel?.Clipboard is not null)
        {
            await topLevel.Clipboard.SetTextAsync(text);
        }
    }
}
