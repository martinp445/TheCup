using System.ComponentModel;
using TheCup_Application.Models;
using TheCup_Application.Ports;
using TheCup_Application.Services;
using TheCup_Domain.Enums;
using TheCup_Infrastructure.Services;

namespace TheCup_Application.ViewModels;

public sealed class MainWindowViewModel : ViewModel
{
    private const int NavOverview = 0;
    private const int NavTeams = 1;
    private const int NavPitches = 2;
    private const int NavGroups = 3;
    private const int NavMatches = 4;

    private readonly ITournamentRepository _repository;
    private ViewModel? _currentContent;
    private Guid? _activeTournamentId;
    private string? _activeTournamentName;
    private TournamentStatus _activeTournamentStatus = TournamentStatus.Draft;
    private bool _teamsConfirmed;
    private bool _hasActiveTournament;
    private int _selectedNavIndex;
    private bool _suppressNavChange;

    public MainWindowViewModel()
        : this(new InMemoryTournamentRepository(new TournamentPersistenceService()))
    {
    }

    public MainWindowViewModel(ITournamentRepository repository)
    {
        _repository = repository;

        Home = new TournamentHomeViewModel(repository);
        CreateTournament = new CreateTournamentViewModel(repository);
        Teams = new TeamsViewModel(repository);
        Pitches = new PitchesViewModel(repository);
        Groups = new GroupsViewModel(repository);
        Games = new GamesViewModel(repository);

        Home.NewTournamentRequested += ShowCreateTournament;
        Home.OpenTournamentRequested += summary => _ = ShowTeamsAsync(summary);
        Home.TournamentDeleted += OnTournamentDeleted;

        CreateTournament.TournamentCreated += summary => _ = OnTournamentCreatedAsync(summary);
        CreateTournament.CancelRequested += ShowHome;

        Groups.TournamentStarted += summary => _ = OnTournamentActivatedAsync(summary);
        Teams.TournamentActivated += summary => _ = OnTournamentActivatedAsync(summary);
        Pitches.TournamentActivated += summary => _ = OnTournamentActivatedAsync(summary);
        Teams.TeamsConfirmationChanged += OnTeamsConfirmationChanged;

        _currentContent = Home;
        Home.PropertyChanged += OnChildPropertyChanged;
    }

    public TournamentHomeViewModel Home { get; }

    public CreateTournamentViewModel CreateTournament { get; }

    public TeamsViewModel Teams { get; }

    public PitchesViewModel Pitches { get; }

    public GroupsViewModel Groups { get; }

    public GamesViewModel Games { get; }

