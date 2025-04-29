using ProjectManager.SDK;
using ProjectManager.SDK.Models;

namespace PmTask.Clone;

public class AccountCloneHelper
{
    public static async Task CloneAccount(ProjectManagerClient src, ProjectManagerClient dest)
    {
        var map = new AccountMap();
        await CloneCustomers(src, dest, map);
        await MatchProjectPriorities(src, dest, map);
        await MatchProjectChargeCodes(src, dest, map);
        await MatchProjectFolders(src, dest, map);
        await MatchProjectStatuses(src, dest, map);
        await CloneProjectFields(src, dest, map);
        await CloneResourceSkills(src, dest, map);
        await CloneResourceTeams(src, dest, map);
        await CloneTags(src, dest, map);
        await CloneResources(src, dest, map);
        await CloneProjects(src, dest, map);
        await CloneTimesheets(src, dest, map);
    }

    private static async Task CloneTimesheets(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        // Task Field Value
        //var taskFieldValues = await src.TaskField.RetrieveAllTaskFieldValues(taskId);
        // Timesheet
        var timesheets = await src.Timesheet.QueryTimeSheets();
        Console.WriteLine($"Cloning {timesheets.Data.Length} timesheets");
    }

    private static async Task CloneProjects(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        var srcProjects = await src.Project.QueryProjects().ThrowOnError("Fetching from source");
        var destProjects = await dest.Project.QueryProjects().ThrowOnError("Fetching from destination");
        Console.Write($"Cloning {srcProjects.Data.Length} projects... ");

        var results = await SyncHelper.SyncData("Project", srcProjects.Data, destProjects.Data, map,
            p => p.Name,
            p => p.Id!.Value.ToString(),
            (p1, p2) =>
            {
                return p1.Name == p2.Name
                       && p1.HourlyRate == p2.HourlyRate
                       && p1.Budget == p2.Budget
                       && p1.Description == p2.Description
                       && p1.Folder.Id == p2.Folder.Id
                       && p1.ChargeCode.Name == p2.ChargeCode.Name
                       && p1.Manager.Name == p2.Manager.Name
                       && p1.Customer.Name == p2.Customer.Name
                       && p1.Status.Name == p2.Status.Name
                       && p1.Priority.Name == p2.Priority.Name
                       && p1.StatusUpdate == p2.StatusUpdate;
            },
            async p =>
            {
                var result = await dest.Project.CreateProject(new ProjectCreateDto()
                {
                    Name = p.Name,
                    Description = p.Description,
                    HourlyRate = p.HourlyRate,
                    Budget = p.Budget,
                    StatusUpdate = p.StatusUpdate,
                    FolderId = map.MapKeyGuid("ProjectFolder", p.Folder.Id),
                    ChargeCodeId = map.MapKeyGuid("ProjectChargeCode", p.ChargeCode.Id),
                    ManagerId = map.MapKeyGuid("Resource", p.Manager.Id),
                    CustomerId = map.MapKeyGuid("ProjectCustomer", p.Customer.Id),
                    StatusId = map.MapKeyGuid("ProjectStatus", p.Status.Id),
                    PriorityId = map.MapKeyGuid("ProjectPriority", p.Priority.Id),
                }).ThrowOnError("Creating");
                return result.Data.Id!.Value.ToString();
            },
            null,
            null);
        Console.WriteLine(results);
        
        // Project Members
        //var projectId = Guid.Empty;
        //var projectMembers = await src.ProjectMembers.RetrieveProjectMembers(projectId, false);
        
        // Project Field Value
        //var projectFieldValues = await src.ProjectField.RetrieveAllProjectFieldValues(projectId);

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
    }

