using System.Windows;
using NetSpeedMeter.ViewModels;

namespace NetSpeedMeter.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(SettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
