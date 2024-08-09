namespace PmTask.SonarClient;

public class SonarHotspotResponse
{
    public SonarPagination paging { get; set; } = new();
    public SonarHotspot[] hotspots { get; set; } = [];
}
