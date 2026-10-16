using Microsoft.Maui.Controls;
using GuitarApp.Core.ViewModels;

namespace GuitarApp.Maui.Controls;

public abstract class PageBase : ContentPage
{
    protected PageBase() { }
    protected PageBase(ViewModelBase viewModel) { BindingContext = viewModel; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ViewModelBase vm) await vm.InitializeAsync(null);
    }
}
