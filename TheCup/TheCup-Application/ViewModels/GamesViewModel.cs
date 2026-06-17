using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCup_Application.Commands;
using TheCup_Application.Models;
using TheCup_Application.Ports;
using TheCup_Application.Services;
using TheCup_Domain.Enums;

namespace TheCup_Application.ViewModels;

public class GamesViewModel : ViewModel, IHasStatusMessage
{
    private readonly ITournamentRepository _repository;
    private Guid _tournamentId;
    private string _tournamentName = string.Empty;
    private TournamentStatus _status = TournamentStatus.Draft;
    private int _groupCount;
    private int _pitchCount;
    private bool _hasGroups;
    private bool _hasSchedule;
    private bool _isBusy;
    private string? _errorMessage;
    private string _statusMessage = string.Empty;
    private GameSummary? _selectedGame;
    private ScheduleTypeOption? _selectedScheduleType;

    public GamesViewModel(ITournamentRepository repository)
    {
        _repository = repository;
        Games = new ObservableCollection<GameSummary>();
        ScheduleTypeOptions = new ObservableCollection<ScheduleTypeOption>();

        StartGameCommand = new ActionCommand(
            async (param) => await StartGameAsync(param as GameSummary),
            (param) => !IsBusy && (param as GameSummary)?.Status == GameStatus.Scheduled);

        FinishGameCommand = new ActionCommand(
            async (param) => await FinishGameAsync(param as GameSummary),
            (param) => !IsBusy && (param as GameSummary)?.Status == GameStatus.Ongoing);

        GenerateScheduleCommand = new ActionCommand(
            () => _ = GenerateScheduleAsync(),
            () => !IsBusy && CanGenerateSchedule);
    }

    public ObservableCollection<GameSummary> Games { get; }

    public ObservableCollection<ScheduleTypeOption> ScheduleTypeOptions { get; }

    public GameSummary? SelectedGame
    {
        get => _selectedGame;
        set => SetProperty(ref _selectedGame, value);
    }

    public ScheduleTypeOption? SelectedScheduleType
    {
        get => _selectedScheduleType;
        set
        {
            if (SetProperty(ref _selectedScheduleType, value))
            {
                RaiseGenerateScheduleCanExecute();
            }
        }
    }

    public string TournamentName
    {
        get => _tournamentName;
        private set => SetProperty(ref _tournamentName, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
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

    public bool HasSchedule
    {
        get => _hasSchedule;
        private set
        {
            if (SetProperty(ref _hasSchedule, value))
            {
                OnPropertyChanged(nameof(ShowScheduleGenerator));
                RaiseGenerateScheduleCanExecute();
            }
        }
    }

    public bool ShowScheduleGenerator => IsActive && HasGroups && !HasSchedule;

    public bool CanGenerateSchedule =>
        IsActive && HasGroups && !HasSchedule && !IsBusy
        && SelectedScheduleType is not null && SelectedScheduleType.IsAvailable;

    public bool IsActive => _status == TournamentStatus.Active;

    public bool HasGroups
    {
        get => _hasGroups;
        private set
        {
            if (SetProperty(ref _hasGroups, value))
            {
                OnPropertyChanged(nameof(ShowScheduleGenerator));
                RaiseGenerateScheduleCanExecute();
            }
        }
    }

    public string StatusDisplay => _status.ToString();

    public ICommand StartGameCommand { get; }

    public ICommand FinishGameCommand { get; }

    public ICommand GenerateScheduleCommand { get; }

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
        UpdateScheduleTypeAvailability();
        EnsureSelectedScheduleType();

        var games = await _repository.GetScheduleAsync(tournamentId).ConfigureAwait(true);
        HasSchedule = games.Count > 0;

        Games.Clear();
        foreach (var game in games)
        {
            Games.Add(game);
        }

        if (games.Count > 0)
        {
            StatusMessage = $"{games.Count} games scheduled.";
        }
        else if (ShowScheduleGenerator)
        {
            StatusMessage = "Choose a schedule type and generate games.";
        }
        else if (!HasGroups)
        {
            StatusMessage = "Generate groups before creating the schedule.";
        }

        OnPropertyChanged(nameof(Games));
    }

    private async Task GenerateScheduleAsync()
    {
        if (IsBusy || _tournamentId == Guid.Empty || !CanGenerateSchedule || SelectedScheduleType is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var games = await _repository
                .GenerateScheduleAsync(_tournamentId, SelectedScheduleType.Type)
                .ConfigureAwait(true);

            HasSchedule = games.Count > 0;
            Games.Clear();
            foreach (var game in games)
            {
                Games.Add(game);
            }

            StatusMessage = $"Generated {games.Count} games.";
            OnPropertyChanged(nameof(Games));
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

    private async Task StartGameAsync(GameSummary? game)
    {
        if (game is null)
        {
            return;
        }

        try
        {
            ErrorMessage = null;
            game.Status = await _repository.StartGameAsync(_tournamentId, game.Id).ConfigureAwait(true);
            RaiseCommandStates();
            OnPropertyChanged(nameof(Games));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to start game: {ex.Message}";
        }
    }

    private async Task FinishGameAsync(GameSummary? game)
    {
        if (game is null)
        {
            return;
        }

        try
        {
            ErrorMessage = null;
            game.Status = await _repository.FinishGameAsync(
                _tournamentId,
                game.Id,
                game.HomeTeamScore,
                game.AwayTeamScore).ConfigureAwait(true);
            RaiseCommandStates();
            OnPropertyChanged(nameof(Games));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to finish game: {ex.Message}";
        }
    }

    private void ApplySummary(TournamentSummary summary)
    {
        _status = summary.Status;
        _groupCount = summary.GroupCount;
        _pitchCount = summary.PitchCount;
        HasGroups = summary.HasGroups;
        TournamentName = summary.Name;

        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(ShowScheduleGenerator));
        RaiseGenerateScheduleCanExecute();
    }

    private void UpdateScheduleTypeAvailability()
    {
        ScheduleTypeOptions.Clear();
        foreach (var option in ScheduleTypeCatalog.CreateOptions())
        {
            option.IsAvailable = ScheduleTypeCatalog.IsAvailable(option.Type, _groupCount, _pitchCount);
            ScheduleTypeOptions.Add(option);
        }
    }

    private void EnsureSelectedScheduleType()
    {
        var currentType = SelectedScheduleType?.Type ?? ScheduleType.Default;
        SelectedScheduleType = ScheduleTypeOptions.FirstOrDefault(option => option.Type == currentType && option.IsAvailable)
            ?? ScheduleTypeOptions.FirstOrDefault(option => option.IsAvailable)
            ?? ScheduleTypeOptions.FirstOrDefault();
    }

    private void RaiseCommandStates()
    {
        RaiseGenerateScheduleCanExecute();

        if (StartGameCommand is ActionCommand start)
        {
            start.RaiseCanExecuteChanged();
        }

        if (FinishGameCommand is ActionCommand finish)
        {
            finish.RaiseCanExecuteChanged();
        }
    }

    private void RaiseGenerateScheduleCanExecute()
    {
        OnPropertyChanged(nameof(CanGenerateSchedule));

        if (GenerateScheduleCommand is ActionCommand generate)
        {
            generate.RaiseCanExecuteChanged();
        }
    }
}
