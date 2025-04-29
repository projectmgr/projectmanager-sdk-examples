using System.Security.Cryptography;
using System.Text;
using ProjectManager.SDK;
using ProjectManager.SDK.Models;

namespace PmTask.Clone;

public class AccountCloneHelper
{
    public static async Task CloneAccount(ProjectManagerClient src, ProjectManagerClient dest)
    {
        var map = new AccountMap();
        await CloneCustomers(src, dest, map);
        
        // Manager
        // what are these?
        // Project
        var projects = await src.Project.QueryProjects();
        Console.WriteLine($"Cloning {projects.Data.Length} projects");
        
        // Project Priority
        var projectPriorities = await src.ProjectPriority.RetrieveProjectPriorities();
        Console.WriteLine($"Cloning {projectPriorities.Data.Length} projectPriorities");
        
        // Project Charge Code
        var projectChargeCodes = await src.ProjectChargeCode.RetrieveChargeCodes();
        Console.WriteLine($"Cloning {projectChargeCodes.Data.Length} projectChargeCodes");
        
        // Project Folder
        var projectFolders = await src.ProjectFolder.RetrieveProjectFolders();
        Console.WriteLine($"Cloning {projectFolders.Data.Length} projectFolders");
        
        // Project Members
        //var projectId = Guid.Empty;
        //var projectMembers = await src.ProjectMembers.RetrieveProjectMembers(projectId, false);
        
        // Project Status
        var projectStatuses = await src.ProjectStatus.RetrieveProjectStatuses();
        Console.WriteLine($"Cloning {projectStatuses.Data.Length} projectStatuses");

        // Project Field
        var projectFields = await src.ProjectField.RetrieveProjectFields();
        Console.WriteLine($"Cloning {projectFields.Data.Length} projectFields");

        // Project Field Value
        //var projectFieldValues = await src.ProjectField.RetrieveAllProjectFieldValues(projectId);
        
        // Resource
        var resources = await src.Resource.QueryResources();
        Console.WriteLine($"Cloning {resources.Data.Length} resources");

        // Skills
        // Resource Skill
        var resourceSkills = await src.ResourceSkill.RetrieveResourceSkills();
        Console.WriteLine($"Cloning {resourceSkills.Data.Length} resourceSkills");

        // Teams
        // Resource Team
        var resourceTeams = await src.ResourceTeam.RetrieveResourceTeams();
        Console.WriteLine($"Cloning {resourceTeams.Data.Length} resourceTeams");

        // Tags
        var tags = await src.Tag.QueryTags();
        Console.WriteLine($"Cloning {tags.Data.Length} tags");

        // Tasks
        var tasks = await src.Task.QueryTasks();
        Console.WriteLine($"Cloning {tasks.Data.Length} tasks");

        // Task Priority
        // Task Status
        //var taskStatus = await src.TaskStatus.RetrieveTaskStatuses(projectId);
        
        // Task Tag
        //var taskTags = await src.TaskTag.ReplaceTaskTags(taskId);
        // Task ToDo
        //var taskToDos = await src.TaskTodo.GetTodos(taskId);
        // Task Field
        var taskFields = await src.TaskField.QueryTaskFields();
        Console.WriteLine($"Cloning {taskFields.Data.Length} taskFields");

        // Task Field Value
        //var taskFieldValues = await src.TaskField.RetrieveAllTaskFieldValues(taskId);
        // Timesheet
        var timesheets = await src.Timesheet.QueryTimeSheets();
        Console.WriteLine($"Cloning {timesheets.Data.Length} timesheets");
    }

