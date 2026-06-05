using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCup_Application.Commands;
using TheCup_Application.Models;
using TheCup_Application.Ports;
using TheCup_Domain.Enums;

namespace TheCup_Application.ViewModels
{
    public class GamesViewModel : ViewModel
    {
        private readonly ITournamentRepository _repository;
        private Guid _tournamentId;
        private string _tournamentName = string.Empty;
        private TournamentStatus _status = TournamentStatus.Draft;
        private string? _errorMessage;
        private string _statusMessage = string.Empty;
        private GameSummary? _selectedGame;

        public GamesViewModel(ITournamentRepository repository)
        {
            _repository = repository;
            Games = new ObservableCollection<GameSummary>();

            StartGameCommand = new ActionCommand(
                async (param) => await StartGameAsync(param as GameSummary),
                (param) => (param as GameSummary)?.Status == GameStatus.Scheduled);
            FinishGameCommand = new ActionCommand(
                async (param) => await FinishGameAsync(param as GameSummary),
                (param) => (param as GameSummary)?.Status == GameStatus.Ongoing);
        }

        public ObservableCollection<GameSummary> Games { get; }

        public GameSummary? SelectedGame
        {
            get => _selectedGame;
            set => SetProperty(ref _selectedGame, value);
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

        public string StatusDisplay => _status.ToString();

        public ICommand StartGameCommand { get; }

        public ICommand FinishGameCommand { get; }

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
            var games = await _repository.GetScheduleAsync(tournamentId).ConfigureAwait(true);

            Games.Clear();
            foreach (var game in games)
            {
                Games.Add(game);
            }

            OnPropertyChanged(nameof(Games));
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
                game.Status = await _repository.FinishGameAsync(_tournamentId, game.Id, game.HomeTeamScore, game.AwayTeamScore).ConfigureAwait(true);
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
            TournamentName = summary.Name;

            OnPropertyChanged(nameof(StatusDisplay));
        }

        private void RaiseCommandStates()
        {
            if (StartGameCommand is ActionCommand start)
            {
                start.RaiseCanExecuteChanged();
            }

            if (FinishGameCommand is ActionCommand finish)
            {
                finish.RaiseCanExecuteChanged();
            }
        }
    }
}
