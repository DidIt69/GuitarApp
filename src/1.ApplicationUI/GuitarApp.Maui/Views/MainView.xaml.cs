using GuitarApp.Maui.Controls;
using GuitarApp.Core.ViewModels;
using System.ComponentModel;
using Microsoft.Maui.ApplicationModel;

namespace GuitarApp.Maui.Views;

public partial class MainView : PageBase
{
    public MainView(MainViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();

        // On s'abonne aux changements de propriétés pour forcer le rafraîchissement des canevas
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MainViewModel vm && e.PropertyName == nameof(MainViewModel.WaveformBuffer))
        {
            // Forcer le rafraîchissement graphique de l'oscilloscope sur le thread principal
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Invalidate() demande au système d'exploitation de redessiner immédiatement le canevas
                WaveformCanvas?.Invalidate();
            });
        }
    }
}
