using Minio;
using Minio.DataModel.Args;

namespace WebDriveBackend.Services;

public class MinioService(IMinioClient minio) : IMinioService
{
    public async Task<UploadResult> UploadFileObject(string name, Stream data, long size, string bucket)
    {
        try
        {
            var args = new PutObjectArgs()
                .WithObject(name)
                .WithObjectSize(size)
                .WithStreamData(data)
                .WithBucket(bucket);
            await minio.PutObjectAsync(args);
            return new UploadResult { Succeeded = true };
        }
        catch (Exception)
        {
            return new UploadResult { Succeeded = false, Errors = { "error upload" } };
        }
    }

    public async Task<DownloadResult> DownloadFileObject(string name, string bucket)
    {
        try
        {
            var file = new MemoryStream();
            var args = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(name)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(file);
                    file.Seek(0, SeekOrigin.Begin);
                });
            await minio.GetObjectAsync(args);
            return new DownloadResult { Succeeded = true, File = file };
        }
        catch (Exception)
        {
            return new DownloadResult { Succeeded = false, Errors = { "error download" } };
        }
    }

    public async Task<DeleteResult> DeleteFileObject(string name, string bucket)
    {
        try
        {
            var args = new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(name);
            await minio.RemoveObjectAsync(args);
            return new DeleteResult { Succeeded = true };
        }
        catch (Exception)
        {
            return new DeleteResult { Succeeded = false, Errors = { "error delete" } };
        }
    }
}

public class UploadResult
{
    public bool Succeeded { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class DownloadResult
{
    public bool Succeeded { get; set; }
    public MemoryStream? File { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class DeleteResult
{
    public bool Succeeded { get; set; }
    public List<string> Errors { get; set; } = new();
}