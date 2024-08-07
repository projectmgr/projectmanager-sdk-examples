using ProjectManager.SDK.Models;

namespace PmTask.Sync;

public class RemoteSystemTaskModel
{
    public string UniqueId { get; set; }
    public TaskCreateDto TaskCreate { get; set; }
}
