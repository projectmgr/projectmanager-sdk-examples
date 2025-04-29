using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Web;
using CommandLine;
using Microsoft.Extensions.FileSystemGlobbing;
using PmTask;
using PmTask.Clone;
using PmTask.SonarClient;
using PmTask.Sync;
using ProjectManager.SDK;
using ProjectManager.SDK.Models;

public static class Program
{
    private class BaseOptions
    {
        [Option('a', "apikey", HelpText = "The API key to use.  If not specified, uses the environment variable PM_API_KEY.")]
        public string? ApiKey { get; set; }
        
        [Option('e', "env", HelpText = "The URL of the ProjectManager environment to use.  If not specified, uses the environment variable PM_ENV.")]
        public string? Env { get; set; }
    }
    
    [Verb("git-blame-files", HelpText = "Create tasks for files matching a pattern in a git repository")]
    private class GitBlameFileOptions : BaseOptions
    {
        [Option('p', "pattern", Required = true,
            HelpText = "The file pattern to scan. Multiple patterns are separated by commas.")]
        public string Pattern { get; set; } = default!;

        [Option('f', "folder", HelpText = "The folder to scan (defaults to .)")]
        public string? Folder { get; set; }

        [Option("pmproject", Required = true, HelpText = "The ProjectManager project code")]
        public string PmProject { get; set; } = default!;
    }
    
    [Verb("sonar-hotspots", HelpText = "Create tasks for hotspots from SonarCloud.io")]
    private class SonarCloudOptions : BaseOptions
    {
        [Option('s', "sonar_token", HelpText = "SonarCloud API token")]
        public string ScApiKey { get; set; } = string.Empty;

        [Option("scproject", Required = true, HelpText = "The SonarCloud.io project code")]
        public string ScProject { get; set; } = default!;

        [Option("pmproject", Required = true, HelpText = "The ProjectManager project code")]
        public string PmProject { get; set; } = default!;
    }
    
    [Verb("create-task", HelpText = "Create a single task within a project")]
    private class CreateTaskOptions : BaseOptions
    {
        [Option("project", Required = true, HelpText = "The name, ID, or ShortID of the project in which to create the task")]
        public string? Project { get; set; }
        
        [Option("name", HelpText = "Name of the task")]
        public string? TaskName { get; set; }
        
        [Option("desc", HelpText = "Description of the task")]
        public string? Description { get; set; }
        
    }
    
    [Verb("list-tasks", HelpText = "List tasks within a project")]
    private class ListTasksOptions : BaseOptions
    {
        [Option("project", Required = true, HelpText = "The name, ID, or ShortCode of the project.")]
        public string? Project { get; set; }
        
        [Option('f', "format", HelpText = "If specified, outputs in the format JSON, CSV, or TSV (with tabs instead of commas).")]
        public OutputFormat? Format { get; set; }
        
        [Option('q', "query", HelpText = "A query to specify the tasks to retrieve")]
        public string? Query { get; set; }
    }
    
    [Verb("query-tasks", HelpText = "Query for tasks within your Workspace")]
    private class QueryTasksOptions : BaseOptions
    {
        [Option('f', "format", HelpText = "If specified, outputs in the format JSON, CSV, or TSV (with tabs instead of commas).")]
        public OutputFormat? Format { get; set; }

        [Option('q', "query", Required = true, HelpText = "A query to specify the tasks to retrieve")]
        public string Query { get; set; } = default!;
    }
    
    [Verb("list-projects", HelpText = "List projects within your Workspace")]
    private class ListProjectsOptions : BaseOptions
    {
        [Option('f', "format", HelpText = "If specified, outputs in the format JSON, CSV, or TSV (with tabs instead of commas).")]
        public OutputFormat? Format { get; set; }
    }
    
    [Verb("log-time", HelpText = "Log a timesheet entry for a Resource on a specific Task")]
    private class LogTimeOptions : BaseOptions
    {
        [Option('r', "resource", Required = true, HelpText = "The ID or email address of the Resource who is recording time")]
        public string ResourceId { get; set; } = default!;

