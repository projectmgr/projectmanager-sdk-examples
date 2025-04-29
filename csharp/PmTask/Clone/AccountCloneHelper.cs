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
        await CloneProjectPriorities(src, dest, map);
        await CloneProjectChargeCodes(src, dest, map);
        await CloneProjectFolders(src, dest, map);
        await CloneProjectStatuses(src, dest, map);
        await CloneProjectFields(src, dest, map);
        await CloneResourceSkills(src, dest, map);
        
        // Manager
        // what are these?
        // Project
        var projects = await src.Project.QueryProjects();
        Console.WriteLine($"Cloning {projects.Data.Length} projects");
        
        // Project Members
        //var projectId = Guid.Empty;
        //var projectMembers = await src.ProjectMembers.RetrieveProjectMembers(projectId, false);
        
        // Project Field Value
        //var projectFieldValues = await src.ProjectField.RetrieveAllProjectFieldValues(projectId);
        
        // Resource
        var resources = await src.Resource.QueryResources();
        Console.WriteLine($"Cloning {resources.Data.Length} resources");


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

    private static async Task CloneResourceSkills(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        // Resource Skill
        var srcResourceSkills = await src.ResourceSkill.RetrieveResourceSkills().ThrowOnError("Fetching from source");
        var destResourceSkills =
            await dest.ResourceSkill.RetrieveResourceSkills().ThrowOnError("Fetching from destination");
        Console.Write($"Cloning {srcResourceSkills.Data.Length} resourceSkills... ");

        var results = await SyncHelper.SyncData(srcResourceSkills.Data, destResourceSkills.Data, map,
            s => s.Name,
            s => s.Id!.Value.ToString(),
            async s =>
            {
                var result = await dest.ResourceSkill
                    .CreateResourceSkill(new CreateResourceSkillDto() { Name = s.Name }).ThrowOnError("Creating");
                return result.Data.Id!.Value.ToString();
            },
            (s1, s2) =>
            {
                return s1.Name == s2.Name;
            },
            async (srcSkill, destSkill) =>
            {
                var result = await dest.ResourceSkill
                    .UpdateResourceSkill(destSkill.Id!.Value, new UpdateResourceSkillDto() { Name = srcSkill.Name })
                    .ThrowOnError("Updating");
            },
            async s =>
            {
                await dest.ResourceSkill.DeleteResourceSkill(s.Id!.Value).ThrowOnError("Deleting");
            }
        );
        Console.WriteLine(results);
    }

    private static async Task CloneProjectFields(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        // Project Field
        var srcProjectFields = await src.ProjectField.RetrieveProjectFields().ThrowOnError("Fetching from source");
        var destProjectFields =
            await dest.ProjectField.RetrieveProjectFields().ThrowOnError("Fetching from destination");
        Console.Write($"Cloning {srcProjectFields.Data.Length} projectFields... ");

        var results = await SyncHelper.SyncData(srcProjectFields.Data, destProjectFields.Data, map,
            pf => pf.Name,
            pf => pf.Id!.Value.ToString(),
            async pf =>
            {
                var result = await dest.ProjectField.CreateProjectField(new ProjectFieldCreateDto()
                        { Name = pf.Name, Options = pf.Options, ShortId = pf.ShortId, Type = pf.Type })
                    .ThrowOnError("Creating");
                return result.Data.Id!.Value.ToString();
            }, 
            (srcPf, destPf) =>
            {
                return srcPf.Name == destPf.Name
                       && srcPf.Options == destPf.Options
                       && srcPf.Type == destPf.Type
                       && srcPf.ShortId == destPf.ShortId;
            }, 
            null, // no updates available for project fields 
            async pf =>
            {
                await dest.ProjectField.DeleteProjectField(pf.Id!.Value.ToString()).ThrowOnError("Deleting");
            });
        Console.WriteLine(results);
    }

    private static async Task CloneProjectStatuses(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        // Project statuses are not currently create-able from the API
        
        // Project Status
        // var srcProjectStatuses = await src.ProjectStatus.RetrieveProjectStatuses().ThrowOnError("Fetching from source");
        // var destProjectStatuses =
        //     await dest.ProjectStatus.RetrieveProjectStatuses().ThrowOnError("Fetching from destination");
        // Console.WriteLine($"Cloning {srcProjectStatuses.Data.Length} projectStatuses");
    }

    private static async Task CloneProjectFolders(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        // Project folders are not currently create-able from the API
        
        // Project Folder
        // var srcProjectFolders = await src.ProjectFolder.RetrieveProjectFolders().ThrowOnError("Fetching from source");
        // var destProjectFolders =
        //     await dest.ProjectFolder.RetrieveProjectFolders().ThrowOnError("Fetching from destination");
        // Console.WriteLine($"Cloning {projectFolders.Data.Length} projectFolders");
    }

    private static async Task CloneProjectChargeCodes(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        // Project Charge Codes are not currently create-able from the API

        // Project Charge Code
        // var srcProjectChargeCodes = await src.ProjectChargeCode.RetrieveChargeCodes().ThrowOnError("Fetching from source");
        // var destProjectChargeCodes =
        //     await dest.ProjectChargeCode.RetrieveChargeCodes().ThrowOnError("Fetching from destination");
        // Console.Write($"Cloning {srcProjectChargeCodes.Data.Length} projectChargeCodes... ");
    }

    private static async Task CloneProjectPriorities(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        // Project Priorities are not currently create-able from the API
        
        
        // var srcProjectPriorities = await src.ProjectPriority.RetrieveProjectPriorities()
        //     .ThrowOnError("Fetching from source");
        // Console.Write($"Cloning {srcProjectPriorities.Data.Length} projectPriorities... ");
        // var destProjectPriorities =
        //     await dest.ProjectPriority.RetrieveProjectPriorities().ThrowOnError("Fetching from destination");
        //
        // // Execute the sync
        // var results = await SyncHelper.SyncData(srcProjectPriorities.Data, destProjectPriorities.Data, map, 
        //     p => p.Name,
        //     p => p.Id?.ToString() ?? string.Empty,
    }

    private static async Task CloneCustomers(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        var srcCustomers = await src.ProjectCustomer.RetrieveProjectCustomers()
            .ThrowOnError("Fetching from source");
        Console.Write($"Cloning {srcCustomers.Data.Length} customers... ");
        var destCustomers = await dest.ProjectCustomer.RetrieveProjectCustomers()
            .ThrowOnError("Fetching from destination");

        // Execute the sync
        var results = await SyncHelper.SyncData<ProjectCustomerDto>(srcCustomers.Data, destCustomers.Data, map,
            c => c.Name,
            c => c.Id!.Value.ToString(),
            async c =>
            {
                var created = await dest.ProjectCustomer.CreateProjectCustomer(new ProjectCustomerCreateDto()
                {
                    Name = c.Name
                }).ThrowOnError("Creating");
                return created.Data.Id!.Value.ToString();
            },
            (cSrc, cDest) => cSrc.Name == cDest.Name, async (cSrc, cDest) =>
            {
                await dest.ProjectCustomer.UpdateProjectCustomer(cDest.Id!.Value, new ProjectCustomerCreateDto() { Name = cSrc.Name })
                    .ThrowOnError("Updating");
            },
            async (c) =>
            {
                await dest.ProjectCustomer.DeleteProjectCustomer(c.Id!.Value)
                    .ThrowOnError("Deleting");
            });
        Console.WriteLine(results);
    }

}