using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using TheCup_Application.Commands;
using TheCup_Application.Models;
using TheCup_Application.Ports;

namespace TheCup_Application.ViewModels;

public sealed class TournamentHomeViewModel : ViewModel, IHasStatusMessage
{
    private readonly ITournamentRepository _repository;
    private bool _isBusy;
    private string? _errorMessage;
    private bool _isEmpty = true;
    private TournamentSummary? _selectedTournament;

    public TournamentHomeViewModel(ITournamentRepository repository)
    {
        _repository = repository;
        Tournaments = new ObservableCollection<TournamentSummary>();

        RefreshCommand = new ActionCommand(
            () => _ = LoadAsync(),
            () => !IsBusy);

        NewTournamentCommand = new ActionCommand(
            () => NewTournamentRequested?.Invoke(),
            () => !IsBusy);

        OpenTournamentCommand = new ActionCommand(
            () => OpenSelectedTournament(),
            () => !IsBusy && SelectedTournament is not null);

        DeleteTournamentCommand = new ActionCommand(
            () => _ = DeleteSelectedTournamentAsync(),
            () => !IsBusy && SelectedTournament is not null);

        SaveTournamentCommand = new ActionCommand(
            () => _ = SaveSelectedTournamentAsync(),
            () => !IsBusy && SelectedTournament is not null);
    }

    public event Action? NewTournamentRequested;

    public event Action<TournamentSummary>? OpenTournamentRequested;

    public event Action<Guid>? TournamentDeleted;

    public ObservableCollection<TournamentSummary> Tournaments { get; }

    public TournamentSummary? SelectedTournament
    {
        get => _selectedTournament;
        set
        {
            if (SetProperty(ref _selectedTournament, value))
            {
                RaiseSelectionCommandStates();
            }
        }
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

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public ICommand RefreshCommand { get; }

    public ICommand NewTournamentCommand { get; }

    public ICommand OpenTournamentCommand { get; }

    public ICommand DeleteTournamentCommand { get; }

    public ICommand SaveTournamentCommand { get; }

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await RefreshTournamentsAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load tournaments: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshTournamentsAsync()
    {
        ErrorMessage = null;

        var tournaments = await _repository.GetAllAsync().ConfigureAwait(true);

        Tournaments.Clear();
        foreach (var tournament in tournaments)
        {
            Tournaments.Add(tournament);
        }

        IsEmpty = Tournaments.Count == 0;
    }

    public void OpenSelectedTournament()
    {
        if (SelectedTournament is not null)
        {
            OpenTournamentRequested?.Invoke(SelectedTournament);
        }
    }

    private async Task DeleteSelectedTournamentAsync()
    {
        if (IsBusy || SelectedTournament is null)
        {
            return;
        }

        var tournament = SelectedTournament;
        var confirm = MessageBox.Show(
            $"Delete tournament \"{tournament.Name}\"?\n\nAll teams and data for this tournament will be removed. This cannot be undone.",
            "Delete tournament",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            await _repository.DeleteAsync(tournament.Id).ConfigureAwait(true);

            SelectedTournament = null;
            await RefreshTournamentsAsync().ConfigureAwait(true);

            TournamentDeleted?.Invoke(tournament.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete tournament: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveSelectedTournamentAsync()
    {
        if (IsBusy || SelectedTournament is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            await _repository.SaveAsync(SelectedTournament.Id).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save tournament: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseSelectionCommandStates()
    {
        if (OpenTournamentCommand is ActionCommand open)
        {
            open.RaiseCanExecuteChanged();
        }

        if (DeleteTournamentCommand is ActionCommand delete)
        {
            delete.RaiseCanExecuteChanged();
        }

        if (SaveTournamentCommand is ActionCommand save)
        {
            save.RaiseCanExecuteChanged();
        }
    }

    private void RaiseCommandStates()
    {
        if (RefreshCommand is ActionCommand refresh)
        {
            refresh.RaiseCanExecuteChanged();
        }

        if (NewTournamentCommand is ActionCommand create)
        {
            create.RaiseCanExecuteChanged();
        }

        RaiseSelectionCommandStates();
    }
}