    public ViewModel? CurrentContent
    {
        get => _currentContent;
        private set
        {
            if (_currentContent is INotifyPropertyChanged old)
            {
                old.PropertyChanged -= OnChildPropertyChanged;
            }

            if (SetProperty(ref _currentContent, value) && value is INotifyPropertyChanged current)
            {
                current.PropertyChanged += OnChildPropertyChanged;
            }

            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(IsStatusBusy));
        }
    }

    public bool HasActiveTournament
    {
        get => _hasActiveTournament;
        private set
        {
            if (SetProperty(ref _hasActiveTournament, value))
            {
                OnPropertyChanged(nameof(IsTournamentNavEnabled));
                OnPropertyChanged(nameof(IsGroupsNavEnabled));
                OnPropertyChanged(nameof(IsGamesNavEnabled));
            }
        }
    }

    public string? ActiveTournamentName
    {
        get => _activeTournamentName;
        private set => SetProperty(ref _activeTournamentName, value);
    }

    public string? StatusMessage => GetActiveChild()?.ErrorMessage;

    public bool IsStatusBusy => GetActiveChild()?.IsBusy ?? false;

    public bool IsTournamentNavEnabled => HasActiveTournament;

    public bool IsGroupsNavEnabled =>
        HasActiveTournament && _activeTournamentStatus == TournamentStatus.Active;

    public bool IsGamesNavEnabled =>
        HasActiveTournament && _activeTournamentStatus == TournamentStatus.Active;

    public int SelectedNavIndex
    {
        get => _selectedNavIndex;
        set
        {
            if (!SetProperty(ref _selectedNavIndex, value) || _suppressNavChange)
            {
                return;
            }

            ApplyNavigationFromUser(value);
        }
    }

    public async Task InitializeAsync()
    {
        // Initialize repository by loading persisted tournaments if it's an InMemoryTournamentRepository
        if (_repository is InMemoryTournamentRepository inMemoryRepo)
        {
            await inMemoryRepo.InitializeAsync().ConfigureAwait(true);
        }

        // Load tournaments into the Home view
        await Home.LoadAsync().ConfigureAwait(true);
    }

    public void ShowHome()
    {
        SetCurrentContent(Home);
        SetNavIndexSilently(NavOverview);
    }

    public void ShowCreateTournament()
    {
        CreateTournament.Reset();
        SetCurrentContent(CreateTournament);
        SetNavIndexSilently(NavOverview);
    }

    public async Task ShowTeamsAsync(TournamentSummary summary)
    {
        SetActiveTournament(summary);
        SetCurrentContent(Teams);
        SetNavIndexSilently(NavTeams);
        await Teams.LoadAsync(summary.Id).ConfigureAwait(true);
    }

    public async Task ShowTeamsAsync()
    {
        if (_activeTournamentId is Guid tournamentId)
        {
            var summary = await _repository.GetByIdAsync(tournamentId).ConfigureAwait(true);
            if (summary is not null)
            {
                await ShowTeamsAsync(summary).ConfigureAwait(true);
            }
        }
    }

    public async Task ShowPitchesAsync(TournamentSummary summary)
    {
        SetActiveTournament(summary);
        SetCurrentContent(Pitches);
        SetNavIndexSilently(NavPitches);
        await Pitches.LoadAsync(summary.Id).ConfigureAwait(true);
    }

    public async Task ShowPitchesAsync()
    {
        if (_activeTournamentId is Guid tournamentId)
        {
            var summary = await _repository.GetByIdAsync(tournamentId).ConfigureAwait(true);
            if (summary is not null)
            {
                await ShowPitchesAsync(summary).ConfigureAwait(true);
            }
        }
    }

    public async Task ShowGroupsAsync(TournamentSummary summary)
    {
        SetActiveTournament(summary);
        SetCurrentContent(Groups);
        SetNavIndexSilently(NavGroups);
        await Groups.LoadAsync(summary.Id).ConfigureAwait(true);
    }

    public async Task ShowGroupsAsync()
    {
        if (_activeTournamentId is Guid tournamentId)
        {
            var summary = await _repository.GetByIdAsync(tournamentId).ConfigureAwait(true);
            if (summary is not null)
            {
                await ShowGroupsAsync(summary).ConfigureAwait(true);
            }
        }
    }

    public async Task ShowGamesAsync(TournamentSummary summary)
    {
        SetActiveTournament(summary);
        SetCurrentContent(Games);
        SetNavIndexSilently(NavMatches);
        await Games.LoadAsync(summary.Id).ConfigureAwait(true);
    }

    public async Task ShowGamesAsync()
    {
        if (_activeTournamentId is  Guid tournamentId)
        {
            var summary = await _repository.GetByIdAsync(tournamentId).ConfigureAwait(true);
            if (summary is not null)
            {
                await ShowGamesAsync(summary).ConfigureAwait(true);
            }
        }
    }

    private async Task OnTournamentCreatedAsync(TournamentSummary summary)
    {
        await Home.LoadAsync().ConfigureAwait(true);
        await ShowTeamsAsync(summary).ConfigureAwait(true);
    }

    private async Task OnTournamentActivatedAsync(TournamentSummary summary)
    {
        SetActiveTournament(summary);
        await Home.LoadAsync().ConfigureAwait(true);

        if (CurrentContent is TeamsViewModel teams)
        {
            await teams.LoadAsync(summary.Id).ConfigureAwait(true);
        }
        else if (CurrentContent is PitchesViewModel pitches)
        {
            await pitches.LoadAsync(summary.Id).ConfigureAwait(true);
        }
        else if (CurrentContent is GroupsViewModel groups)
        {
            await groups.LoadAsync(summary.Id).ConfigureAwait(true);
        }
    }

    private void OnTournamentDeleted(Guid tournamentId)
    {
        if (_activeTournamentId == tournamentId)
        {
            ClearActiveTournament();
            ShowHome();
        }
    }

    private void SetActiveTournament(TournamentSummary summary)
    {
        _activeTournamentId = summary.Id;
        ActiveTournamentName = summary.Name;
        _activeTournamentStatus = summary.Status;
        _teamsConfirmed = summary.TeamsConfirmed;
        HasActiveTournament = true;
        OnPropertyChanged(nameof(IsGroupsNavEnabled));
        OnPropertyChanged(nameof(IsGamesNavEnabled));
    }

    private void OnTeamsConfirmationChanged(TournamentSummary summary)
    {
        if (_activeTournamentId == summary.Id)
        {
            _teamsConfirmed = summary.TeamsConfirmed;
            OnPropertyChanged(nameof(IsGroupsNavEnabled));
        }

        _ = Home.LoadAsync();
    }

    private void ClearActiveTournament()
    {
        _activeTournamentId = null;
        ActiveTournamentName = null;
        _activeTournamentStatus = TournamentStatus.Draft;
        _teamsConfirmed = false;
        HasActiveTournament = false;
    }

    private void SetCurrentContent(ViewModel content)
    {
        CurrentContent = content;
    }

    private void ApplyNavigationFromUser(int index)
    {
        switch (index)
        {
            case NavOverview:
                SetCurrentContent(Home);
                break;
            case NavTeams when HasActiveTournament:
                _ = ShowTeamsAsync();
                break;
            case NavPitches when HasActiveTournament:
                _ = ShowPitchesAsync();
                break;
            case NavGroups when IsGroupsNavEnabled:
                _ = ShowGroupsAsync();
                break;
            case NavMatches when IsGamesNavEnabled:
                _ = ShowGamesAsync();
                break;
            default:
                SetNavIndexSilently(GetNavIndexForContent(CurrentContent));
                break;
        }
    }

    private int GetNavIndexForContent(ViewModel? content) => content switch
    {
        TeamsViewModel => NavTeams,
        PitchesViewModel => NavPitches,
        GroupsViewModel => NavGroups,
        GamesViewModel => NavMatches,
        _ => NavOverview
    };

    private void SetNavIndexSilently(int index)
    {
        _suppressNavChange = true;
        SelectedNavIndex = index;
        _suppressNavChange = false;
    }

    private IHasStatusMessage? GetActiveChild() => CurrentContent switch
    {
        TournamentHomeViewModel home => home,
        CreateTournamentViewModel create => create,
        TeamsViewModel teams => teams,
        PitchesViewModel pitches => pitches,
        GroupsViewModel groups => groups,
        GamesViewModel games => games,
        _ => null
    };

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IHasStatusMessage.ErrorMessage) or nameof(IHasStatusMessage.IsBusy))
        {
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(IsStatusBusy));
        }
    }
}

public interface IHasStatusMessage
{
    string? ErrorMessage { get; }

    bool IsBusy { get; }
}
