using System;
using System.Collections.Generic;
using System.Text;
using TheCup_Application.Ports;
using TheCup_Domain.Entities;

namespace TheCup_Application.ViewModels
{
    public class PlayoffViewModel : ViewModel, IHasStatusMessage
    {
        private readonly ITournamentRepository _repository;
        private Guid _tournamentId;
        private string? _errorMessage;
        private bool _isBusy;

        public PlayoffViewModel(ITournamentRepository repository) { 
            _repository = repository;
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                }
            }
        }

        public async Task LoadAsync(Guid tournamentId)
        {
            _tournamentId = tournamentId;

            var summary = await _repository.GetByIdAsync(tournamentId).ConfigureAwait(true);
            if (summary is null)
            {
                ErrorMessage = "Tournament was not found.";
                return;
            }
        }
    }
}
