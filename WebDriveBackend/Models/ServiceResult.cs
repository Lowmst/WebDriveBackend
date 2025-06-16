namespace WebDriveBackend.Models;

public class ServiceResult
{
    public bool Succeeded { get; set; }
    public List<string> ResultValue { get; set; } = new();

    public List<string> Errors { get; set; } = new();
    // public string Errors { get; set; }
}