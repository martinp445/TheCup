using System.Windows;
using TheCup_Application.ViewModels;
using TheCup_Application.Services;
using TheCup_Infrastructure.Services;
using TheCup_Presentation.Services;

namespace TheCup_Presentation;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(
            new InMemoryTournamentRepository(new TournamentPersistenceService()),
            new WpfFileSaveDialogService(),
            new GameSchedulePdfExporter());
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync().ConfigureAwait(true);
    }
}
