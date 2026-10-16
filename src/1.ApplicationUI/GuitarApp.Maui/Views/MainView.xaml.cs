#region Imports
using GuitarApp.Maui.Controls;
using GuitarApp.Core.ViewModels;
using System.ComponentModel;
#endregion

namespace GuitarApp.Maui.Views;

public partial class MainView : PageBase
{
    public MainView(MainViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Force l'invalidation du canevas de l'oscilloscope sur le thread UI principal de WinUI3
        if (sender is MainViewModel && e.PropertyName == nameof(MainViewModel.WaveformBuffer))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WaveformCanvas?.Invalidate();
            });
        }
    }
}
