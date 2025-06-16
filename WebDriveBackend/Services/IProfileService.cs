using WebDriveBackend.Entities;

namespace WebDriveBackend.Services;

public interface IProfileService
{
    public string GetEmail(string id);
    public UploadResult SetAvatar(string id, Stream data, long size);

    public DownloadResult GetAvatar(string id);

    public void SetName(string id, string name);
    public string? GetName(string id);

    public UserIdentity Demo(string id);
}