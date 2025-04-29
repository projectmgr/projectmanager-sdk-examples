using ProjectManager.SDK;

namespace PmTask.Clone;

public class AccountCloneHelper
{
    public static async Task CloneAccount(ProjectManagerClient src, ProjectManagerClient dest)
    {
        // Customer
        var customers = await src.ProjectCustomer.RetrieveProjectCustomers();
        Console.WriteLine($"Cloning {customers.Data.Length} customers");
        
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
}