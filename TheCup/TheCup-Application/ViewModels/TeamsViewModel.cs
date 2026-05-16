using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCup_Application.Commands;
using TheCup_Application.Models;
using TheCup_Application.Ports;
using TheCup_Application.Services;
using TheCup_Domain.Enums;

namespace TheCup_Application.ViewModels;

public sealed class TeamsViewModel : ViewModel, IHasStatusMessage
{
    private readonly ITournamentRepository _repository;
    private Guid _tournamentId;
    private string _tournamentName = string.Empty;
    private string _newTeamName = string.Empty;
    private bool _isBusy;
    private string? _errorMessage;
    private bool _isEmpty = true;
    private bool _teamsConfirmed;
    private TournamentStatus _status = TournamentStatus.Draft;
    private int _pitchCount;
    private string _infoMessage = string.Empty;
    private string _activationHint = string.Empty;

    public TeamsViewModel(ITournamentRepository repository)
    {
        _repository = repository;
        Teams = new ObservableCollection<TeamItemViewModel>();

        AddTeamCommand = new ActionCommand(
            () => _ = AddTeamAsync(),
            () => !IsBusy && !TeamsConfirmed && !string.IsNullOrWhiteSpace(NewTeamName));

        RefreshCommand = new ActionCommand(
            () => _ = LoadAsync(),
            () => !IsBusy && _tournamentId != Guid.Empty);

        ConfirmTeamsCommand = new ActionCommand(
            () => _ = ConfirmTeamsAsync(),
            () => !IsBusy && !TeamsConfirmed && !IsEmpty);

        EditTeamsCommand = new ActionCommand(
            () => _ = UnconfirmTeamsAsync(),
            () => !IsBusy && TeamsConfirmed);

        ActivateTournamentCommand = new ActionCommand(
            () => _ = ActivateTournamentAsync(),
            () => !IsBusy && CanActivate);
    }

    public event Action<TournamentSummary>? TeamsConfirmationChanged;

    public event Action<TournamentSummary>? TournamentActivated;

    public ObservableCollection<TeamItemViewModel> Teams { get; }

    public string TournamentName
    {
        get => _tournamentName;
        private set => SetProperty(ref _tournamentName, value);
    }

