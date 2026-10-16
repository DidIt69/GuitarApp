#region Imports
using GuitarApp.Core.ViewModels;
#endregion

namespace GuitarApp.Maui.Controls;

public abstract class PageBase : ContentPage
{
    #region Constructors
    /// <summary>
    /// 
    /// </summary>
    protected PageBase() { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="viewModel"></param>
    protected PageBase(ViewModelBase viewModel) 
    {
        BindingContext = viewModel; 
    }
    #endregion

    #region Override Methods
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ViewModelBase vm)
        {
            await vm.InitializeAsync(null);
        }
    }
    #endregion
}
