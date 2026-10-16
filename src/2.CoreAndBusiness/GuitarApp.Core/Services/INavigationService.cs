// Dans GuitarApp.Core / Services / INavigationService.cs
using GuitarApp.Core.ViewModels;

namespace GuitarApp.Core.Services;

public interface INavigationService
{
    Task NavigateToAsync<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;
    Task GoBackAsync();
}
