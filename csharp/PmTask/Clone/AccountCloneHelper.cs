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
        var srcCustomers = await src.ProjectCustomer.RetrieveProjectCustomers()
            .ThrowOnError("Fetching customers from source");
        Console.Write($"Cloning {srcCustomers.Data.Length} customers... ");

        // Dest
        var destCustomers = await dest.ProjectCustomer.RetrieveProjectCustomers()
            .ThrowOnError("Fetching customers from destination");

        // Execute the sync
        var results = await SyncHelper.SyncData<ProjectCustomerDto>(srcCustomers.Data, destCustomers.Data, map,
            c => c.Name,
            c => c.Id?.ToString() ?? string.Empty,
            async c =>
            {
                c.Id = Guid.Empty;
                var created = await dest.ProjectCustomer.CreateProjectCustomer(new ProjectCustomerCreateDto()
                {
                    Name = c.Name
                }).ThrowOnError("Creating Project Customer");
                return created.Data.Id!.Value.ToString();
            },
            (cSrc, cDest) => cSrc.Name == cDest.Name, async (cSrc, cDest) =>
            {
                await dest.ProjectCustomer.UpdateProjectCustomer(cDest.Id!.Value, new ProjectCustomerCreateDto() { Name = cSrc.Name })
                    .ThrowOnError("Updating Project Customer");
            },
            async (c) =>
            {
                await dest.ProjectCustomer.DeleteProjectCustomer(c.Id!.Value)
                    .ThrowOnError("Deleting Project Customer");
            });
        Console.WriteLine(results.ToString());
    }

}