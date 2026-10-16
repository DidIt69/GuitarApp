using GuitarApp.Maui.Views;
namespace GuitarApp.Maui;
public partial class App : Application {
    public App(MainView mainView) {
        InitializeComponent();
        MainPage = new NavigationPage(mainView);
    }
}