    private static async Task CloneResources(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        var srcResources = await src.Resource.QueryResources().ThrowOnError("Fetching from source");
        var destResources = await dest.Resource.QueryResources().ThrowOnError("Fetching from destination");
        Console.Write($"Cloning {srcResources.Data.Length} resources... ");
        
        // Special handling for resources: We aren't creating them with email addresses, because that would create
        // users and link them to the test account.  Because of this, we eliminate first and last names, and only sync
        // on email addresses.  But when migrating data, all email addresses are moved to last names.
        foreach (var item in srcResources.Data)
        {
            if (!string.IsNullOrWhiteSpace(item.Email))
            {
                item.FirstName = "Cloned";
                item.LastName = item.Email.Replace('@','_');
                item.Email = null;
            }
        }
        var filteredDestResources = destResources.Data.Where(r => String.IsNullOrWhiteSpace(r.Email)).ToArray();

        var results = await SyncHelper.SyncData("Resource", srcResources.Data, filteredDestResources, map,
            r => $"{r.FirstName} {r.LastName}",
            r => r.Id!.Value.ToString(),
            (r1, r2) =>
            {
                return r1.Color == r2.Color
                       && r1.FirstName == r2.FirstName
                       && r1.LastName == r2.LastName
                       && r1.AvatarUrl == r2.AvatarUrl
                       && r1.City == r2.City
                       && r1.ColorName == r2.ColorName
                       && r1.Country == r2.Country
                       && r1.CountryName == r2.CountryName
                       && r1.HourlyRate == r2.HourlyRate
                       && r1.Initials == r2.Initials
                       && r1.IsActive == r2.IsActive
                       && r1.Notes == r2.Notes
                       && r1.Phone == r2.Phone
                       && r1.Role == r2.Role
                       && r1.State == r2.State;
            },
            async r =>
            {
                var teams = r.Teams
                    .Select(t => t.Id!.Value)
                    .Select(t => map.MapKeyGuid("ResourceTeam", t)!.Value)
                    .ToArray();
                var skills = r.Skills
                    .Select(t => t.Id!.Value)
                    .Select(t => map.MapKeyGuid("ResourceSkill", t)!.Value)
                    .ToArray();
                var result = await dest.Resource.CreateResource(new ResourceCreateDto()
                {
                    City = r.City,
                    ColorName = r.ColorName,
                    CountryCode = r.Country,
                    Email = null,
                    FirstName = r.FirstName,
                    HourlyRate = r.HourlyRate,
                    LastName = r.LastName,
                    Notes = r.Notes,
                    Phone = r.Phone,
                    RoleId = null,
                    State = r.State,
                    TeamIds = teams,
                    SkillIds = skills,

                }).ThrowOnError("Creating");
                return result.Data.Id!.Value.ToString();
            },
            async (srcResource, destResource) =>
            {
                await dest.Resource.UpdateResource(destResource.Id!.Value, new ResourceUpdateDto()
                {
                    Notes = srcResource.Notes,
                    HourlyRate = srcResource.HourlyRate,
                    City = srcResource.City,
                    State = srcResource.State,
                    CountryCode = srcResource.Country,
                    ColorName = srcResource.ColorName,
                    FirstName = srcResource.FirstName,
                    LastName = srcResource.LastName,
                    Phone = srcResource.Phone,
                }).ThrowOnError("Updating");
            },
            async r =>
            {
                await dest.Resource.UpdateResource(r.Id!.Value, new ResourceUpdateDto()
                {
                    IsActive = false
                }).ThrowOnError("Deactivating");
            }
        );
        Console.WriteLine(results);
    }

    private static async Task CloneTags(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        var srcTags = await src.Tag.QueryTags().ThrowOnError("Fetching from source");
        var destTags = await dest.Tag.QueryTags().ThrowOnError("Fetching from destination");
        Console.Write($"Cloning {srcTags.Data.Length} tags... ");

        var results = await SyncHelper.SyncData("Tag", srcTags.Data, destTags.Data, map,
            t => t.Name,
            t => t.Id!.Value.ToString(),
            (t1, t2) => t1.Name == t2.Name && t1.Color == t2.Color,
            async t =>
            {
                var result = await dest.Tag.CreateTag(new TagCreateDto() { Color = t.Color, Name = t.Name })
                    .ThrowOnError("Creating");
                return result.Data.Id!.Value.ToString();
            },
            null, // Updating can't rename a tag, only change its color
            null // You can't delete tags
        );
        Console.WriteLine(results);
    }

    private static async Task CloneResourceTeams(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        var srcResourceTeams = await src.ResourceTeam.RetrieveResourceTeams()
            .ThrowOnError("Fetching from source");
        var destResourceTeams =
            await dest.ResourceTeam.RetrieveResourceTeams().ThrowOnError("Fetching from destination");
        Console.Write($"Cloning {srcResourceTeams.Data.Length} resourceTeams... ");

        var results = await SyncHelper.SyncData("ResourceTeam", srcResourceTeams.Data, destResourceTeams.Data, map,
            t => t.Name,
            t => t.Id!.Value.ToString(),
            (t1, t2) => t1.Name == t2.Name,
            async t =>
            {
                var result = await dest.ResourceTeam.CreateResourceTeam(new CreateResourceTeamDto() { Name = t.Name })
                    .ThrowOnError("Creating");
                return result.Data.Id!.Value.ToString();
            },
            async (srcTeam, destTeam) =>
            {
                await dest.ResourceTeam
                    .UpdateResourceTeam(destTeam.Id!.Value, new UpdateResourceTeamDto() { Name = srcTeam.Name })
                    .ThrowOnError("Updating");
            },
            async t =>
            {
                await dest.ResourceTeam.DeleteResourceTeam(t.Id!.Value).ThrowOnError("Deleting");
            }
        );
        Console.WriteLine(results);
    }

