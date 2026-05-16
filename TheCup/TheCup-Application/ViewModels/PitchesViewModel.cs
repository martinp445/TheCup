using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCup_Application.Commands;
using TheCup_Application.Models;
using TheCup_Application.Ports;
using TheCup_Application.Services;
using TheCup_Domain.Enums;

namespace TheCup_Application.ViewModels;

public sealed class PitchesViewModel : ViewModel, IHasStatusMessage
{
    private readonly ITournamentRepository _repository;
    private Guid _tournamentId;
    private string _tournamentName = string.Empty;
    private TournamentStatus _status = TournamentStatus.Draft;
    private bool _teamsConfirmed;
    private int _teamCount;
    private int _pitchCount;
    private string _newPitchName = string.Empty;
    private string _activationHint = string.Empty;
    private bool _isBusy;
    private string? _errorMessage;

    public PitchesViewModel(ITournamentRepository repository)
    {
        _repository = repository;
        Pitches = new ObservableCollection<PitchItemViewModel>();

        AddPitchCommand = new ActionCommand(
            () => _ = AddPitchAsync(),
            () => !IsBusy && !IsActive && !string.IsNullOrWhiteSpace(NewPitchName));

        RefreshCommand = new ActionCommand(
            () => _ = LoadAsync(),
            () => !IsBusy && _tournamentId != Guid.Empty);

        ActivateTournamentCommand = new ActionCommand(
            () => _ = ActivateTournamentAsync(),
            () => !IsBusy && CanActivate);
    }

    public event Action<TournamentSummary>? TournamentActivated;

    public ObservableCollection<PitchItemViewModel> Pitches { get; }

    public string TournamentName
    {
        get => _tournamentName;
        private set => SetProperty(ref _tournamentName, value);
    }

    public string StatusDisplay => _status.ToString();

    public string ActivationHint
    {
        get => _activationHint;
        private set => SetProperty(ref _activationHint, value);
    }

    public string NewPitchName
    {
        get => _newPitchName;
        set
        {
            if (SetProperty(ref _newPitchName, value))
            {
                RaiseAddPitchCanExecute();
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

    public bool CanActivate { get; private set; }

    public bool ShowActivateSection => IsDraft;

    public bool ShowActiveBanner => IsActive;

    public ICommand AddPitchCommand { get; }

    public ICommand RefreshCommand { get; }

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
        await LoadPitchesAsync().ConfigureAwait(true);
    }

    public async Task LoadAsync()
    {
        if (_tournamentId == Guid.Empty)
        {
            return;
        }

        await LoadAsync(_tournamentId).ConfigureAwait(true);
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

    private async Task LoadPitchesAsync()
    {
        if (IsBusy || _tournamentId == Guid.Empty)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var summary = await _repository.GetByIdAsync(_tournamentId).ConfigureAwait(true);
            if (summary is not null)
            {
                ApplySummary(summary);
            }

            var pitches = await _repository.GetPitchesAsync(_tournamentId).ConfigureAwait(true);

            Pitches.Clear();
            foreach (var pitch in pitches)
            {
                Pitches.Add(CreatePitchItem(pitch));
            }

            RefreshPitchCommandStates();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load pitches: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddPitchAsync()
    {
        if (IsBusy || _tournamentId == Guid.Empty || IsActive)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var added = await _repository
                .AddPitchAsync(_tournamentId, NewPitchName)
                .ConfigureAwait(true);

            Pitches.Add(CreatePitchItem(added));
            NewPitchName = string.Empty;

            var summary = await _repository.GetByIdAsync(_tournamentId).ConfigureAwait(true);
            if (summary is not null)
            {
                ApplySummary(summary);
            }

            RefreshPitchCommandStates();
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

    private void ApplySummary(TournamentSummary summary)
    {
        TournamentName = summary.Name;
        _status = summary.Status;
        _teamsConfirmed = summary.TeamsConfirmed;
        _teamCount = summary.TeamCount;
        _pitchCount = summary.PitchCount;
        CanActivate = TournamentActivationRules.CanActivate(summary);
        ActivationHint = TournamentActivationRules.GetActivationHint(summary);

        OnPropertyChanged(nameof(IsDraft));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(ShowActivateSection));
        OnPropertyChanged(nameof(ShowActiveBanner));
        OnPropertyChanged(nameof(CanActivate));
        RaiseCommandStates();
    }

    private PitchItemViewModel CreatePitchItem(PitchSummary pitch) => new(
        _tournamentId,
        pitch.Id,
        pitch.Name,
        _repository,
        message => ErrorMessage = message,
        () => _ = OnPitchesChangedAsync(),
        () => !IsActive && Pitches.Count > 1,
        () => !IsActive);

    private async Task OnPitchesChangedAsync()
    {
        var summary = await _repository.GetByIdAsync(_tournamentId).ConfigureAwait(true);
        if (summary is not null)
        {
            ApplySummary(summary);
        }

        await LoadPitchesAsync().ConfigureAwait(true);
    }

    private void RefreshPitchCommandStates()
    {
        foreach (var pitch in Pitches)
        {
            pitch.RefreshCommandStates();
        }
    }

    private void RaiseCommandStates()
    {
        RaiseAddPitchCanExecute();

        if (RefreshCommand is ActionCommand refresh)
        {
            refresh.RaiseCanExecuteChanged();
        }

        if (ActivateTournamentCommand is ActionCommand activate)
        {
            activate.RaiseCanExecuteChanged();
        }
    }

    private void RaiseAddPitchCanExecute()
    {
        if (AddPitchCommand is ActionCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }
}
