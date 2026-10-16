// Dans GuitarApp.Maui / Services / NavigationService.cs
using GuitarApp.Core.Services;
using GuitarApp.Core.ViewModels;
using GuitarApp.Maui.Views;
using System;
using System.Threading.Tasks;

namespace GuitarApp.Maui.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task NavigateToAsync<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase
    {
        // 1. Détermination de la vue correspondante au ViewModel demandé
        Page page = TypeOfViewModelToPage(typeof(TViewModel));

        // 2. Initialisation asynchrone du ViewModel avec passage de paramètre optionnel
        if (page.BindingContext is ViewModelBase viewModel)
        {
            await viewModel.InitializeAsync(parameter);
        }

        // 3. Exécution de la navigation native MAUI
        if (Application.Current?.MainPage is NavigationPage navigationPage)
        {
            await navigationPage.PushAsync(page);
        }
    }

    public async Task GoBackAsync()
    {
        if (Application.Current?.MainPage is NavigationPage navigationPage)
        {
            await navigationPage.PopAsync();
        }
    }

    /// <summary>
    /// Dictionnaire de correspondance IoC (Mapping) entre les ViewModels et les Vues physiques
    /// </summary>
    private Page TypeOfViewModelToPage(Type viewModelType)
    {
        if (viewModelType == typeof(MainViewModel))
            return (Page)_serviceProvider.GetRequiredService<MainView>();

        if (viewModelType == typeof(HistoryViewModel))
            return (Page)_serviceProvider.GetRequiredService<HistoryView>();

        throw new InvalidOperationException($"Aucune vue graphique n'est associée au ViewModel {viewModelType.Name}.");
    }
}
