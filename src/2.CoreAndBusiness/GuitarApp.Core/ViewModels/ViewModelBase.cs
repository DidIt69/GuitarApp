using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace GuitarApp.Core.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _title = string.Empty;
    public virtual Task InitializeAsync(object? parameter) => Task.CompletedTask;
}