        [Option('t', "task", Required = true, HelpText = "The ID or short code of the Task where the time was spent")]
        public string TaskId { get; set; } = default!;

        [Option('d', "day", HelpText = "The day on which the time was spent (Default: Today)")]
        public string? Day { get; set; } = default!;

        [Option('h', "hours", Required = true, HelpText = "The number of hours spent on this Task by this Resource on this Day")]
        public decimal Hours { get; set; } = default!;

        [Option('n', "notes", HelpText = "Comments on this timesheet")]
        public string? Notes { get; set; } 
    }
    
    [Verb("read-comments", HelpText = "Read all discussion comments about a task")]
    private class ReadCommentsOptions : BaseOptions
    {
        [Option("task", Required = true, HelpText = "The ShortID of the task to review discussion")]
        public string? Task { get; set; }
        
        [Option('f', "format", HelpText = "If specified, outputs in the format JSON, CSV, or TSV (with tabs instead of commas).")]
        public OutputFormat? Format { get; set; }
    }
    
    [Verb("add-comment", HelpText = "Add a comment to a task")]
    private class AddCommentOptions : BaseOptions
    {
        [Option("task", Required = true, HelpText = "The ShortID of the task to comment upon")]
        public string? Task { get; set; }
        
        [Option("message", HelpText = "The text, in markdown format, to add to the discussion")]
        public string? Message { get; set; }
    }

    [Verb("clone-account", HelpText = "Clone one account into another")]
    private class CloneAccountOptions
    {
        [Option('s', "src", Required = true, HelpText = "The API key of the source account to clone")]
        public string? SourceKey { get; set; }
        
        [Option('d', "dest", Required = true, HelpText = "The API key of the destination account that will receive the cloned data")]
        public string? DestinationKey { get; set; }
        
        [Option('e', "env", HelpText = "The URL of the ProjectManager environment to use.  If not specified, uses the environment variable PM_ENV.")]
        public string? Env { get; set; }
    }
    
    private	static Type[] LoadVerbs()
    {
        return Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();		 
    }
    
    public static async Task Main(string[] args)
    {
        var types = LoadVerbs();
        var parsed = Parser.Default.ParseArguments(args, types);
        await parsed.WithParsedAsync<ListProjectsOptions>(ListProjects);
        await parsed.WithParsedAsync<ListTasksOptions>(ListTasks);
        await parsed.WithParsedAsync<QueryTasksOptions>(QueryTasks);
        await parsed.WithParsedAsync<ReadCommentsOptions>(ReadComments);
        await parsed.WithParsedAsync<AddCommentOptions>(AddComment);
        await parsed.WithParsedAsync<CreateTaskOptions>(CreateTask);
        await parsed.WithParsedAsync<SonarCloudOptions>(ImportSonarCloud);
        await parsed.WithParsedAsync<GitBlameFileOptions>(GitBlameFiles);
        await parsed.WithParsedAsync<LogTimeOptions>(LogTime);
        await parsed.WithParsedAsync<CloneAccountOptions>(CloneAccount);
    }

