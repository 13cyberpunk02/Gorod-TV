
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GorodTV.Core.Models;
using GorodTV.Core.Services;
using System.Collections.ObjectModel;

namespace GorodTV.Core.ViewModels;

public partial class CategoriesViewModel : ObservableObject
{
    private readonly IDialogService _dialogs;
    private readonly IGorodTvService _channels;

    public CategoriesViewModel(IDialogService dialogs, IGorodTvService channels)
    {
        _dialogs = dialogs;
        _channels = channels;
    }

    public ObservableCollection<CategoryItem> Categories { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var items = await _channels.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in items)
                Categories.Add(c);
        }
        catch (Exception)
        {
            await _dialogs.AlertAsync("Не удалось загрузить",
                "Проверьте подключение к интернету и попробуйте ещё раз.",
                AlertKind.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        // TODO: живой поиск каналов и программ
    }

    [RelayCommand]
    private async Task OpenCategoryAsync(CategoryItem item)
        => await Shell.Current.GoToAsync($"channellist?category={item.Id}&title={Uri.EscapeDataString(item.Title)}");

    [RelayCommand]
    private Task OpenFiltersAsync() => Task.CompletedTask;

    [RelayCommand]
    private async Task OpenProfileAsync()
    {
        bool confirm = await _dialogs.ConfirmAsync(
            "Выход из кабинета",
            "Выйти? Потребуется снова ввести номер договора и пароль.",
            accept: "Выйти",
            cancel: "Отмена",
            kind: AlertKind.Error);

        if (confirm)
        {
            _channels.Logout();                        // чистит сессию
            await Shell.Current.GoToAsync("//login");
        }
    }
}
