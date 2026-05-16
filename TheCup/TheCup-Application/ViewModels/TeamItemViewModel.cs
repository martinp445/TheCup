using System.Windows.Input;
using TheCup_Application.Commands;
using TheCup_Application.Ports;

namespace TheCup_Application.ViewModels;

public sealed class TeamItemViewModel : ViewModel
{
    private readonly ITournamentRepository _repository;
    private readonly Guid _tournamentId;
    private readonly Action<string?> _setParentError;
    private readonly Action _onTeamChanged;
    private readonly Func<bool> _canModify;
    private string _name;
    private string _committedName;
    private bool _isRenaming;
    private bool _isRemoving;

    public TeamItemViewModel(
        Guid tournamentId,
        Guid teamId,
        string name,
        ITournamentRepository repository,
        Action<string?> setParentError,
        Action onTeamChanged,
        Func<bool> canModify)
    {
        _tournamentId = tournamentId;
        Id = teamId;
        _name = name;
        _committedName = name;
        _repository = repository;
        _setParentError = setParentError;
        _onTeamChanged = onTeamChanged;
        _canModify = canModify;

        CommitRenameCommand = new ActionCommand(
            () => _ = CommitRenameAsync(),
            () => _canModify() && !IsRenaming && !IsRemoving && IsDirty);

        RemoveTeamCommand = new ActionCommand(
            () => _ = RemoveAsync(),
            () => _canModify() && !IsRenaming && !IsRemoving);
    }

    public bool CanModify => _canModify();

    public void RefreshCommandStates() => RaiseCommandStates();

    public Guid Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                OnPropertyChanged(nameof(IsDirty));
                OnPropertyChanged(nameof(CanModify));
                RaiseCommandStates();
            }
        }
    }

    public bool IsDirty => !string.Equals(_name.Trim(), _committedName, StringComparison.Ordinal);

    public bool IsRenaming
    {
        get => _isRenaming;
        private set
        {
            if (SetProperty(ref _isRenaming, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool IsRemoving
    {
        get => _isRemoving;
        private set
        {
            if (SetProperty(ref _isRemoving, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public ICommand CommitRenameCommand { get; }

    public ICommand RemoveTeamCommand { get; }

    public async Task CommitRenameAsync()
    {
        if (!IsDirty || IsRenaming || IsRemoving)
        {
            return;
        }

        try
        {
            IsRenaming = true;
            _setParentError(null);

            await _repository.RenameTeamAsync(_tournamentId, Id, Name).ConfigureAwait(true);

            _committedName = Name.Trim();
            OnPropertyChanged(nameof(IsDirty));
            _onTeamChanged();
        }
        catch (Exception ex)
        {
            _setParentError(ex.Message);
            Name = _committedName;
        }
        finally
        {
            IsRenaming = false;
        }
    }

    private async Task RemoveAsync()
    {
        if (IsRenaming || IsRemoving)
        {
            return;
        }

        try
        {
            IsRemoving = true;
            _setParentError(null);

            await _repository.RemoveTeamAsync(_tournamentId, Id).ConfigureAwait(true);
            _onTeamChanged();
        }
        catch (Exception ex)
        {
            _setParentError(ex.Message);
        }
        finally
        {
            IsRemoving = false;
        }
    }

    private void RaiseCommandStates()
    {
        if (CommitRenameCommand is ActionCommand rename)
        {
            rename.RaiseCanExecuteChanged();
        }

        if (RemoveTeamCommand is ActionCommand remove)
        {
            remove.RaiseCanExecuteChanged();
        }
    }
}