    private static async Task CloneAccount(CloneAccountOptions options)
    {
        var src = ProjectManagerClient
            .WithEnvironment(options.Env)
            .WithMachineName(Environment.MachineName)
            .WithBearerToken(options.SourceKey)
            .WithAppName("PmTask");
        if (src == null)
        {
            return;
        }

        // Check privileges for source account
        var srcMe = await src.Me.RetrieveMe();
        if (srcMe.Data.Permissions.EditAllProjects != true)
        {
            Console.WriteLine(
                $"The user {srcMe.Data.EmailAddress} within workspace {srcMe.Data.WorkSpaceName} does not have EditAllProjects permission.");
            Console.WriteLine("This permission is required to ensure all projects can be cloned properly.");
            Console.WriteLine("Please try again using an API key that has EditAllProjects permission.");
            return;
        }
        Console.WriteLine($"Copying from account {srcMe.Data.WorkSpaceName} as {srcMe.Data.EmailAddress}.");
        
        // Check privileges for destination account
        var dest = ProjectManagerClient
            .WithEnvironment(options.Env)
            .WithMachineName(Environment.MachineName)
            .WithBearerToken(options.DestinationKey)
            .WithAppName("PmTask");
        var destMe = await dest.Me.RetrieveMe();
        if (destMe.Data.Permissions.EditAllProjects != true)
        {
            Console.WriteLine(
                $"The user {destMe.Data.EmailAddress} within workspace {destMe.Data.WorkSpaceName} does not have EditAllProjects permission.");
            Console.WriteLine("This permission is required to ensure all projects can be cloned properly.");
            Console.WriteLine("Please try again using an API key that has EditAllProjects permission.");
            return;
        }
        
        // This does all the work
        await AccountCloneHelper.CloneAccount(src, dest);
    }

    private static async Task LogTime(LogTimeOptions options)
    {
        var pmClient = await MakeClient(options);
        if (pmClient == null)
        {
            return;
        }

        // Identify the task and resource
        var task = await pmClient.FindOneTask($"(ShortId eq '{options.TaskId}')");
        if (task == null)
        {
            Console.WriteLine($"Unable to locate task '{options.TaskId}'");
            return;
        }
        var resource = await pmClient.FindOneResource($"(email eq '{options.ResourceId}')");
        if (resource == null)
        {
            Console.WriteLine($"Unable to locate resource '{options.ResourceId}'");
            return;
        }
        
        // Okay, let's create this timesheet entry
        var request = new TimesheetCreateRequestDto()
        {
            TaskId = task.Id,
            ResourceId = resource.Id,
            Date = options.Day ?? DateTime.Now.ToString("yyyy-MM-dd"),
            Hours = options.Hours,
            Notes = options.Notes,
        };
        var result = await pmClient.Timesheet.CreateTimeEntry(request);
        if (result.Success)
        {
            Console.WriteLine(
                $"Created timesheet entry for {resource.Email} on {request.Date} for {options.Hours} hours spent on {task.Name}.");
        }
        else
        {
            Console.WriteLine($"Error: Unable to create timesheet entry - {result.Error.Message}");
        }
    }

    private static async Task GitBlameFiles(GitBlameFileOptions options)
    {
        var pmClient = await MakeClient(options);
        if (pmClient == null)
        {
            return;
        }

        // Find files matching the globbing pattern
        Console.WriteLine($"Searching {options.Folder} for files matching {options.Pattern}...");
        var patterns =
            options.Pattern.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Matcher matcher = new();
        matcher.AddIncludePatterns(patterns);

        // Retrieve resources so we can match assignees
        var resources = await pmClient.LoadResources(null);
        if (resources == null)
        {
            return;
        }

        // Convert all these matches to tasks to sync
        var list = new List<RemoteSystemTaskModel>();
        foreach (var file in matcher.GetResultsInFullPath(options.Folder ?? "."))
        {
            // Gather stats on this file
            var fileInfo = new FileInfo(file);

            // Gather github most recent change on this file
            var processStartInfo = new ProcessStartInfo("git", $"log --pretty=format:\"%an%n%ae%n%ad\" -n 1 \"{file}\"")
            {
                WorkingDirectory = options.Folder
            };
            var gitInfo = await RunProcessAsTask.ProcessEx.RunAsync(processStartInfo);
            var blame = gitInfo == null || gitInfo.StandardOutput.Length == 0
                ? string.Empty
                : $"* **Last Change**: {string.Join(Environment.NewLine, gitInfo.StandardOutput)}";
            var name = gitInfo != null && gitInfo.StandardOutput.Length == 3 ? gitInfo.StandardOutput[0] : null;
            var email = gitInfo != null && gitInfo.StandardOutput.Length == 3 ? gitInfo.StandardOutput[1] : null;

            // Construct a task for it
            var item = new TaskCreateDto()
            {
                Name = $"Migration: {fileInfo.Name}",
                Description =
                    $"* **Directory**: {fileInfo.Directory}\n" +
                    $"* **File**: {fileInfo.Name} ({fileInfo.Length} bytes)\n" +
                    $"* **Last Modified**: {fileInfo.LastWriteTimeUtc}\n{blame}\n\n{file}",
                Assignees = FindAssigneeByEmail(email, name, resources),
            };
            list.Add(new RemoteSystemTaskModel()
            {
                UniqueId = file,
                TaskCreate = item,
            });
        }

        // Sync with ProjectManager
        Console.WriteLine($"Syncing with ProjectManager Project '{options.PmProject}'...");
        var results = await RemoteTaskSync.SyncRemoteTasks(list, pmClient, options.PmProject ?? string.Empty);
        if (results.Success)
        {
            Console.WriteLine("Success.");
            Console.WriteLine($"{results.TasksCreated} task(s) created, {results.TasksUpdated} task(s) updated, and {results.TasksDeleted} task(s) deleted.");
        }
        else
        {
            Console.WriteLine("Failed.");
        }
    }
    
