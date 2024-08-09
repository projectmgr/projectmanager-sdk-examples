namespace PmTask.SonarClient;

public class SonarHotspot
{
    public string key { get; set; } = default!;
    public string component { get; set; } = default!;
    public string project { get; set; } = default!;
    public string securityCategory { get; set; } = default!;
    public string vulnerabilityProbability { get; set; } = default!;
    public string status { get; set; } = default!;
    public int line { get; set; } = 0;
    public string message { get; set; } = default!;
    public string author { get; set; } = default!;
    public string creationDate { get; set; } = default!;
    public string updateDate { get; set; } = default!;
    public string ruleKey { get; set; } = default!;
    // textRange: startLine, endLine, startOffset, endOffset
    // flows: (?) an array
}
