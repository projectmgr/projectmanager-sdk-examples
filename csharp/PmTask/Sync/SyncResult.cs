namespace PmTask.Sync;

public class SyncResult
{
    public bool Success { get; set; }
    public int TasksCreated { get; set; }
    public int TasksDeleted { get; set; }
    public int TasksUpdated { get; set; }
}
