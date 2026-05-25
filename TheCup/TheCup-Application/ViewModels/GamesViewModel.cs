using System.Collections.ObjectModel;
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

        public GamesViewModel(ITournamentRepository repository)
        {
            _repository = repository;

            Games = new ObservableCollection<GameSummary>();
        }

        public ObservableCollection<GameSummary> Games { get; }

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
            // await load schedule or something

            GameSummary test1 = new GameSummary
            {
                Id = new Guid(),
                Name = "game 1",
                PitchName = "pitch 1",
                HomeTeamName = "home team",
                AwayTeamName = "away team"
            };

            Games.Add(test1);
            OnPropertyChanged(nameof(Games));
        }

        private void ApplySummary(TournamentSummary summary)
        {
            _status = summary.Status;
            TournamentName = summary.Name;

            OnPropertyChanged(nameof(StatusDisplay));
        }
    }
}
