using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using TheCup_Application.Commands;
using TheCup_Application.Models;
using TheCup_Application.Ports;
using TheCup_Application.Services;
using TheCup_Domain.Enums;
using TheCup_Infrastructure.Services;

namespace TheCup_Application.ViewModels;

public class GamesViewModel : ViewModel, IHasStatusMessage
{
    private readonly ITournamentRepository _repository;
    private readonly IFileSaveDialogService _fileSaveDialog;
    private readonly IGameSchedulePdfExporter _pdfExporter;
    private readonly IDialogService _dialogService;
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

    public GamesViewModel(
        ITournamentRepository repository,
        IFileSaveDialogService fileSaveDialog,
        IGameSchedulePdfExporter pdfExporter,
        IDialogService dialogService)
    {
        _repository = repository;
        _fileSaveDialog = fileSaveDialog;
        _pdfExporter = pdfExporter;
        _dialogService = dialogService;
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

        ExportToPdfCommand = new ActionCommand(
            () => _ = ExportToPdfAsync(),
            () => !IsBusy && CanExportToPdf);

        DeleteScheduleCommand = new ActionCommand(
            async () => await DeleteScheduleAsync(),
            () => !IsBusy && HasSchedule);

        ManualScheduleCommand = new ActionCommand(
            () => _ = OpenManualScheduleDialogAsync(),
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
                OnPropertyChanged(nameof(CanExportToPdf));
                RaiseGenerateScheduleCanExecute();
                RaiseExportCanExecute();
            }
        }
    }

    public bool ShowScheduleGenerator => IsActive && HasGroups && !HasSchedule;

    public bool CanGenerateSchedule =>
        IsActive && HasGroups && !HasSchedule && !IsBusy
        && SelectedScheduleType is not null && SelectedScheduleType.IsAvailable;

    public bool CanExportToPdf => HasSchedule && Games.Count > 0 && !IsBusy;

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

    public ICommand ExportToPdfCommand { get; }

    public ICommand DeleteScheduleCommand { get; }

    public ICommand ManualScheduleCommand { get; }

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
        RaiseExportCanExecute();
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
            RaiseExportCanExecute();
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

    private async Task DeleteScheduleAsync()
    {
        if (IsBusy || _tournamentId == Guid.Empty || !HasSchedule)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            await _repository
                .DeleteScheduleAsync(_tournamentId)
                .ConfigureAwait(true);

            HasSchedule = false;
            Games.Clear();
            
            StatusMessage = "Games was deleted.";
            OnPropertyChanged(nameof(Games));
            RaiseExportCanExecute();
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

    private async Task ExportToPdfAsync()
    {
        if (IsBusy || !CanExportToPdf)
        {
            return;
        }

        var suggestedFileName = $"{SanitizeFileName(TournamentName)} - Games.pdf";
        var filePath = _fileSaveDialog.PromptSavePdf(suggestedFileName);
        if (filePath is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var request = new GameSchedulePdfRequest
            {
                TournamentName = TournamentName,
                Games = Games
                    .Select(game => new GameSchedulePdfRow
                    {
                        PitchName = game.PitchName,
                        HomeTeamName = game.HomeTeamName,
                        AwayTeamName = game.AwayTeamName
                    })
                    .ToList()
            };

            await _pdfExporter.ExportAsync(request, filePath).ConfigureAwait(true);
            StatusMessage = $"Exported {Games.Count} games to PDF.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to export PDF: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OpenManualScheduleDialogAsync()
    {
        if (IsBusy || _tournamentId == Guid.Empty)
        {
            return;
        }

        try
        {
            var manualScheduleViewModel = new ManualScheduleViewModel(_repository, _tournamentId);
            _dialogService.ShowDialog(manualScheduleViewModel);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to open manual schedule dialog: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(character => invalidChars.Contains(character) ? '_' : character)).Trim();
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
        RaiseExportCanExecute();

        if (StartGameCommand is ActionCommand start)
        {
            start.RaiseCanExecuteChanged();
        }

        if (FinishGameCommand is ActionCommand finish)
        {
            finish.RaiseCanExecuteChanged();
        }

        if (DeleteScheduleCommand is ActionCommand delete)
        {
            delete.RaiseCanExecuteChanged();
        }
    }

    private void RaiseGenerateScheduleCanExecute()
    {
        OnPropertyChanged(nameof(CanGenerateSchedule));

        if (GenerateScheduleCommand is ActionCommand generate)
        {
            generate.RaiseCanExecuteChanged();
        }

        if (ManualScheduleCommand is ActionCommand manual)
        {
            manual.RaiseCanExecuteChanged();
        }
    }

    private void RaiseExportCanExecute()
    {
        OnPropertyChanged(nameof(CanExportToPdf));

        if (ExportToPdfCommand is ActionCommand export)
        {
            export.RaiseCanExecuteChanged();
        }
    }
}
