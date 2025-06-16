namespace WebDriveBackend.Entities;

public class UserIdentity
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string PasswordSalt { get; set; }
    public string? RefreshToken { get; set; }
    public long? RefreshExpire { get; set; }
}