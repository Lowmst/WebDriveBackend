namespace WebDriveBackend.Services;

public interface IMinioService
{
    public Task<UploadResult> UploadFileObject(string name, Stream data, long size, string bucket);
    public Task<DownloadResult> DownloadFileObject(string name, string bucket);
    public Task<DeleteResult> DeleteFileObject(string name, string bucket);
}