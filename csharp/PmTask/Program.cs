using System.Reflection;
using CommandLine;
using PmTask;
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
    
    [Verb("create", HelpText = "Create a new task")]
    private class CreateOptions : BaseOptions
    {
        [Option("project", HelpText = "The name, ID, or ShortID of the project in which to create the task")]
        public string? Project { get; set; }
        
        [Option("name", HelpText = "Name of the task")]
        public string? TaskName { get; set; }
        
        [Option("desc", HelpText = "Description of the task")]
        public string? Description { get; set; }
        
        [Option("pri", HelpText = "Priority of the task")]
        public string? Priority { get; set; }
    }
    
    [Verb("list-tasks", HelpText = "List tasks within a project")]
    private class ListTasksOptions : BaseOptions
    {
        [Option("project", Required = true, HelpText = "The name, ID, or ShortCode of the project")]
        public string? Project { get; set; }
    }
    
    [Verb("list-projects", HelpText = "List projects")]
    private class ListProjectsOptions : BaseOptions
    {
    }
    
    [Verb("read-comments", HelpText = "Read all discussion comments about a task")]
    private class ReadCommentsOptions : BaseOptions
    {
        [Option("task", Required = true, HelpText = "The ShortID of the task to review discussion")]
        public string? Task { get; set; }
    }
    
    [Verb("add-comment", HelpText = "Add a comment to a task")]
    private class AddCommentOptions : BaseOptions
    {
        [Option("task", Required = true, HelpText = "The ShortID of the task to comment upon")]
        public string? Task { get; set; }
        
        [Option("message", HelpText = "The text, in markdown format, to add to the discussion")]
        public string? Message { get; set; }
    }
    
    private	static Type[] LoadVerbs()
    {
        return Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();		 
    }
    
    public static async Task Main(string[] args)
    {
        var types = LoadVerbs();
        
        // Project related verbs
        var parsed = Parser.Default.ParseArguments(args, types);
        await parsed.WithParsedAsync<ListProjectsOptions>(ListProjects);
        await parsed.WithParsedAsync<ListTasksOptions>(ListTasks);
        await parsed.WithParsedAsync<ReadCommentsOptions>(ReadComments);
        await parsed.WithParsedAsync<AddCommentOptions>(AddComment);
        // .WithParsed<CreateOptions>(CreateTask)
        // .WithParsed<ListOptions>(ListTask)
        // .WithParsed<CommentOptions>(CommentTask)
    }

    private static async Task ListProjects(ListProjectsOptions options)
    {
        var client = await MakeClient(options);
        if (client != null)
        {
            var projects = await client.LoadProjects(null);
            if (projects != null)
            {
                Console.WriteLine($"Found {projects.Count} projects:");
                foreach (var project in projects)
                {
                    Console.WriteLine($"* {project.ShortId} - {project.Name}");
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
                Console.WriteLine($"Project {project.Name} ({project.ShortId})");

                // List all tasks within this project
                var tasks = await client.LoadTasks($"projectId eq {project.Id}");
                if (tasks != null)
                {
                    tasks.Sort(new WbsSortHelper());
                    Console.WriteLine($"Found {tasks.Count} tasks.");
                    foreach (var task in tasks)
                    {
                        Console.WriteLine(
                            $"* {task.Wbs} - {task.ShortId} - {task.Name} ({task.PercentComplete}% complete)");
                    }
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
/*
    private static void HandleErrors(IEnumerable<Error> errors)
    {
        var errList = errors.ToList();
        Console.WriteLine($"Found {errList.Count} errors.");
    }

    private static void CreateTask(CreateOptions options)
    {
        var client = MakeClient(options);
        
        // Fetch all projects and find the one that matches locally so we can give debugging information
        var project = client.FindProject(options.Project).Result;
        if (project?.Id == null)
        {
            return;
        }
        
        // Create a task for this project
        var info = new TaskCreateDto()
        {
            Name = options.TaskName,
            Description = options.Description,
        };
        var task = client.Task.CreateTask(project.Id.Value, info).Result;
        if (!task.Success)
        {
            Console.WriteLine($"Could not create task {options.TaskName}: {task.Error.Message}");
            return;
        }
        Console.WriteLine($"Created task {options.TaskName}: {task.Data.Id}");
    }
*/
    private static async Task<ProjectManagerClient?> MakeClient(BaseOptions options)
    {
        ProjectManagerClient client;
        
        // Construct client
        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("PM_API_KEY");
        var environmentString = options.Env ?? Environment.GetEnvironmentVariable("PM_ENV");
        if (environmentString != null)
        {
            client = ProjectManagerClient
                .WithCustomEnvironment(new Uri(environmentString))
                .WithBearerToken(apiKey)
                .WithAppName("PmTask");
        }
        else
        {
            client = ProjectManagerClient
                .WithEnvironment("production")
                .WithBearerToken(apiKey)
                .WithAppName("PmTask");
        }

        // Verify it's working
        var me = await client.Me.RetrieveMe();
        if (me.Success)
        {
            Console.WriteLine($"Logged on as {me.Data.EmailAddress} ({me.Data.RoleName}) in workspace {me.Data.WorkSpaceName}.");
            return client;
        }
        Console.WriteLine($"Failed to verify credentials: {me.Error.Message}");
        return null;
    }
}