    public string NewTeamName
    {
        get => _newTeamName;
        set
        {
            if (SetProperty(ref _newTeamName, value))
            {
                RaiseAddTeamCanExecute();
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

    public string InfoMessage
    {
        get => _infoMessage;
        private set => SetProperty(ref _infoMessage, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set
        {
            if (SetProperty(ref _isEmpty, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool TeamsConfirmed
    {
        get => _teamsConfirmed;
        private set
        {
            if (SetProperty(ref _teamsConfirmed, value))
            {
                OnPropertyChanged(nameof(CanEditTeams));
                OnPropertyChanged(nameof(ShowConfirmSection));
                OnPropertyChanged(nameof(ShowConfirmedSection));
                RaiseCommandStates();
                RefreshTeamItemStates();
            }
        }
    }

    public bool CanEditTeams => !TeamsConfirmed;

    public bool ShowConfirmSection => !TeamsConfirmed;

    public bool ShowConfirmedSection => TeamsConfirmed;

    public bool IsDraft => _status == TournamentStatus.Draft;

    public bool IsActive => _status == TournamentStatus.Active;

    public string StatusDisplay => _status.ToString();

    public string ActivationHint
    {
        get => _activationHint;
        private set => SetProperty(ref _activationHint, value);
    }

    public bool CanActivate { get; private set; }

    public bool ShowActivateSection => TeamsConfirmed && IsDraft;

    public bool ShowActiveBanner => IsActive;

    public ICommand AddTeamCommand { get; }

    public ICommand RefreshCommand { get; }

    public ICommand ConfirmTeamsCommand { get; }

    public ICommand EditTeamsCommand { get; }

    public ICommand ActivateTournamentCommand { get; }

    public async Task LoadAsync(Guid tournamentId)
    {
        _tournamentId = tournamentId;

        var summary = await _repository.GetByIdAsync(tournamentId).ConfigureAwait(true);
        if (summary is null)
        {
            ErrorMessage = "Tournament was not found.";
            return;
        }

        ApplySummary(summary);
        await LoadTeamsAsync().ConfigureAwait(true);
    }

    public async Task LoadAsync()
    {
        if (_tournamentId == Guid.Empty)
        {
            return;
        }

        await LoadAsync(_tournamentId).ConfigureAwait(true);
    }

    private async Task LoadTeamsAsync()
    {
        if (IsBusy || _tournamentId == Guid.Empty)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var teams = await _repository.GetTeamsAsync(_tournamentId).ConfigureAwait(true);

            Teams.Clear();
            foreach (var team in teams)
            {
                Teams.Add(CreateTeamItem(team));
            }

            IsEmpty = Teams.Count == 0;
            UpdateInfoMessage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load teams: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddTeamAsync()
    {
        if (IsBusy || _tournamentId == Guid.Empty || TeamsConfirmed)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var added = await _repository
                .AddTeamAsync(_tournamentId, NewTeamName)
                .ConfigureAwait(true);

            Teams.Add(CreateTeamItem(added));
            NewTeamName = string.Empty;
            IsEmpty = false;
            UpdateInfoMessage();
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

    private async Task ActivateTournamentAsync()
    {
        if (IsBusy || _tournamentId == Guid.Empty || !CanActivate)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var summary = await _repository.StartTournamentAsync(_tournamentId).ConfigureAwait(true);
            ApplySummary(summary);
            TournamentActivated?.Invoke(summary);
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

    private async Task ConfirmTeamsAsync()
    {
        if (IsBusy || _tournamentId == Guid.Empty || TeamsConfirmed || IsEmpty)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var summary = await _repository.ConfirmTeamsAsync(_tournamentId).ConfigureAwait(true);
            ApplySummary(summary);
            TeamsConfirmationChanged?.Invoke(summary);
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

    private async Task UnconfirmTeamsAsync()
    {
        if (IsBusy || _tournamentId == Guid.Empty || !TeamsConfirmed)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var summary = await _repository.UnconfirmTeamsAsync(_tournamentId).ConfigureAwait(true);
            ApplySummary(summary);
            TeamsConfirmationChanged?.Invoke(summary);
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

    private TeamItemViewModel CreateTeamItem(TeamSummary team) => new(
        _tournamentId,
        team.Id,
        team.Name,
        _repository,
        message => ErrorMessage = message,
        () => _ = OnTeamListChangedAsync(),
        () => CanEditTeams);

    private async Task OnTeamListChangedAsync()
    {
        var summary = await _repository.GetByIdAsync(_tournamentId).ConfigureAwait(true);
        if (summary is not null)
        {
            ApplySummary(summary);
            TeamsConfirmationChanged?.Invoke(summary);
        }

        await LoadTeamsAsync().ConfigureAwait(true);
    }

    private void ApplySummary(TournamentSummary summary)
    {
        TournamentName = summary.Name;
        TeamsConfirmed = summary.TeamsConfirmed;
        _status = summary.Status;
        _pitchCount = summary.PitchCount;
        CanActivate = TournamentActivationRules.CanActivate(summary);
        ActivationHint = TournamentActivationRules.GetActivationHint(summary);

        OnPropertyChanged(nameof(IsDraft));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(ShowActivateSection));
        OnPropertyChanged(nameof(ShowActiveBanner));
        OnPropertyChanged(nameof(CanActivate));
        UpdateInfoMessage();
        RaiseCommandStates();
    }

    private void UpdateInfoMessage()
    {
        if (IsActive)
        {
            InfoMessage = "Tournament is active. Open Groups to run the group draw.";
            return;
        }

        if (TeamsConfirmed)
        {
            InfoMessage = "Teams are confirmed. The Groups section is available. Activate when pitches are ready.";
            return;
        }

        InfoMessage = IsEmpty
            ? "Add at least one team, then confirm to unlock the Groups section."
            : "When the team list is complete, confirm teams to unlock the Groups section.";
    }

    private void RefreshTeamItemStates()
    {
        foreach (var team in Teams)
        {
            team.RefreshCommandStates();
        }
    }

    private void RaiseCommandStates()
    {
        RaiseAddTeamCanExecute();

        if (RefreshCommand is ActionCommand refresh)
        {
            refresh.RaiseCanExecuteChanged();
        }

        if (ConfirmTeamsCommand is ActionCommand confirm)
        {
            confirm.RaiseCanExecuteChanged();
        }

        if (EditTeamsCommand is ActionCommand edit)
        {
            edit.RaiseCanExecuteChanged();
        }

        if (ActivateTournamentCommand is ActionCommand activate)
        {
            activate.RaiseCanExecuteChanged();
        }
    }

    private void RaiseAddTeamCanExecute()
    {
        if (AddTeamCommand is ActionCommand command)
        {
            command.RaiseCanExecuteChanged();
        }

        if (ConfirmTeamsCommand is ActionCommand confirm)
        {
            confirm.RaiseCanExecuteChanged();
        }
    }
}
