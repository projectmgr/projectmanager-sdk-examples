using ProjectManager.SDK;
using ProjectManager.SDK.Models;

namespace PmTask;

public static class PmHelper
{
    /// <summary>
    /// Find a project by its GUID, ShortID, or name
    /// </summary>
    /// <param name="projectsClient"></param>
    /// <param name="projectId"></param>
    public static async Task<ProjectDto?> FindProject(this ProjectManagerClient projectsClient, string? projectId)
    {
        var projects = (await projectsClient.Project.QueryProjects()).Data;
        if (projects == null)
        {
            Console.WriteLine("No projects found within this account.");
            return null;
        }

        // Fetch all projects and find the one that matches locally so we can give debugging information
        var project = projects.FirstOrDefault(project =>
            project.ShortId == projectId
            || string.Equals(project.Name, projectId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(project.Id.ToString(), projectId, StringComparison.OrdinalIgnoreCase));
        if (project != null)
        {
            return project;
        }
        
        // Provide some helpful information
        Console.WriteLine(
            $"Found {projects.Count()} project(s), but none with ID, shortID, or name '{projectId}'.");
        foreach (var item in projects)
        {
            Console.WriteLine($"    {item.ShortId} - {item.Name} ({item.Id})");
        }
        return null;
    }


}