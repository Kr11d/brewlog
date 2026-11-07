using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Login;

/// <summary>Вхід за логіном і паролем; за бажанням запам'ятовує вхід, щоб наступний запуск минав це вікно.</summary>
public sealed partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _auth;
    private readonly CurrentSession _session;
    private readonly RememberMeStore _rememberMeStore;

    [ObservableProperty]
    private string _login = string.Empty;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public LoginViewModel(AuthService auth, CurrentSession session, RememberMeStore rememberMeStore)
    {
        _auth = auth;
        _session = session;
        _rememberMeStore = rememberMeStore;
    }

    /// <summary>Вхід вдався, сесію відкрито: вікно можна змінити на головне.</summary>
    public event Action? SignedIn;

    /// <summary>Пароль приходить із PasswordBox через code-behind: його властивість Password не підтримує прив'язку.</summary>
    public string Password { private get; set; } = string.Empty;

    /// <summary>Підказка з демо-логінами (HANDOFF §3.3); є лише в Debug-збірці.</summary>
    public string DemoHint { get; } =
#if DEBUG
        "Демо: brewer / brewer123 · assistant / assistant123 · owner / owner123";
#else
        string.Empty;
#endif

    public bool HasDemoHint => DemoHint.Length > 0;

    [RelayCommand]
    private void SignIn()
    {
        if (string.IsNullOrWhiteSpace(Login) || Password.Length == 0)
        {
            ErrorMessage = "Введіть логін і пароль.";
            return;
        }

        try
        {
            var user = _auth.Login(Login, Password);
            if (RememberMe)
            {
                _rememberMeStore.Save(user.Id, _auth.IssueRememberToken(user));
            }

            ErrorMessage = string.Empty;
            _session.SignIn(user);
            SignedIn?.Invoke();
        }
        catch (DomainException exception)
        {
            ErrorMessage = exception.Message;
        }
    }
}
