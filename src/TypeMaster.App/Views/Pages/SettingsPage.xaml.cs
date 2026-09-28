using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.App;
using TypeMaster.ViewModels;

namespace TypeMaster.App.Views.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<SettingsViewModel>();
    }
}