    private static async Task ImportSonarCloud(SonarCloudOptions options)
    {
        var pmClient = await MakeClient(options);
        if (pmClient == null)
        {
            return;
        }

        // Fetch tasks from SonarCloud
        var sonarClient = new SonarClient(options.ScApiKey);
        
        // Fetch all hotspots
        var hotspots = await sonarClient.GetHotspots(options.ScProject);
        if (hotspots == null)
        {
            return;
        }
        Console.WriteLine($"Fetched {hotspots.Count} hotspots...");
        
        // Fetch priorities & resources
        var resources = await pmClient.LoadResources(null);
        if (resources == null)
        {
            return;
        }
        var priorityResult = await pmClient.Task.RetrieveTaskPriorities();
        if (!priorityResult.Success)
        {
            Console.WriteLine($"Failed to fetch task priorities: {priorityResult.Error.Message}");
            return;
        }
        var priorities = priorityResult.Data;
        
        // Convert HotSpots into PM Tasks
        var list = new List<RemoteSystemTaskModel>();
        foreach (var item in hotspots)
        {
            var taskCreate = new TaskCreateDto()
            {
                Name = $"[{item.vulnerabilityProbability}] - {item.message}",
                Description =
                    $"- **Problem**: {HttpUtility.HtmlEncode(item.message)}\n" +
                    $"- **Source**: {item.component.Replace(item.project + ":", "")} Line {item.line}\n" +
                    $"- **Detected**: {item.creationDate}\n" +
                    $"- **Blame**: {item.author}\n" +
                    $"- **Link**: [https://sonarcloud.io/project/security_hotspots?id={item.project}&hotspots={item.key}]\n" +
                    $"- **SonarCloud ID**: {item.key}",
                PriorityId = GetPriorityId(item.vulnerabilityProbability, priorities),
                Assignees = FindAssigneeByEmail(item.author, null, resources),
                Theme = GetThemeFromSeverity(item.vulnerabilityProbability),
            };
            list.Add(new RemoteSystemTaskModel() { UniqueId = item.key, TaskCreate = taskCreate });
        }

        // Sync this with ProjectManager
        Console.WriteLine($"Syncing with ProjectManager Project '{options.PmProject}'...");
        var results = await RemoteTaskSync.SyncRemoteTasks(list, pmClient, options.PmProject);
        if (results.Success)
        {
            Console.WriteLine("Success.");
            Console.WriteLine($"{results.TasksCreated} task(s) created, {results.TasksUpdated} task(s) updated, and {results.TasksDeleted} task(s) deleted.");
        }
        else
        {
            Console.WriteLine("Failed.");
        }
    }

