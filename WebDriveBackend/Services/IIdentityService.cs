using WebDriveBackend.Models;

namespace WebDriveBackend.Services;

public interface IIdentityService
{
    public RegisterResult Register(UserRegister userRegister);
    public LoginResult Login(UserLogin userLogin);
    public AuthenticateResult Authenticate(HttpContext httpContext);
    public RefreshResult RefreshToken(string refreshToken);

    public void Logout(string id);
    public ChangePasswordResult ChangePassword(string id, string oldPassword, string newPassword);
    public ChangeEmailResult ChangeEmail(string id, string newEmail);
}