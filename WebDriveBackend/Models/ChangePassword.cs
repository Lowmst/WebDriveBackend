namespace WebDriveBackend.Models;

public class ChangePassword
{
    public required string OldPassword { get; set; }
    public required string NewPassword { get; set; }
}