    private static Guid[] FindAssigneeByEmail(string? email, string? name, List<ResourceDto> resources)
    {
        // Prefer to find via email first
        if (email != null)
        {
            var assignee = resources.FirstOrDefault(r => string.Equals(r.Email, email, StringComparison.CurrentCultureIgnoreCase));
            if (assignee != null)
            {
                return [assignee.Id!.Value];
            }
        }
        
        // Find by name - has a few variants.  We'll try full name first, then last name, then first name.
        // We will also only use matches if there's a single one.
        if (name != null)
        {
            var assignee = resources.SingleOrDefault(r =>
                string.Equals($"{r.FirstName} {r.LastName}", name, StringComparison.CurrentCultureIgnoreCase));
            
            if (assignee == null)
            {
                assignee = resources.SingleOrDefault(r =>
                    string.Equals(r.LastName, name, StringComparison.CurrentCultureIgnoreCase));
            }

            if (assignee == null)
            {
                assignee = resources.SingleOrDefault(r =>
                    string.Equals(r.FirstName, name, StringComparison.CurrentCultureIgnoreCase));
            }
            
            if (assignee != null)
            {
                return [assignee.Id!.Value];
            }
        }

        return [];
    }

    private static int? GetPriorityId(string priorityName, TaskPriorityDto[] priorities)
    {
        var priority = priorities.FirstOrDefault(p =>
            string.Equals(p.Name, priorityName, StringComparison.CurrentCultureIgnoreCase));
        return priority?.Id;
    }

    private static string? GetThemeFromSeverity(string severity)
    {
        switch (severity.ToUpper())
        {
            case "HIGH":
                return "Red";
            case "CRITICAL":
                return "Orange";
            case "MEDIUM":
                return "Yellow";
            default:
                return null;
        }
    }
    
    private static async Task ListProjects(ListProjectsOptions options)
    {
        var client = await MakeClient(options);
        if (client != null)
        {
            var projects = await client.LoadProjects(null);
            if (projects != null)
            {
                if (options.Format == null)
                {
                    options.Format.WriteLine($"Found {projects.Count} projects:");
                    foreach (var project in projects)
                    {
                        options.Format.WriteLine($"* {project.ShortId} - {project.Name} ({project.Id})");
                    }
                }
                else
                {
                    OutputHelper.WriteItems(projects, options.Format);
                }
            }
        }
    }
    
    private static async Task ListTasks(ListTasksOptions options)
    {
        var client = await MakeClient(options);
        if (client != null)
        {
            var project = await client.FindOneProject($"(ShortId eq '{options.Project}' OR Name eq '{options.Project}')");
            if (project != null)
            {
                options.Format.WriteLine($"Project {project.Name} ({project.ShortId})");

                // List all tasks within this project
                var tasks = await client.LoadTasks($"projectId eq {project.Id}");
                if (tasks != null)
                {
                    tasks.Sort(new WbsSortHelper());
                    if (options.Format == null)
                    {
                        foreach (var task in tasks)
                        {
                            Console.WriteLine(
                                $"* {task.Wbs} - {task.ShortId} - {task.Name} ({task.PercentComplete}% complete)");
                        }
                        Console.WriteLine($"Total {tasks.Count} tasks.");
                    }
                    else
                    {
                        OutputHelper.WriteItems(tasks, options.Format);
                    }
                }
            }
        }
    }
    
    private static async Task QueryTasks(QueryTasksOptions options)
    {
        var client = await MakeClient(options);
        if (client != null)
        {
            // List all tasks within this project
            var tasks = await client.LoadTasks(options.Query);
            if (tasks != null)
            {
                tasks.Sort(new WbsSortHelper());
                if (options.Format == null)
                {
                    foreach (var task in tasks)
                    {
                        Console.WriteLine(
                            $"* {task.Wbs} - {task.ShortId} - {task.Name} ({task.PercentComplete}% complete)");
                    }
                    Console.WriteLine($"Total {tasks.Count} tasks.");
                }
                else
                {
                    OutputHelper.WriteItems(tasks, options.Format);
                }
            }
        }
    }
    