    private static async Task CloneCustomers(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        // Source
        var srcCustomers = await src.ProjectCustomer.RetrieveProjectCustomers();
        if (!srcCustomers.Success)
        {
            throw new Exception($"Problem fetching customers from source: {srcCustomers.Error.Message}");
        }

        Console.Write($"Cloning {srcCustomers.Data.Length} customers... ");

        // Dest
        var destCustomers = await dest.ProjectCustomer.RetrieveProjectCustomers();
        if (!destCustomers.Success)
        {
            throw new Exception($"Problem fetching customers from destination: {destCustomers.Error.Message}");
        }

        // Execute the sync
        var results = await SyncData<ProjectCustomerDto>(srcCustomers.Data, destCustomers.Data, map,
            c => c.Name,
            c => c.Id?.ToString() ?? string.Empty,
            async c =>
            {
                c.Id = Guid.Empty;
                var created = await dest.ProjectCustomer.CreateProjectCustomer(new ProjectCustomerCreateDto()
                {
                    Name = c.Name
                });
                if (!created.Success || !created.Data.Id.HasValue)
                {
                    throw new Exception($"Project customer not created: {created.Error.Message}");
                }

                return created.Data.Id.Value.ToString();
            },
            (cSrc, cDest) => cSrc.Name == cDest.Name, async (cSrc, cDest) =>
            {
                if (!cDest.Id.HasValue)
                {
                    throw new Exception("Attempted to delete object with null ID");
                }
                var updateResult = await dest.ProjectCustomer.UpdateProjectCustomer(cDest.Id.Value, new ProjectCustomerCreateDto() { Name = cSrc.Name });
                if (!updateResult.Success)
                {
                    throw new Exception($"Project customer not updated: {updateResult.Error.Message}");
                }
            },
            async (c) =>
            {
                if (!c.Id.HasValue)
                {
                    throw new Exception("Attempted to delete object with null ID");
                }
                var deleteResult = await dest.ProjectCustomer.DeleteProjectCustomer(c.Id.Value);
                if (!deleteResult.Success)
                {
                    throw new Exception($"Project customer not deleted: {deleteResult.Error.Message}");
                }
            });
        Console.WriteLine(results.ToString());
    }

    private class SyncResults
    {
        public int Creates { get; set; }
        public int Updates { get; set; }
        public int Deletes { get; set; }

        public override string ToString()
        {
            if (Creates + Updates + Deletes == 0)
            {
                return "No changes.";
            }
            var sb = new StringBuilder();
            if (Creates > 0)
            {
                sb.Append($"Created {Creates}, ");
            }
            if (Updates > 0)
            {
                sb.Append($"Updated {Updates}, ");
            }
            if (Deletes > 0)
            {
                sb.Append($"Deleted {Deletes}, ");
            }

            sb.Length -= 2;
            return sb.ToString();
        }
    }
    
    private static async Task<SyncResults> SyncData<T>(T[] src, T[] dest, AccountMap map, 
        Func<T, string> identityFunc, 
        Func<T, string> primaryKeyFunc,
        Func<T, Task<string>> createFunc, 
        Func<T, T, bool> compareFunc,
        Func<T, T, Task> updateFunc, 
        Func<T, Task> deleteFunc)
    {
        var results = new SyncResults();
        
        // Convert our destination list to a dictionary for fast lookup
        var destMap = dest.ToDictionary(d => identityFunc(d));
        var destKeyMap = dest.ToDictionary(d => primaryKeyFunc(d));
        var keysToDelete = dest.Select(d => primaryKeyFunc(d)).ToList();
        foreach (var item in src)
        { 
            var identityString = identityFunc(item);
            var primaryKeyString = primaryKeyFunc(item);
            if (destMap.TryGetValue(identityString, out var matchingItem))
            {
                // Since this key matches and is valid, don't delete it after sync completes
                keysToDelete.Remove(primaryKeyFunc(matchingItem));
                if (!compareFunc(item, matchingItem))
                {
                    await updateFunc(item, matchingItem);
                    results.Updates++;
                }
            }
            else
            {
                var newPrimaryKey = await createFunc(item);
                results.Creates++;
                map.Items.Add(new AccountMap.AccountMapItem()
                {
                    Category = nameof(T), 
                    Identity = identityString, 
                    OriginalPrimaryKey = primaryKeyString,
                    NewPrimaryKey = newPrimaryKey,
                });
            }
        }
        
        // Delete other items that shouldn't continue to exist
        foreach (var key in keysToDelete)
        {
            var itemToDelete = destKeyMap[key];
            await deleteFunc(itemToDelete);
            results.Deletes++;
        }

        return results;
    }
}