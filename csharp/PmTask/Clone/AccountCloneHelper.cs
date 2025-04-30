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
        await MatchTaskPriorities(src, dest, map);
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
        var srcProjects = await src.Project.QueryProjects(null, null, "status/isDeleted eq false").ThrowOnError("Fetching from source");
        var destProjects = await dest.Project.QueryProjects().ThrowOnError("Fetching from destination");
        Console.Write($"Cloning {srcProjects.Data.Length} projects... ");

        var results = await SyncHelper.SyncData("Project", srcProjects.Data, destProjects.Data, map,
            p => p.Name,
            p => p.Id!.Value.ToString(),
            (p1, p2) => p1.Name == p2.Name
                        && p1.HourlyRate == p2.HourlyRate
                        && p1.Budget == p2.Budget
                        && p1.Description == p2.Description
                        && p1.Folder?.Name == p2.Folder?.Name
                        && p1.ChargeCode?.Name == p2.ChargeCode?.Name
                        && p1.Customer?.Name == p2.Customer?.Name
                        && p1.Status.Name == p2.Status?.Name
                        && p1.Priority.Name == p2.Priority.Name
                        && p1.StatusUpdate == p2.StatusUpdate,
            async p =>
            {
                var np = new ProjectCreateDto
                {
                    Name = p.Name,
                    Description = p.Description,
                    HourlyRate = p.HourlyRate,
                    Budget = p.Budget,
                    StatusUpdate = p.StatusUpdate,
                    FolderId = map.MapKeyGuid("ProjectFolder", p.Folder?.Id),
                    ChargeCodeId = map.MapKeyGuid("ProjectChargeCode", p.ChargeCode?.Id),
                    ManagerId = map.MapKeyGuid("Resource", p.Manager?.Id),
                    CustomerId = map.MapKeyGuid("ProjectCustomer", p.Customer?.Id),
                    StatusId = map.MapKeyGuid("ProjectStatus", p.Status.Id),
                    PriorityId = map.MapKeyGuid("ProjectPriority", p.Priority.Id),
                };
                var result = await dest.Project.CreateProject(np).ThrowOnError("Creating");
                return result.Data.Id!.Value.ToString();
            },
            null,
            async p =>
            {
                await dest.Project.DeleteProject(p.Id!.Value, true).ThrowOnError("Deleting");
            });
        Console.WriteLine(results);

        foreach (var srcProject in srcProjects.Data)
        {
            var srcProjectId = srcProject.Id!.Value;
            var destProjectId = map.MapKeyGuid("Project", srcProjectId) ?? Guid.Empty;

            if (destProjectId == Guid.Empty)
            {
                Console.WriteLine($"No destination project found for {srcProject.Name}");
                continue;
            }

            //// Project Members
            //var srcProjectMembers = await src.ProjectMembers.RetrieveProjectMembers(srcProjectId, false).ThrowOnError("Fetching from source");
            //var destProjectMembers = await dest.ProjectMembers.RetrieveProjectMembers(destProjectId!, false).ThrowOnError("Fetching from destination");
            //Console.Write($"Cloning {srcProjectMembers.Data.Length} project members for project... ");

            // Because Resources have been created without an email address, they cannot have any other Role assigned to them except the default None role.
            // Resources with the None role are only present in the Resources table, not in the BusinessUsers table. The problem we have here is that when we
            // try and assign the Guest role/permission to a Resource, it tries to write an entry in the ProjectAccess table, which throws a foreign key error
            //      The INSERT statement conflicted with the FOREIGN KEY constraint "FK_ProjectAccess_aspnet_Users".
            //      The conflict occurred in database "pmproject", table "dbo.BusinessUser", column 'UserId'.
            /*
            results = await SyncHelper.SyncData("ProjectMember", srcProjectMembers.Data, destProjectMembers.Data, map,
                m => m.Name,
                m => m.Id!.Value.ToString(),
                (m1, m2) => m1.Initials == m2.Initials
                            && m1.Name == m2.Name
                            && m1.AvatarUrl == m2.AvatarUrl
                            // Cannot compare on Permission because destination can only be added as Guest
                            //&& m1.Permission == m2.Permission
                            && m1.Color == m2.Color,
                async m =>
                {
                    var nm = new ProjectMemberRoleDto
                    {
                        Role = "Guest"
                    };
                    var destMemberId = map.MapKeyGuid("Resource", m.Id) ?? Guid.Empty;
                    if (destMemberId == Guid.Empty)
                    {
                        throw new Exception($"No destination resource found for {m.Name}");
                    }
                    var result = await dest.ProjectMembers.CreateUserProjectMembership(destProjectId, destMemberId, nm).ThrowOnError("Creating");
                    return result.Data.Id!.Value.ToString();
                },
                async (sm, dm) =>
                {
                    var um = new ProjectMemberRoleDto
                    {
                        Role = sm.Permission
                    };
                    await dest.ProjectMembers.UpdateUserProjectMembership(destProjectId, dm.Id!.Value, um).ThrowOnError("Updating");
                },
                async m =>
                {
                    await dest.ProjectMembers.RemoveUserProjectMembership(destProjectId, m.Id!.Value).ThrowOnError("Removing");
                }
            );
            Console.WriteLine(results);
            */

            // Project Field Values
            var srcProjectFieldValues = await src.ProjectField.RetrieveAllProjectFieldValues(srcProjectId);
            var destProjectFieldValues = await dest.ProjectField.RetrieveAllProjectFieldValues(destProjectId);
            Console.Write($"Cloning {srcProjectFieldValues.Data.Length} project field values for project... ");

            results = await SyncHelper.SyncData("ProjectFieldValue", srcProjectFieldValues.Data, destProjectFieldValues.Data, map,
                v => $"{srcProject.Name} - {v.Name}",
                v => v.Id!.Value.ToString(),
                (v1, v2) => v1.ShortId == v2.ShortId
                            && v1.Name == v2.Name
                            && v1.Type == v2.Type
                            && v1.Value == v2.Value,
                async v =>
                {
                    var nv = new UpdateProjectFieldValueDto
                    {
                        Value = v.Value
                    };
                    var destFieldId = map.MapKeyGuid("ProjectField", v.Id) ?? Guid.Empty;
                    if (destFieldId == Guid.Empty)
                    {
                        throw new Exception($"No destination project field found for {v.Name}");
                    }
                    await dest.ProjectField.UpdateProjectFieldValue(destProjectId, destFieldId.ToString(), nv).ThrowOnError("Creating");
                    return destFieldId.ToString();
                },
                async (sv, dv) =>
                {
                    var uv = new UpdateProjectFieldValueDto
                    {
                        Value = sv.Value
                    };
                    await dest.ProjectField.UpdateProjectFieldValue(destProjectId, dv.Id!.ToString(), uv).ThrowOnError("Updating");
                },
                async v =>
                {
                    var uv = new UpdateProjectFieldValueDto
                    {
                        Value = string.Empty
                    };
                    await dest.ProjectField.UpdateProjectFieldValue(destProjectId, v.Id!.ToString(), uv).ThrowOnError("Clearing");
                }
            );
            Console.WriteLine(results);

            // Task Status
            var srcTaskStatus = await src.TaskStatus.RetrieveTaskStatuses(srcProjectId);
            var destTaskStatus = await dest.TaskStatus.RetrieveTaskStatuses(destProjectId);
            Console.Write($"Cloning {srcTaskStatus.Data.Length} task statuses for project... ");

            results = await SyncHelper.SyncData("TaskStatus", srcTaskStatus.Data, destTaskStatus.Data, map,
                s => $"{srcProject.Name} - {s.Name}",
                s => s.Id!.Value.ToString(),
                (s1, s2) => s1.Name == s2.Name
                            && s1.Order == s2.Order,
                            // Currently, there is no way to create/update the IsDone property via the API - value is always false
                            //&& s1.IsDone == s2.IsDone,
                async s =>
                {
                    var ns = new TaskStatusCreateDto
                    {
                        Name = s.Name,
                        Order = s.Order,
                        IsDone = s.IsDone, // Currently, there is no way to create/update the IsDone property via the API - value is always false
                    };
                    var result = await dest.TaskStatus.CreateTaskStatus(destProjectId, ns).ThrowOnError("Creating");
                    return result.Data.Id!.Value.ToString();
                },
                async (ss, ds) =>
                {
                    var us = new TaskStatusUpdateDto
                    {
                        Id = ds.Id,
                        Name = ss.Name,
                        Order = ss.Order,
                    };
                    await dest.TaskStatus.UpdateTaskStatus(destProjectId, us).ThrowOnError("Updating");
                },
                async v =>
                {
                    await dest.TaskStatus.DeleteTaskStatus(destProjectId, v.Id!.Value).ThrowOnError("Deleting");
                }
            );
            Console.WriteLine(results);
        }

        // Tasks
        var tasks = await src.Task.QueryTasks();
        Console.WriteLine($"Cloning {tasks.Data.Length} tasks");

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
        var srcResources = await src.Resource.QueryResources(null, null, "isActive eq true").ThrowOnError("Fetching from source");
        var destResources = await dest.Resource.QueryResources(null, null, "isActive eq true").ThrowOnError("Fetching from destination");
        Console.Write($"Cloning {srcResources.Data.Length} resources... ");

        // Special handling for resources: We aren't creating them with email addresses, because that would create
        // users and link them to the test account.  Because of this, we eliminate first and last names, and only sync
        // on email addresses.  But when migrating data, all email addresses are moved to last names.
        foreach (var item in srcResources.Data)
        {
            if (!string.IsNullOrWhiteSpace(item.Email))
            {
                item.FirstName = "Cloned";
                item.LastName = item.Email.Replace('@', '_');
                item.Email = string.Empty;
            }
        }
        var filteredDestResources = destResources.Data.Where(r => String.IsNullOrWhiteSpace(r.Email)).ToArray();

        var results = await SyncHelper.SyncData("Resource", srcResources.Data, filteredDestResources, map,
            r => $"{r.FirstName} {r.LastName}".ToLowerInvariant(),
            r => r.Id!.Value.ToString(),
            (r1, r2) =>
            {
                return r1.Color == r2.Color
                       && String.Equals(r1.FirstName, r2.FirstName, StringComparison.InvariantCultureIgnoreCase)
                       && String.Equals(r1.LastName, r2.LastName, StringComparison.InvariantCultureIgnoreCase)
                       && r1.AvatarUrl == r2.AvatarUrl
                       && r1.City == r2.City
                       && r1.ColorName == r2.ColorName
                       && r1.Country == r2.Country
                       && r1.CountryName == r2.CountryName
                       && r1.HourlyRate == r2.HourlyRate
                       && r1.IsActive == r2.IsActive
                       && r1.Notes == r2.Notes
                       && r1.Phone == r2.Phone
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

    private static async Task MatchTaskPriorities(ProjectManagerClient src, ProjectManagerClient dest, AccountMap map)
    {
        var srcTaskPriorities = await src.Task.RetrieveTaskPriorities().ThrowOnError("Fetching from source");
        var destTaskPriorities = await dest.Task.RetrieveTaskPriorities().ThrowOnError("Fetching from destination");
        Console.WriteLine($"Comparing {srcTaskPriorities.Data.Length} TaskPriorities.");

        // Load up the mappings between source and destination
        SyncHelper.MatchData("TaskPriority", srcTaskPriorities.Data, destTaskPriorities.Data, map,
            tp => tp.Name,
            tp => tp.Id?.ToString() ?? string.Empty // The id of the None priority is null
        );
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