    private static async Task CloneResourceSkills(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        // Resource Skill
        var srcResourceSkills = await src.ResourceSkill.RetrieveResourceSkills().ThrowOnError("Fetching from source");
        var destResourceSkills =
            await dest.ResourceSkill.RetrieveResourceSkills().ThrowOnError("Fetching from destination");
        Console.Write($"Cloning {srcResourceSkills.Data.Length} resourceSkills... ");

        var results = await SyncHelper.SyncData("ResourceSkill", srcResourceSkills.Data, destResourceSkills.Data, map,
            s => s.Name,
            s => s.Id!.Value.ToString(),
            (s1, s2) => s1.Name == s2.Name,
            async s =>
            {
                var result = await dest.ResourceSkill
                    .CreateResourceSkill(new CreateResourceSkillDto() { Name = s.Name }).ThrowOnError("Creating");
                return result.Data.Id!.Value.ToString();
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

        var results = await SyncHelper.SyncData("ProjectField", srcProjectFields.Data, destProjectFields.Data, map,
            pf => pf.Name,
            pf => pf.Id!.Value.ToString(),
            (srcPf, destPf) =>
            {
                return srcPf.Name == destPf.Name
                       && srcPf.Options == destPf.Options
                       && srcPf.Type == destPf.Type
                       && srcPf.ShortId == destPf.ShortId;
            }, 
            async pf =>
            {
                var result = await dest.ProjectField.CreateProjectField(new ProjectFieldCreateDto()
                        { Name = pf.Name, Options = pf.Options, ShortId = pf.ShortId, Type = pf.Type })
                    .ThrowOnError("Creating");
                return result.Data.Id!.Value.ToString();
            }, 
            null, // no updates available for project fields 
            async pf =>
            {
                await dest.ProjectField.DeleteProjectField(pf.Id!.Value.ToString()).ThrowOnError("Deleting");
            });
        Console.WriteLine(results);
    }

    private static async Task MatchProjectStatuses(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        var srcProjectStatuses = await src.ProjectStatus.RetrieveProjectStatuses().ThrowOnError("Fetching from source");
        var destProjectStatuses =
            await dest.ProjectStatus.RetrieveProjectStatuses().ThrowOnError("Fetching from destination");
        Console.WriteLine($"Comparing {srcProjectStatuses.Data.Length} projectStatuses.");

        // Load up the mappings between source and destination
        SyncHelper.MatchData("ProjectStatus", srcProjectStatuses.Data, destProjectStatuses.Data, map,
                ps => ps.Name,
                ps => ps.Id!.Value.ToString());
    }

    private static async Task MatchProjectFolders(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        var srcProjectFolders = await src.ProjectFolder.RetrieveProjectFolders().ThrowOnError("Fetching from source");
        var destProjectFolders =
            await dest.ProjectFolder.RetrieveProjectFolders().ThrowOnError("Fetching from destination");
        Console.WriteLine($"Comparing {srcProjectFolders.Data.Length} projectFolders.");

        // Load up the mappings between source and destination
        SyncHelper.MatchData("ProjectFolder", srcProjectFolders.Data, destProjectFolders.Data, map,
            ps => ps.Name,
            ps => ps.Id!.Value.ToString());
    }

    private static async Task MatchProjectChargeCodes(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        var srcProjectChargeCodes =
            await src.ProjectChargeCode.RetrieveChargeCodes().ThrowOnError("Fetching from source");
        var destProjectChargeCodes =
            await dest.ProjectChargeCode.RetrieveChargeCodes().ThrowOnError("Fetching from destination");
        Console.WriteLine($"Comparing {srcProjectChargeCodes.Data.Length} projectChargeCodes.");

        // Load up the mappings between source and destination
        SyncHelper.MatchData("ProjectChargeCode", srcProjectChargeCodes.Data, destProjectChargeCodes.Data, map,
            ps => ps.Name,
            ps => ps.Id!.Value.ToString());
    }

    private static async Task MatchProjectPriorities(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        var srcProjectPriorities = await src.ProjectPriority.RetrieveProjectPriorities()
            .ThrowOnError("Fetching from source");
        var destProjectPriorities =
            await dest.ProjectPriority.RetrieveProjectPriorities().ThrowOnError("Fetching from destination");
        Console.WriteLine($"Comparing {srcProjectPriorities.Data.Length} projectPriorities.");

        // Load up the mappings between source and destination
        SyncHelper.MatchData("ProjectPriority", srcProjectPriorities.Data, destProjectPriorities.Data, map,
            ps => ps.Name,
            ps => ps.Id!.Value.ToString());
    }

    private static async Task CloneCustomers(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        var srcCustomers = await src.ProjectCustomer.RetrieveProjectCustomers()
            .ThrowOnError("Fetching from source");
        Console.Write($"Cloning {srcCustomers.Data.Length} customers... ");
        var destCustomers = await dest.ProjectCustomer.RetrieveProjectCustomers()
            .ThrowOnError("Fetching from destination");

        // Execute the sync
        var results = await SyncHelper.SyncData<ProjectCustomerDto>("ProjectCustomer", srcCustomers.Data, destCustomers.Data, map,
            c => c.Name,
            c => c.Id!.Value.ToString(),
            (cSrc, cDest) => cSrc.Name == cDest.Name, 
            async c =>
            {
                var created = await dest.ProjectCustomer.CreateProjectCustomer(new ProjectCustomerCreateDto()
                {
                    Name = c.Name
                }).ThrowOnError("Creating");
                return created.Data.Id!.Value.ToString();
            },
            async (cSrc, cDest) =>
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