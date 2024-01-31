using ProjectManager.SDK;
using ProjectManager.SDK.Models;

namespace PmTask;

public static class PmHelper
{
    public static async Task<TaskDto?> FindTask(this ProjectManagerClient client, string? taskId)
    {
        var tasks = await client.Task.QueryTasks(filter: $"ShortId eq '{taskId}'");
        if (tasks == null || !tasks.Success)
        {
            Console.WriteLine($"API call failed: {tasks?.Error.Message}.");
            return null;
        }
        if (tasks.Data.Length != 1) {
            Console.WriteLine($"No task found with ID {taskId}.");
            return null;
        }
        return tasks.Data.FirstOrDefault();
    }
    
    public static async Task<ProjectDto?> FindProject(this ProjectManagerClient projectsClient, string? projectId)
    {
        var projects = (await projectsClient.Project.QueryProjects());
        if (projects == null || !projects.Success)
        {
            Console.WriteLine($"API call failed: {projects?.Error.Message}.");
            return null;
        }
        if (projects.Data == null || projects.Data.Length == 0)
        {
            Console.WriteLine("No projects found within this account.");
            return null;
        }

        // Fetch all projects and find the one that matches locally so we can give debugging information
        var project = projects.Data.FirstOrDefault(project =>
            project.ShortId == projectId
            || string.Equals(project.Name, projectId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(project.Id.ToString(), projectId, StringComparison.OrdinalIgnoreCase));
        if (project != null)
        {
            return project;
        }
        
        // Provide some helpful information
        Console.WriteLine(
            $"Found {projects.Data.Count()} project(s), but none with ID, shortID, or name '{projectId}'.");
        foreach (var item in projects.Data)
        {
            Console.WriteLine($"    {item.ShortId} - {item.Name} ({item.Id})");
        }
        return null;
    }


}