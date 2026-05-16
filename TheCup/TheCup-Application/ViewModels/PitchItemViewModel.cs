using System.Windows.Input;
using TheCup_Application.Commands;
using TheCup_Application.Ports;

namespace TheCup_Application.ViewModels;

public sealed class PitchItemViewModel : ViewModel
{
    private readonly ITournamentRepository _repository;
    private readonly Guid _tournamentId;
    private readonly Action<string?> _setParentError;
    private readonly Action _onPitchChanged;
    private readonly Func<bool> _canRemove;
    private readonly Func<bool> _canModify;
    private string _name;
    private string _committedName;
    private bool _isRenaming;
    private bool _isRemoving;

    public PitchItemViewModel(
        Guid tournamentId,
        Guid pitchId,
        string name,
        ITournamentRepository repository,
        Action<string?> setParentError,
        Action onPitchChanged,
        Func<bool> canRemove,
        Func<bool> canModify)
    {
        _tournamentId = tournamentId;
        Id = pitchId;
        _name = name;
        _committedName = name;
        _repository = repository;
        _setParentError = setParentError;
        _onPitchChanged = onPitchChanged;
        _canRemove = canRemove;
        _canModify = canModify;

        CommitRenameCommand = new ActionCommand(
            () => _ = CommitRenameAsync(),
            () => _canModify() && !IsRenaming && !IsRemoving && IsDirty);

        RemovePitchCommand = new ActionCommand(
            () => _ = RemoveAsync(),
            () => _canModify() && !IsRenaming && !IsRemoving && _canRemove());
    }

    public Guid Id { get; }

    public bool CanModify => _canModify();

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                OnPropertyChanged(nameof(IsDirty));
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

    public ICommand RemovePitchCommand { get; }

    public void RefreshCommandStates() => RaiseCommandStates();

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

            await _repository.RenamePitchAsync(_tournamentId, Id, Name).ConfigureAwait(true);

            _committedName = Name.Trim();
            OnPropertyChanged(nameof(IsDirty));
            _onPitchChanged();
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
        if (IsRenaming || IsRemoving || !_canRemove())
        {
            return;
        }

        try
        {
            IsRemoving = true;
            _setParentError(null);

            await _repository.RemovePitchAsync(_tournamentId, Id).ConfigureAwait(true);
            _onPitchChanged();
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

        if (RemovePitchCommand is ActionCommand remove)
        {
            remove.RaiseCanExecuteChanged();
        }
    }
}