    private static async Task ReadComments(ReadCommentsOptions options)
    {
        // Fetch project and task
        var client = await MakeClient(options);
        if (client != null)
        {
            var item = await client.FindOneTask($"(ShortId eq '{options.Task}')");
            if (item != null)
            {
                Console.WriteLine($"Task {item.Name} ({item.ShortId})");

                // Retrieve discussions
                var discussions = await client.Discussion.RetrieveTaskComments(item.Id!.Value);
                if (options.Format == null)
                {
                    if (discussions.Data.Length == 0)
                    {
                        Console.WriteLine("No comments.");
                    }

                    foreach (var comment in discussions.Data)
                    {
                        Console.WriteLine($"On {comment.CreateDate} {comment.AuthorName} wrote:");
                        Console.WriteLine("  " + comment.Text);
                        foreach (var reaction in comment.Emoji ?? Array.Empty<DiscussionEmoji>())
                        {
                            Console.WriteLine($"Reaction: {reaction.Name} ({reaction.UserIds.Length})");
                        }
                    }
                }
                else
                {
                    OutputHelper.WriteItems(discussions.Data, options.Format);
                }
            }
        }
    }
    
    private static async Task AddComment(AddCommentOptions options)
    {
        // Fetch project and task
        var client = await MakeClient(options);
        if (client != null)
        {
            var item = await client.FindOneTask($"(ShortId eq '{options.Task}')");
            if (item != null)
            {
                Console.WriteLine($"Task {item.Name} ({item.ShortId})");

                // Add discussion
                var comment = new DiscussionCommentCreateDto()
                {
                    Text = options.Message,
                };
                var result = await client.Discussion.CreateTaskComments(item.Id!.Value, comment);
                if (result == null || !result.Success)
                {
                    Console.WriteLine($"Task comment failed: {result?.Error.Message}");
                    return;
                }

                Console.WriteLine($"Added discussion comment {result.Data.DiscussionCommentId}.");
            }
        }
    }

    private static async Task CreateTask(CreateTaskOptions options)
    {
        var client = await MakeClient(options);
        if (client != null)
        {
            var project = await client.FindOneProject($"(ShortId eq '{options.Project}' OR Name eq '{options.Project}')");
            if (project != null)
            {
                // Create a task for this project
                var info = new TaskCreateDto()
                {
                    Name = options.TaskName,
                    Description = options.Description,
                };
                var task = await client.Task.CreateTask(project.Id!.Value, info);
                if (!task.Success)
                {
                    Console.WriteLine($"Could not create task {options.TaskName}: {task.Error.Message}");
                    return;
                }
                Console.WriteLine($"Created task {options.TaskName}: {task.Data.Id}");
            }
        }
    }

    private static async Task<ProjectManagerClient?> MakeClient(BaseOptions options)
    {
        ProjectManagerClient client;
        
        // Construct client
        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("PM_API_KEY");
        var environmentString = options.Env ?? Environment.GetEnvironmentVariable("PM_ENV");
        if (environmentString != null)
        {
            client = ProjectManagerClient
                .WithCustomEnvironment(new Uri(environmentString), null)
                .WithMachineName(Environment.MachineName)
                .WithBearerToken(apiKey)
                .WithAppName("PmTask");
        }
        else
        {
            client = ProjectManagerClient
                .WithEnvironment("production")
                .WithMachineName(Environment.MachineName)
                .WithBearerToken(apiKey)
                .WithAppName("PmTask");
        }

        // Verify it's working
        var me = await client.Me.RetrieveMe();
        if (me.Success)
        {
            Console.Error.WriteLine($"Logged on as {me.Data.EmailAddress} ({me.Data.RoleName}) in workspace {me.Data.WorkSpaceName}.");
            return client;
        }
        Console.Error.WriteLine($"Failed to verify credentials: {me.Error.Message}");
        return null;
    }
}