using ProjectManager.SDK;
using ProjectManager.SDK.Models;

namespace PmTask;

public static class PmHelper
{
    private const int CHUNK_SIZE = 1000;

    /// <summary>
    /// Find a single resource matching a specific filter
    /// </summary>
    public static async Task<ResourceDto?> FindOneResource(this ProjectManagerClient client, string? filter)
    {
        var items = await LoadResources(client, filter);
        if (items == null)
        {
            return null;
        }
        if (items.Count == 0)
        {
            Console.WriteLine("Found no matching resources.");
            return null;
        }
        if (items.Count > 1)
        {
            Console.WriteLine("Found multiple matches:");
            foreach (var item in items)
            {
                Console.WriteLine($"* {item.Email} - {item.FirstName} {item.LastName} ({item.Id})");
            }

            return null;
        }
        
        // Okay we got just one match
        return items[0];
    }
    
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
            var result = await client.Project.QueryProjects(CHUNK_SIZE, list.Count, filter);
            if (!result.Success)
            {
                Console.WriteLine($"Error querying projects: {result.Error.Message}");
                return null;
            }

            list.AddRange(result.Data);

            if (result.Data.Length < CHUNK_SIZE)
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
            var result = await client.Task.QueryTasks(CHUNK_SIZE, list.Count, filter);
            if (!result.Success)
            {
                Console.WriteLine($"Error querying tasks: {result.Error.Message}");
                return null;
            }

            list.AddRange(result.Data);
            
            if (result.Data.Length < CHUNK_SIZE)
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
    public static async Task<List<ResourceDto>?> LoadResources(this ProjectManagerClient client, string? filter)
    {
        var list = new List<ResourceDto>();
        while (true)
        {
            var result = await client.Resource.QueryResources(CHUNK_SIZE, list.Count, filter);
            if (!result.Success)
            {
                Console.WriteLine($"Error querying resources: {result.Error.Message}");
                return null;
            }

            list.AddRange(result.Data);
            
            if (result.Data.Length < CHUNK_SIZE)
            {
                return list;
            }
        }
    }
}