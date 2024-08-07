using ProjectManager.SDK.Models;

namespace PmTask.Sync;

public class RemoteSystemTaskModel
{
    public string UniqueId { get; set; } = default!;
    public TaskCreateDto TaskCreate { get; set; } = default!;
}
