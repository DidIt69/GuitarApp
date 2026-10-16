#region Imports
using CommunityToolkit.Mvvm.ComponentModel;
#endregion

namespace GuitarApp.Core.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Obtient ou définit si le viewmodel est occupé
    /// </summary>
    [ObservableProperty] private bool _isBusy;

    /// <summary>
    /// Obtient ou définit le titre du viewmodel
    /// </summary>
    [ObservableProperty] private string _title = string.Empty;

    /// <summary>
    /// Méthode permettant d'initialiser le viewmodel
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public virtual Task InitializeAsync(object? parameter) => Task.CompletedTask;
}
