using System.ComponentModel;
using System.Runtime.CompilerServices;
using TheCup_Domain.Enums;

namespace TheCup_Application.Models
{
    public sealed class GameSummary : INotifyPropertyChanged
    {
        public required Guid Id { get; init; }

        public required string Name { get; init; }

        public required string PitchName { get; init; }

        public required string HomeTeamName { get; init; }

        public required string AwayTeamName { get; init; }

        private GameStatus _status = GameStatus.Scheduled;
        public GameStatus Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged();
            }
        }

        private int _homeTeamScore = 0;
        public int HomeTeamScore
        {
            get => _homeTeamScore;
            set
            {
                if (_homeTeamScore == value) return;
                _homeTeamScore = value;
                OnPropertyChanged();
            }
        }

        private int _awayTeamScore = 0;
        public int AwayTeamScore
        {
            get => _awayTeamScore;
            set
            {
                if (_awayTeamScore == value) return;
                _awayTeamScore = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
