namespace PmTask.SonarClient;

public class SonarIssue
{
    public string key { get; set; } = default!;
    public string rule { get; set; } = default!;
    public string severity { get; set; } = default!;
    public string component { get; set; } = default!;
    public string project { get; set; } = default!;
    public int line { get; set; } = 0;
    public string status { get; set; } = default!;
    public string message { get; set; } = default!;
    public string author { get; set; } = default!;
    public string creationDate { get; set; } = default!;
    public string updateDate { get; set; } = default!;
    public string type { get; set; } = default!;
}
