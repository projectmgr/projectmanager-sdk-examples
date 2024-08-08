namespace PmTask.SonarClient;

public class SonarIssue
{
    public string key { get; set; }
    public string rule { get; set; }
    public string severity { get; set; }
    public string component { get; set; }
    public string project { get; set; }
    public int  line { get; set; }
    public string status { get; set; }
    public string message { get; set; }
    public string author { get; set; }
    public string creationDate { get; set; }
    public string updateDate { get; set; }
    public string type { get; set; }
}
