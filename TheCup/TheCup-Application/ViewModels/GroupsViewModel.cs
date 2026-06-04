using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCup_Application.Commands;
using TheCup_Application.Models;
using TheCup_Application.Ports;
using TheCup_Application.Services;
using TheCup_Domain.Enums;

namespace TheCup_Application.ViewModels;

public sealed class GroupsViewModel : ViewModel, IHasStatusMessage
{
    private readonly ITournamentRepository _repository;
    private Guid _tournamentId;
    private string _tournamentName = string.Empty;
    private TournamentStatus _status = TournamentStatus.Draft;
    private int _teamCount;
    private int _pitchCount;
    private int _groupCount = 4;
    private int _teamsPerGroup = 4;
    private int _minTeamsPerGroup = 3;
    private bool _isBusy;
    private string? _errorMessage;
    private string _statusMessage = string.Empty;
    private bool _hasGroups;
    private bool _teamsConfirmed;

    public GroupsViewModel(ITournamentRepository repository)
    {
        _repository = repository;
        Groups = new ObservableCollection<GroupSummary>();

        StartTournamentCommand = new ActionCommand(
            () => _ = StartTournamentAsync(),
            () => !IsBusy && CanStartTournament);

        GenerateGroupsCommand = new ActionCommand(
            () => _ = GenerateGroupsAsync(),
            () => !IsBusy && CanGenerateGroups);

        ScheduleCommand = new ActionCommand(
            () => _ = ScheduleAsync(),
            () => !IsBusy && CanSchedule);

        RefreshCommand = new ActionCommand(
            () => _ = LoadAsync(),
            () => !IsBusy && _tournamentId != Guid.Empty);
    }

    public event Action<TournamentSummary>? TournamentStarted;

    public ObservableCollection<GroupSummary> Groups { get; }

    public string TournamentName
    {
        get => _tournamentName;
        private set => SetProperty(ref _tournamentName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string StatusDisplay => _status.ToString();

    public int GroupCount
    {
        get => _groupCount;
        set
        {
            if (SetProperty(ref _groupCount, value))
            {
                RaiseGenerateCanExecute();
            }
        }
    }

    public int TeamsPerGroup
    {
        get => _teamsPerGroup;
        set
        {
            if (SetProperty(ref _teamsPerGroup, value))
            {
                RaiseGenerateCanExecute();
            }
        }
    }

    public int MinTeamsPerGroup
    {
        get => _minTeamsPerGroup;
        set
        {
            if (SetProperty(ref _minTeamsPerGroup, value))
            {
                RaiseGenerateCanExecute();
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

    public bool IsDraft => _status == TournamentStatus.Draft;

    public bool IsActive => _status == TournamentStatus.Active;

    public bool HasGroups
    {
        get => _hasGroups;
        private set => SetProperty(ref _hasGroups, value);
    }

    public bool CanStartTournament =>
        IsDraft && _teamsConfirmed && _teamCount >= 1 && _pitchCount >= 1 && !IsBusy;

    private string _activationHint = string.Empty;

    public string ActivationHint
    {
        get => _activationHint;
        private set => SetProperty(ref _activationHint, value);
    }

    public bool CanGenerateGroups =>
        IsActive && _teamCount >= 1 && GroupCount >= 1 && TeamsPerGroup >= MinTeamsPerGroup
        && MinTeamsPerGroup >= 1 && !IsBusy;

    public bool CanSchedule => IsActive && HasGroups;

    public bool ShowStartSection => IsDraft;

    public bool ShowGeneratorSection => IsActive;

    public ICommand StartTournamentCommand { get; }

    public ICommand GenerateGroupsCommand { get; }

    public ICommand ScheduleCommand { get; }

    public ICommand RefreshCommand { get; }

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
        await LoadGroupsAsync().ConfigureAwait(true);
    }

    public async Task LoadAsync()
    {
        if (_tournamentId == Guid.Empty)
        {
            return;
        }

        await LoadAsync(_tournamentId).ConfigureAwait(true);
    }

    private async Task StartTournamentAsync()
    {
        if (IsBusy || _tournamentId == Guid.Empty || !CanStartTournament)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var summary = await _repository.StartTournamentAsync(_tournamentId).ConfigureAwait(true);
            ApplySummary(summary);
            TournamentStarted?.Invoke(summary);
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

    private async Task GenerateGroupsAsync()
    {
        if (IsBusy || _tournamentId == Guid.Empty || !CanGenerateGroups)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var groups = await _repository
                .GenerateGroupsAsync(_tournamentId, GroupCount, TeamsPerGroup, MinTeamsPerGroup)
                .ConfigureAwait(true);

            UpdateGroups(groups);
            StatusMessage = $"Generated {groups.Count} groups from {_teamCount} teams.";
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

    private async Task ScheduleAsync()
    {
        if (IsBusy || _tournamentId == Guid.Empty || !CanSchedule)
        {
            return;
        }

        try
        {
            IsBusy = true;

            var games = await _repository.GenerateScheduleAsync(_tournamentId).ConfigureAwait(true);
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

    private async Task LoadGroupsAsync()
    {
        if (_tournamentId == Guid.Empty)
        {
            return;
        }

        try
        {
            var groups = await _repository.GetGroupsAsync(_tournamentId).ConfigureAwait(true);
            UpdateGroups(groups);

            if (groups.Count > 0)
            {
                StatusMessage = $"{groups.Count} groups configured.";
            }
            else if (IsDraft)
            {
                StatusMessage = "Add teams and pitches, then start the tournament to generate groups.";
            }
            else
            {
                StatusMessage = "Configure group settings and run the draw.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void ApplySummary(TournamentSummary summary)
    {
        TournamentName = summary.Name;
        _status = summary.Status;
        _teamCount = summary.TeamCount;
        _pitchCount = summary.PitchCount;
        HasGroups = summary.HasGroups;
        _teamsConfirmed = summary.TeamsConfirmed;
        ActivationHint = TournamentActivationRules.GetActivationHint(summary);

        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(IsDraft));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(CanStartTournament));
        OnPropertyChanged(nameof(CanGenerateGroups));
        OnPropertyChanged(nameof(CanSchedule));
        OnPropertyChanged(nameof(ShowStartSection));
        OnPropertyChanged(nameof(ShowGeneratorSection));
        RaiseCommandStates();
    }

    private void UpdateGroups(IReadOnlyList<GroupSummary> groups)
    {
        Groups.Clear();
        foreach (var group in groups)
        {
            Groups.Add(group);
        }

        HasGroups = groups.Count > 0;

        RaiseScheduleCanExecute();
    }

    private void RaiseGenerateCanExecute()
    {
        OnPropertyChanged(nameof(CanGenerateGroups));
        if (GenerateGroupsCommand is ActionCommand generate)
        {
            generate.RaiseCanExecuteChanged();
        }
    }

    private void RaiseCommandStates()
    {
        OnPropertyChanged(nameof(CanStartTournament));
        OnPropertyChanged(nameof(CanGenerateGroups));

        if (StartTournamentCommand is ActionCommand start)
        {
            start.RaiseCanExecuteChanged();
        }

        RaiseGenerateCanExecute();
        RaiseScheduleCanExecute();

        if (RefreshCommand is ActionCommand refresh)
        {
            refresh.RaiseCanExecuteChanged();
        }
    }

    private void RaiseScheduleCanExecute()
    {
        OnPropertyChanged(nameof(CanSchedule));
        if (ScheduleCommand is ActionCommand schedule)
        {
            schedule.RaiseCanExecuteChanged();
        }
    }
}
