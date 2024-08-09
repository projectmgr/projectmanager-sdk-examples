namespace PmTask.SonarClient;

public class SonarIssueResponse
{
    public SonarPagination paging { get; set; } = new();
    public SonarIssue[] issues { get; set; } = [];
}
