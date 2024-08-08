namespace PmTask.SonarClient;

public class SonarIssueResponse
{
    public SonarPagination paging { get; set; }
    public SonarIssue[] issues { get; set; }
}
