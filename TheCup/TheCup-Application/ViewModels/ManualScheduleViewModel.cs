using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCup_Application.Commands;
using TheCup_Application.Ports;

namespace TheCup_Application.ViewModels
{
    public class ManualScheduleViewModel : ViewModel
    {
        private readonly ITournamentRepository _repository;
        private readonly Guid _tournamentId;

        public struct ScheduledGame
        {
            public string TeamA { get; set; }
            public string TeamB { get; set; }
            public string Pitch { get; set; }
            public int Round { get; set; }
        }


        private string _teamA = string.Empty;
        private string _teamB = string.Empty;
        private string _pitch = string.Empty;
        private int round = 1;

        public ManualScheduleViewModel(ITournamentRepository repository, Guid tournamentId)
        {
            _repository = repository;
            _tournamentId = tournamentId;

            AddCommand = new ActionCommand(() =>
            {
                if (!string.IsNullOrWhiteSpace(TeamA) && !string.IsNullOrWhiteSpace(TeamB) && !string.IsNullOrWhiteSpace(Pitch))
                {
                    Items.Add(new ScheduledGame
                    {
                        TeamA = TeamA,
                        TeamB = TeamB,
                        Pitch = Pitch,
                        Round = Round
                    });
                    // Clear the input fields after adding the game
                    TeamA = string.Empty;
                    TeamB = string.Empty;
                    Pitch = string.Empty;

                    OnPropertyChanged(nameof(Items)); // Notify that the Items collection has changed

                    if (ConfirmCommand is ActionCommand confirmCommand)
                    {
                        confirmCommand.RaiseCanExecuteChanged();
                    }
                }
            }, () => true);

            ConfirmCommand = new ActionCommand(async () => await ConfirmSchedule(), () => Items.Count > 0);
        }

        public ICommand AddCommand { get; }

        public ICommand ConfirmCommand { get; }

        public string TeamA
        {
            get => _teamA;
            set => SetProperty(ref _teamA, value);
        }

        public string TeamB
        {
            get => _teamB;
            set => SetProperty(ref _teamB, value);
        }

        public string Pitch
        {
            get => _pitch;
            set => SetProperty(ref _pitch, value);
        }

        public int Round
        {
            get => round;
            set => SetProperty(ref round, value);
        }

        public ObservableCollection<ScheduledGame> Items { get; } = new ObservableCollection<ScheduledGame>();


        private Task ConfirmSchedule()
        {
            foreach (var game in Items)
            {
                var teamA = game.TeamA.ToLower();
                var teamB = game.TeamB.ToLower();
                var pitch = game.Pitch.ToLower();

                _repository.AddGame(_tournamentId, teamA, teamB, pitch, game.Round);
            }

            return Task.CompletedTask;
        }
    }
}
