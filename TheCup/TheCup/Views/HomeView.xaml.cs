using System.Windows.Controls;
using System.Windows.Input;
using TheCup_Application.ViewModels;

namespace TheCup_Presentation.Views;

public partial class HomeView
{
    public HomeView()
    {
        InitializeComponent();
    }

    private void OnTournamentDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is TournamentHomeViewModel viewModel)
        {
            viewModel.OpenSelectedTournament();
        }
    }
}
