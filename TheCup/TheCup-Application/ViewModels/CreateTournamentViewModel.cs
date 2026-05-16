using System.Windows.Input;
using TheCup_Application.Commands;
using TheCup_Application.Models;
using TheCup_Application.Ports;
using TheCup_Domain.Enums;

namespace TheCup_Application.ViewModels;

public sealed class CreateTournamentViewModel : ViewModel, IHasStatusMessage
{
    private readonly ITournamentRepository _repository;
    private string _name = string.Empty;
    private SportOption? _selectedSport;
    private bool _isBusy;
    private string? _errorMessage;

    public CreateTournamentViewModel(ITournamentRepository repository)
    {
        _repository = repository;

        SportOptions =
        [
            new SportOption { Sport = Sport.Football, DisplayName = "Football" },
            new SportOption { Sport = Sport.Hockey, DisplayName = "Hockey" },
            new SportOption { Sport = Sport.Floorball, DisplayName = "Floorball" }
        ];

        _selectedSport = SportOptions[0];

        CreateCommand = new ActionCommand(
            () => _ = CreateAsync(),
            () => !IsBusy && !string.IsNullOrWhiteSpace(Name));

        CancelCommand = new ActionCommand(
            () => CancelRequested?.Invoke(),
            () => !IsBusy);
    }

    public event Action<TournamentSummary>? TournamentCreated;

    public event Action? CancelRequested;

    public IReadOnlyList<SportOption> SportOptions { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public SportOption? SelectedSport
    {
        get => _selectedSport;
        set => SetProperty(ref _selectedSport, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ICommand CreateCommand { get; }

    public ICommand CancelCommand { get; }

    public void Reset()
    {
        Name = string.Empty;
        SelectedSport = SportOptions[0];
        ErrorMessage = null;
    }

    private async Task CreateAsync()
    {
        if (IsBusy || SelectedSport is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var created = await _repository
                .CreateAsync(Name, SelectedSport.Sport)
                .ConfigureAwait(true);

            TournamentCreated?.Invoke(created);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseCommandStates()
    {
        if (CreateCommand is ActionCommand create)
        {
            create.RaiseCanExecuteChanged();
        }

        if (CancelCommand is ActionCommand cancel)
        {
            cancel.RaiseCanExecuteChanged();
        }
    }
}
