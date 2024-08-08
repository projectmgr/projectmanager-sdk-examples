namespace PmTask.SonarClient;

public class SonarHotspot
{
    public string key { get; set; }
    public string component { get; set; }
    public string project { get; set; }
    public string securityCategory { get; set; }
    public string vulnerabilityProbability { get; set; }
    public string status { get; set; }
    public int line { get; set; }
    public string message { get; set; }
    public string author { get; set; }
    public string creationDate { get; set; }
    public string updateDate { get; set; }
    public string ruleKey { get; set; }
    // textRange: startLine, endLine, startOffset, endOffset
    // flows: (?) an array
}
