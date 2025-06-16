using WebDriveBackend.Entities;

namespace WebDriveBackend.Services;

public interface IStorageService
{
    public List<UserStorage> GetFileList(string id);

    public DownloadResult DownloadFile(string fileId, string userId);

    public UploadResult UploadFile(string userId, Stream data, string name, long size);

    public DeleteResult DeleteFile(string fileId, string userId);
}