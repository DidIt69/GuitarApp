using GuitarApp.Maui.Controls;
using GuitarApp.Core.ViewModels;

namespace GuitarApp.Maui.Views;

public partial class HistoryView : PageBase
{
    public HistoryView(HistoryViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
