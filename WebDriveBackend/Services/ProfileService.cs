using WebDriveBackend.Entities;

namespace WebDriveBackend.Services;

public class ProfileService(Database db, IMinioService minioService) : IProfileService
{
    public string GetEmail(string id)
    {
        var user = db.UsersIdentity.FirstOrDefault(user => user.Id == id);
        return user!.Email;
    }

    public UploadResult SetAvatar(string id, Stream data, long size)
    {
        return minioService.UploadFileObject(id, data, size, "avatars").Result;
    }

    public DownloadResult GetAvatar(string id)
    {
        return minioService.DownloadFileObject(id, "avatars").Result;
    }


    // auth ..
    public void SetName(string id, string name)
    {
        var user = db.UsersProfile.FirstOrDefault(user => user.Id == id);
        user!.Name = name;
        db.Update(user);
        db.SaveChanges();
    }

    // auth ..
    public string? GetName(string id)
    {
        var user = db.UsersProfile.FirstOrDefault(user => user.Id == id);
        return user!.Name;
    }

    public UserIdentity Demo(string id)
    {
        var user = db.UsersIdentity.FirstOrDefault(user => user.Id == id);
        return user!;
    }
}