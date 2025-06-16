namespace WebDriveBackend.Services;

public class AdminService : IAdminService
{
    public string GenerateAdminPass()
    {
        return Util.GenerateSalt();
    }
}