namespace PmTask.SonarClient;

public class SonarHotspotResponse
{
    public SonarPagination paging { get; set; }
    public SonarHotspot[] hotspots { get; set; }
}
