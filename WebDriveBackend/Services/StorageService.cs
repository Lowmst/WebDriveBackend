using WebDriveBackend.Entities;

namespace WebDriveBackend.Services;

public class StorageService(Database db, IMinioService minio): IStorageService
{
    public List<UserStorage> GetFileList(string id)
    {
        return db.UsersStorage.Where(file => file.User == id).ToList();
    }

    public DownloadResult DownloadFile(string fileId, string userId)
    {
        var file = db.UsersStorage.FirstOrDefault(file => file.Id == fileId);
        if (file == null) return new DownloadResult() { Succeeded = false, Errors = { "File not found" } };
        if (file.User != userId) return new DownloadResult() { Succeeded = false, Errors = { "permission error" } };
        return minio.DownloadFileObject(fileId, "storage").Result;
        
    }

    public UploadResult UploadFile(string userId, Stream data, string name, long size)
    {
        var fileId = Guid.NewGuid().ToString();
        db.UsersStorage.Add(new UserStorage { Id = fileId, User = userId, Name = name, Size = size });
        db.SaveChanges();
        return minio.UploadFileObject(fileId, data, size, "storage").Result;
    }

    public DeleteResult DeleteFile(string fileId, string userId)
    {
        var file = db.UsersStorage.FirstOrDefault(file => file.Id == fileId);
        if (file == null) return new DeleteResult() { Succeeded = false, Errors = { "File not found" } };
        if (file.User != userId) return new DeleteResult() { Succeeded = false, Errors = { "permission error" } };
        db.UsersStorage.Remove(file);
        db.SaveChanges();
        return minio.DeleteFileObject(fileId, "storage").Result;
    }
}