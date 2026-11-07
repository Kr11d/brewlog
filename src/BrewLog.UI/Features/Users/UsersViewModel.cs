using System.Globalization;
using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Users;

/// <summary>
/// Діалог «Користувачі»: список облікових записів із пошуком і форма вибраного або нового запису.
/// Усі правила (логін, пароль, «не себе», «лишається власник») перевіряє UserService; тут лише форма.
/// </summary>
public sealed partial class UsersViewModel : ObservableObject
{
    private readonly UserService _users;
    private readonly CurrentSession _session;
    private readonly DialogService _dialogs;
    private readonly WindowService _windows;

    private List<UserRowViewModel> _allRows = [];
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private bool _isDirty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyResult))]
    private IReadOnlyList<UserRowViewModel> _rows = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExisting), nameof(IsFormVisible))]
    private UserRowViewModel? _selectedRow;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExisting), nameof(IsFormVisible))]
    private bool _isNew;

    [ObservableProperty]
    private string _formTitle = string.Empty;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _login = string.Empty;

    [ObservableProperty]
    private RoleOption? _selectedRole;

    [ObservableProperty]
    private bool _isActive;

    public UsersViewModel(UserService users, CurrentSession session, DialogService dialogs, WindowService windows)
    {
        _users = users;
        _session = session;
        _dialogs = dialogs;
        _windows = windows;

        Roles = Enum.GetValues<UserRole>()
            .Select(role => new RoleOption(role, UserFactory.Create(role).RoleDisplayName))
            .ToList();

        LoadRows();
        if (Rows.Count > 0)
        {
            Select(Rows[0]);
        }
    }

    /// <summary>Просить вікно очистити поля паролів: PasswordBox не має прив'язки, тому це робить code-behind.</summary>
    public event Action? PasswordsCleared;

    public IReadOnlyList<RoleOption> Roles { get; }

    /// <summary>Пароль нового користувача; приходить із PasswordBox через code-behind.</summary>
    public string NewPassword
    {
        set
        {
            _newPassword = value;
            _isDirty |= value.Length > 0;
        }
    }

    public string ConfirmPassword
    {
        set
        {
            _confirmPassword = value;
            _isDirty |= value.Length > 0;
        }
    }

    public bool IsExisting => !IsNew && SelectedRow is not null;

    public bool IsFormVisible => IsNew || SelectedRow is not null;

    public bool IsEmptyResult => Rows.Count == 0;

    /// <summary>Викликається під час закриття вікна будь-яким способом: незбережені зміни потребують підтвердження.</summary>
    public bool ConfirmClose() =>
        !_isDirty || _dialogs.Confirm("Незбережені зміни буде втрачено.", "Закрити без збереження?", "Закрити", "Лишитися");

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnFullNameChanged(string value) => _isDirty = true;

    partial void OnLoginChanged(string value) => _isDirty = true;

    partial void OnSelectedRoleChanged(RoleOption? value) => _isDirty = true;

    partial void OnIsActiveChanged(bool value) => _isDirty = true;

    [RelayCommand]
    private void NewUser()
    {
        if (!ConfirmDiscard())
        {
            return;
        }

        MarkSelected(null);
        IsNew = true;
        FormTitle = "Новий користувач";
        FullName = string.Empty;
        Login = string.Empty;
        SelectedRole = null;
        IsActive = true;
        ClearPasswords();
        _isDirty = false;
    }

    [RelayCommand]
    private void Save()
    {
        var formError = FindFormError();
        if (formError is not null)
        {
            _dialogs.ShowError(formError);
            return;
        }

        var editedId = SelectedRow?.Id;
        try
        {
            var id = IsNew ? _users.Create(Login, FullName, SelectedRole!.Role, _newPassword).Id : SaveExisting(SelectedRow!.User);
            LoadRows();
            Select(_allRows.First(row => row.Id == id));
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);

            // Частина змін могла вже зберегтися: список показує стан БД, а введене у формі лишається.
            LoadRows();
        }
        finally
        {
            RefreshSessionUser(editedId);
        }
    }

    [RelayCommand]
    private void ResetPassword()
    {
        if (SelectedRow is { } row && _windows.ShowResetPassword(row.User))
        {
            _dialogs.ShowInfo($"Пароль для «{row.FullName}» змінено. Автоматичний вхід цього користувача скасовано.", "Пароль змінено");
        }
    }

    /// <summary>Натискання на рядок. Повторне натискання на вибраний рядок не скидає введеного.</summary>
    private void OnRowClicked(UserRowViewModel row)
    {
        if ((row != SelectedRow || IsNew) && ConfirmDiscard())
        {
            Select(row);
        }
    }

    private bool ConfirmDiscard() =>
        !_isDirty || _dialogs.Confirm("Незбережені зміни буде втрачено.", "Відкинути зміни?", "Відкинути", "Лишитися");

    private void Select(UserRowViewModel row)
    {
        MarkSelected(row);
        IsNew = false;
        FormTitle = row.FullName;
        FullName = row.FullName;
        Login = row.Login;
        SelectedRole = Roles.First(option => option.Role == row.User.Role);
        IsActive = row.User.IsActive;
        ClearPasswords();
        _isDirty = false;
    }

    /// <summary>Перевірки самої форми, яких немає в UserService: вибір ролі й збіг паролів.</summary>
    private string? FindFormError()
    {
        if (SelectedRole is null)
        {
            return "Оберіть роль.";
        }

        return IsNew && _newPassword != _confirmPassword ? "Паролі не збігаються." : null;
    }

    /// <summary>Зберігає лише змінене: ім'я й роль — одним викликом, активність — іншим.</summary>
    private int SaveExisting(User user)
    {
        var role = SelectedRole!.Role;
        if (FullName.Trim() != user.FullName || role != user.Role)
        {
            _users.Update(user.Id, FullName, role);
        }

        if (IsActive != user.IsActive)
        {
            _users.SetActive(user.Id, IsActive);
        }

        return user.Id;
    }

    /// <summary>Якщо змінено власний запис, оновлює його в сесії: оболонка одразу покаже нове ім'я.</summary>
    private void RefreshSessionUser(int? editedId)
    {
        if (editedId is not null && editedId == _session.User?.Id && _allRows.FirstOrDefault(row => row.Id == editedId) is { } row)
        {
            _session.SignIn(row.User);
        }
    }

    /// <summary>Перечитує список із БД, зберігаючи виділення; пошук застосовується наново.</summary>
    private void LoadRows()
    {
        var selectedId = SelectedRow?.Id;
        var byName = StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true);

        _allRows = _users.GetAll()
            .OrderBy(user => user.FullName, byName)
            .Select(user => new UserRowViewModel(user, OnRowClicked))
            .ToList();
        Summary = $"Облікових записів: {_allRows.Count}";

        MarkSelected(_allRows.FirstOrDefault(row => row.Id == selectedId));
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        Rows = search.Length == 0 ? _allRows : _allRows.Where(row => row.Matches(search)).ToList();
    }

    private void MarkSelected(UserRowViewModel? selected)
    {
        foreach (var row in _allRows)
        {
            row.IsSelected = row == selected;
        }

        SelectedRow = selected;
    }

    private void ClearPasswords()
    {
        _newPassword = string.Empty;
        _confirmPassword = string.Empty;
        PasswordsCleared?.Invoke();
    }
}
