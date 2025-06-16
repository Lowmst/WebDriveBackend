using WebDriveBackend.Entities;
using WebDriveBackend.Models;
using WebDriveBackend.Utils;

namespace WebDriveBackend.Services;

public class IdentityService(Database db) : IIdentityService
{
    public RegisterResult Register(UserRegister userRegister)
    {
        if (db.UsersIdentity.FirstOrDefault(user => user.Email == userRegister.Email) != null)
            return new RegisterResult { Succeeded = false, Errors = { "Email already register" } };

        var salt = Util.GenerateSalt();
        var id = Util.GenerateId();
        db.UsersIdentity.Add(new UserIdentity
        {
            Id = id,
            Email = userRegister.Email,
            PasswordHash = Util.HashPassword(userRegister.Password, salt),
            PasswordSalt = salt
        });
        db.UsersProfile.Add(new UserProfile { Id = id, Name = ""});
        db.SaveChanges();
        return new RegisterResult { Succeeded = true };
    }


    public LoginResult Login(UserLogin userLogin)
    {
        var user = db.UsersIdentity.FirstOrDefault(user => user.Email == userLogin.Email);
        if (user == null) return new LoginResult { Succeeded = false, Errors = { "User not found" } };

        if (user.PasswordHash != Util.HashPassword(userLogin.Password, user.PasswordSalt))
            return new LoginResult { Succeeded = false, Errors = { "Password error" } };

        var refresh = Util.GenerateRefreshToken();
        user.RefreshToken = refresh;
        user.RefreshExpire = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();
        db.UsersIdentity.Update(user);
        db.SaveChanges();
        return new LoginResult
            { Succeeded = true, Token = new Token { AccessToken = Jwt.Generate(user.Id), RefreshToken = refresh } };
    }

    public AuthenticateResult Authenticate(HttpContext httpContext)
    {
        var token = httpContext.Request.Headers.Authorization.ToString();
        if (!token.StartsWith("Bearer"))
            return new AuthenticateResult { Succeeded = false, Errors = { "invalid token" } };

        token = token[6..].Trim();

        var ret = Jwt.Verify(token);
        if (!ret.Succeeded) return new AuthenticateResult { Succeeded = false, Errors = ret.Errors };

        var user = db.UsersIdentity.FirstOrDefault(user => user.Id == ret.Id);
        if (user == null) return new AuthenticateResult { Succeeded = false, Errors = { "invalid user" } };

        return new AuthenticateResult { Succeeded = true, Id = user.Id };
    }

    public RefreshResult RefreshToken(string refreshToken)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var user = db.UsersIdentity.FirstOrDefault(user =>
            user.RefreshToken == refreshToken && user.RefreshExpire > now);
        if (user == null) return new RefreshResult { Succeeded = false, Errors = { "invalid refresh token" } };

        var refresh = Util.GenerateRefreshToken();
        user.RefreshToken = refresh;
        user.RefreshExpire = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();
        db.UsersIdentity.Update(user);
        db.SaveChanges();
        return new RefreshResult
            { Succeeded = true, Token = new Token { AccessToken = Jwt.Generate(user.Id), RefreshToken = refresh } };
    }

    // Authenticate require
    public void Logout(string id)
    {
        var user = db.UsersIdentity.FirstOrDefault(user => user.Id == id);
        user!.RefreshToken = null;
        user.RefreshExpire = null;
        db.UsersIdentity.Update(user);
        db.SaveChanges();
    }


    // Authenticate require
    public ChangePasswordResult ChangePassword(string id, string oldPassword, string newPassword)
    {
        var user = db.UsersIdentity.FirstOrDefault(user => user.Id == id);
        if (user!.PasswordHash != Util.HashPassword(oldPassword, user.PasswordSalt))
            return new ChangePasswordResult { Succeeded = false, Errors = { "Old password error" } };

        var salt = Util.GenerateSalt();
        user.PasswordHash = Util.HashPassword(newPassword, salt);
        user.PasswordSalt = salt;
        db.UsersIdentity.Update(user);
        db.SaveChanges();
        Logout(user.Id);
        return new ChangePasswordResult { Succeeded = true };
    }

    // Authenticate require
    public ChangeEmailResult ChangeEmail(string id, string newEmail)
    {
        var userInner = db.UsersIdentity.FirstOrDefault(user => user.Email == newEmail);
        if (userInner != null)
            return new ChangeEmailResult { Succeeded = false, Errors = { "New email address already register" } };

        var user = db.UsersIdentity.FirstOrDefault(user => user.Id == id);
        user!.Email = newEmail;
        db.UsersIdentity.Update(user);
        db.SaveChanges();
        
        return new ChangeEmailResult { Succeeded = true };
    }
}

public class Token
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}

public class RegisterResult
{
    public bool Succeeded { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class LoginResult
{
    public bool Succeeded { get; set; }
    public Token? Token { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class AuthenticateResult
{
    public bool Succeeded { get; set; }
    public string? Id { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class RefreshResult
{
    public bool Succeeded { get; set; }
    public Token? Token { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ChangePasswordResult
{
    public bool Succeeded { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ChangeEmailResult
{
    public bool Succeeded { get; set; }
    public List<string> Errors { get; set; } = new();
}