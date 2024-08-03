using ProjectManager.SDK;
using ProjectManager.SDK.Models;

namespace PmTask;

public static class PmHelper
{
    private const int CHUNK_SIZE = 1000;

    /// <summary>
    /// Find a single task matching a specific filter
    /// </summary>
    public static async Task<TaskDto?> FindOneTask(this ProjectManagerClient client, string? filter)
    {
        var tasks = await LoadTasks(client, filter);
        if (tasks == null)
        {
            return null;
        }
        if (tasks.Count == 0)
        {
            Console.WriteLine("Found no matching tasks.");
            return null;
        }
        if (tasks.Count > 1)
        {
            Console.WriteLine("Found multiple matches:");
            foreach (var item in tasks)
            {
                Console.WriteLine($"* {item.ShortId} - {item.Name}");
            }

            return null;
        }
        
        // Okay we got just one project
        return tasks[0];
    }
    
    /// <summary>
    /// Find a single project matching a specific filter
    /// </summary>
    /// <param name="client"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static async Task<ProjectDto?> FindOneProject(this ProjectManagerClient client, string? filter)
    {
        var projects = await LoadProjects(client, filter);
        if (projects == null)
        {
            return null;
        }
        if (projects.Count == 0)
        {
            Console.WriteLine("Found no matching projects.");
            return null;
        }
        if (projects.Count > 1)
        {
            Console.WriteLine("Found multiple matches:");
            foreach (var item in projects)
            {
                Console.WriteLine($"* {item.ShortId} - {item.ShortCode} - {item.Name}");
            }

            return null;
        }
        
        // Okay we got just one project
        return projects[0];
    }

    /// <summary>
    /// Loads in a collection of projects using pagination
    /// </summary>
    /// <param name="client"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static async Task<List<ProjectDto>?> LoadProjects(this ProjectManagerClient client, string? filter)
    {
        var list = new List<ProjectDto>();
        while (true)
        {
            var projects = await client.Project.QueryProjects(CHUNK_SIZE, list.Count, filter);
            if (!projects.Success)
            {
                Console.WriteLine($"Error querying projects: {projects.Error.Message}");
                return null;
            }

            list.AddRange(projects.Data);

            if (projects.Data.Length < CHUNK_SIZE)
            {
                return list;
            }
        }
    }
    
    /// <summary>
    /// Loads in a collection of tasks using pagination
    /// </summary>
    /// <param name="client"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static async Task<List<TaskDto>?> LoadTasks(this ProjectManagerClient client, string? filter)
    {
        var list = new List<TaskDto>();
        while (true)
        {
            var tasks = await client.Task.QueryTasks(CHUNK_SIZE, list.Count, filter);
            if (!tasks.Success)
            {
                Console.WriteLine($"Error querying tasks: {tasks.Error.Message}");
                return null;
            }

            list.AddRange(tasks.Data);
            
            if (tasks.Data.Length < CHUNK_SIZE)
            {
                return list;
            }
        }
    }
}