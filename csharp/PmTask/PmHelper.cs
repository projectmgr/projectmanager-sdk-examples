using ProjectManager.SDK;
using ProjectManager.SDK.Models;

namespace PmTask;

public static class PmHelper
{
    private const int CHUNK_SIZE = 1000;
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

            if (projects.Data.Length == 0)
            {
                return list;
            }

            list.AddRange(projects.Data);
        }
    }
    
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

            if (tasks.Data.Length == 0)
            {
                return list;
            }

            list.AddRange(tasks.Data);
        }
    }
}