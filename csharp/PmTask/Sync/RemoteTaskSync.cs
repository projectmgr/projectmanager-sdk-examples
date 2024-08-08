using ProjectManager.SDK;
using ProjectManager.SDK.Models;

namespace PmTask.Sync;

public class RemoteTaskSync
{
    private static async Task<ProjectDto?> FindProject(ProjectManagerClient projectsClient, string? projectId)
    {
        var projects = (await projectsClient.Project.QueryProjects(10, 0, $"contains(Name, '{projectId}') OR shortCode eq '{projectId}' OR ShortId eq '{projectId}'"));
        if (projects == null || !projects.Success)
        {
            Console.WriteLine($"API call failed: {projects?.Error.Message}.");
            return null;
        }
        if (projects.Data == null || projects.Data.Length == 0)
        {
            Console.WriteLine($"No projects found matching the name '{projectId}' within this account.");
            return null;
        }

        if (projects.Data.Length > 1)
        {
            Console.WriteLine($"Found multiple projects matching the name '{projectId}' in ProjectManager.com.");
            Console.WriteLine("Please specify exact product ID:");
            foreach (var item in projects.Data)
            {
                Console.WriteLine($"    {item.ShortId} - {item.Name} ({item.Id})");
            }

            return null;
        }

        return projects.Data.FirstOrDefault();
    }

    public static async Task<SyncResult> SyncRemoteTasks(List<RemoteSystemTaskModel> tasks, ProjectManagerClient client,
        string pmProjectId)
    {
        // Find the ProjectManager project we're talking about
        var pmProject = await FindProject(client, pmProjectId);
        if (pmProject == null || !pmProject.Id.HasValue)
        {
            Console.WriteLine($"Unable to find ProjectManager project named '{pmProjectId}'.");
            return new SyncResult() { Success = false };
        }

        // Retrieve all tasks within this project
        var existingTasks = await client.LoadTasks($"ProjectId eq {pmProject.Id}");
        if (existingTasks == null)
        {
            Console.WriteLine($"Unable to load tasks for ProjectManager project '{pmProjectId}'.");
            return new SyncResult() { Success = false };
        }

        // Our goal is to maintain a "sync" between the two systems.
        // By default, all tasks in PM are expected to be deleted UNLESS they also appear in the remote system.
        Console.WriteLine($"Found {existingTasks.Count} tasks in ProjectManager project '{pmProjectId}'.");
        var tasksToCreate = new List<TaskCreateDto>();
        var tasksToDelete = new List<TaskDto>();
        var tasksToUpdate = new Dictionary<Guid, TaskUpdateDto>();
        tasksToDelete.AddRange(existingTasks);

        // Go through all the tasks found in the remote system and sync them
        foreach (var item in tasks)
        {
            var matchedTask = existingTasks.FirstOrDefault(existing => existing.Description.Replace("\\_", "_").Replace("&amp;", "&").Contains(item.UniqueId));

            // Does this task already exist?
            if (matchedTask == null || !matchedTask.Id.HasValue)
            {
                Console.WriteLine($"Creating new task {item.TaskCreate.Name}");
                tasksToCreate.Add(item.TaskCreate);
            }
            else
            {
                // PM seems to do a bunch of cleansing to descriptions, which makes it hard to match up.
                var cleansedDescription = matchedTask.Description
                    .Replace("\\_", "_")
                    .Replace("&amp;", "&")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&#39;")
                    .Replace("\\*", "*");

                // Is the task's description still current?
                if (!cleansedDescription.StartsWith(item.TaskCreate.Description, StringComparison.OrdinalIgnoreCase))
                {
                    // Should always be true but let's be safe
                    if (matchedTask.Id.HasValue)
                    {
                        tasksToUpdate.Add(matchedTask.Id.Value,
                            new TaskUpdateDto() { Name = item.TaskCreate.Name, Description = item.TaskCreate.Description });
                    }
                }
                
                // Is this task assigned to the correct people?
                if (!AssigneesMatch(matchedTask.Assignees.Select(a => a.Id!.Value).ToArray(), item.TaskCreate.Assignees))
                {
                    var newAssignees = item.TaskCreate.Assignees.Select(a => new AssigneeUpsertDto() { Id = a })
                        .ToArray();
                    var result = await client.TaskAssignee.ReplaceTaskAssignees(matchedTask.Id.Value, newAssignees);
                    if (!result.Success)
                    {
                        Console.WriteLine(
                            $"Error assigning task {matchedTask.ShortId} to user {item.TaskCreate.Assignees}: {result.Error.Message}");
                    }
                }

                // This task is still valid, as far as SonarCloud is concerned
                tasksToDelete.Remove(matchedTask);
            }
        }

        // Create new tasks
        if (tasksToCreate.Count > 0)
        {
            Console.WriteLine($"Creating {tasksToCreate.Count} new tasks...");
            try
            {
                var createTasksResult = await client.Task.CreateManyTasks(pmProject.Id.Value, tasksToCreate.ToArray());
                if (!createTasksResult.Success)
                {
                    Console.WriteLine($"Could not create tasks: {createTasksResult.Error.Message}");
                    return new SyncResult() { Success = false };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when creating tasks: {ex.Message}");
                return new SyncResult() { Success = false };
            }
        }

        // Close tasks that are no longer in the StillExistingTasks list
        if (tasksToDelete.Count > 0)
        {
            Console.WriteLine($"Deleting {tasksToDelete.Count} closed tasks...");
            try
            {
                foreach (var task in tasksToDelete)
                {
                    if (task.Id.HasValue)
                    {
                        var result = await client.Task.DeleteTask(task.Id.Value);
                        if (!result.Success)
                        {
                            Console.WriteLine($"Unable to remove task {task.ShortId}: {result.Error.Message}");
                        }
                        else
                        {
                            Console.WriteLine($"Removed completed task {task.ShortId} ({task.Name}).");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when deleting closed tasks: {ex.Message}");
                return new SyncResult() { Success = false };
            }
        }

        // Update tasks whose descriptions have changed
        if (tasksToUpdate.Count > 0)
        {
            Console.WriteLine($"Updating {tasksToUpdate.Count} modified tasks...");
            try
            {
                foreach (var kvp in tasksToUpdate)
                {
                    var response = await client.Task.UpdateTask(kvp.Key, kvp.Value);
                    if (!response.Success)
                    {
                        Console.WriteLine($"Error updating task {kvp.Key} - {response.Error.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when updating tasks: {ex.Message}");
                return new SyncResult() { Success = false };
            }
        }

            // Things succeeded!
        return new SyncResult()
        {
            Success = true,
            TasksCreated = tasksToCreate.Count,
            TasksDeleted = tasksToDelete.Count,
            TasksUpdated = tasksToUpdate.Count,
        };
    }

    private static bool AssigneesMatch(Guid[] listOne, Guid[] listTwo)
    {
        // Trivial case if lists are empty
        if (listOne.Length == 0 && listTwo.Length == 0)
        {
            return true;
        }

        // If list length differs, they don't match
        if (listOne.Length != listTwo.Length)
        {
            return false;
        }

        // Okay, there is a nonzero number of items in the list, let's make sure they match by sorting and checking
        var sortedOne = new List<Guid>(listOne);
        sortedOne.Sort();
        var sortedTwo = new List<Guid>(listTwo);
        sortedTwo.Sort();
        for (int i = 0; i < listOne.Length; i++)
        {
            if (sortedOne[i] != sortedTwo[i])
            {
                return false;
            }
        }

        // Lists match
        return true;
    }
}
