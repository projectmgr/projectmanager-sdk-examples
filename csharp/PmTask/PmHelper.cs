using ProjectManager.SDK;
using ProjectManager.SDK.Models;

namespace PmTask;

public static class PmHelper
{
    private const int CHUNK_SIZE = 1000;
    
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