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
    
    [Verb("list", HelpText = "List tasks within a project")]
    private class ListOptions : BaseOptions
    {
        [Option("project", HelpText = "The name, ID, or ShortID of the project in which to create the task")]
        public string? Project { get; set; }
    }
    
    [Verb("discuss", HelpText = "Read all discussion comments about a task")]
    private class DiscussOptions : BaseOptions
    {
        [Option("task", HelpText = "The ShortID of the task to review discussion")]
        public string? Task { get; set; }
    }
    
    [Verb("comment", HelpText = "Add a comment to a task within a project")]
    private class CommentOptions : BaseOptions
    {
        [Option("task", HelpText = "The ShortID of the task to comment upon")]
        public string? Task { get; set; }
        
        [Option("message", HelpText = "The text, in markdown format, to add to the discussion")]
        public string? Message { get; set; }
    }
    
    private	static Type[] LoadVerbs()
    {
        return Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();		 
    }
    
    public static void Main(string[] args)
    {
        var types = LoadVerbs();	
        Parser.Default.ParseArguments(args, types)
            .WithParsed<CreateOptions>(CreateTask)
            .WithParsed<ListOptions>(ListTask)
            .WithParsed<DiscussOptions>(DiscussTask)
            .WithParsed<CommentOptions>(CommentTask)
            .WithNotParsed(HandleErrors);
    }

    private static void DiscussTask(DiscussOptions options)
    {
        // Fetch project and task
        var client = MakeClient(options);
        var task = client.FindTask(options.Task).Result;
        if (task?.Id == null)
        {
            return;
        }

        Console.WriteLine($"Task {task.Name} ({task.ShortId})");
        
        // Retrieve discussions
        var discussions = client.Discussion.RetrieveTaskComments(task.Id.Value).Result;
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
    
    
    private static void CommentTask(CommentOptions options)
    {
        // Fetch project and task
        var client = MakeClient(options);
        var task = client.FindTask(options.Task).Result;
        if (task?.Id == null)
        {
            return;
        }

        // Explain what we're doing
        Console.WriteLine($"Task {task.Name} ({task.ShortId})");
        
        // Add discussion
        var item = new DiscussionCreateDto()
        {
            Text = options.Message,
        };
        var result = client.Discussion.CreateTaskComments(task.Id.Value, item).Result;
        if (result == null || !result.Success)
        {
            Console.WriteLine("Task comment failed.");
            return;
        }

        Console.WriteLine($"Added discussion comment {result.Data.DiscussionCommentId}.");
    }

    private static void ListTask(ListOptions options)
    {
        var client = MakeClient(options);
        var project = client.FindProject(options.Project).Result;
        if (project == null)
        {
            return;
        }

        // Print out information about this project
        Console.WriteLine($"Project {project.Name} ({project.ShortId})");
        
        // List all tasks within this project
        var tasks = client.Task.QueryTasks(null, null, $"projectId eq {project.Id}", null, null, null).Result;
        foreach (var task in tasks.Data)
        {
            Console.WriteLine($"  {task.ShortId} {task.Name} ({task.PercentComplete}% complete)");
        }

        Console.WriteLine($"  {tasks.Data.Length} tasks.");
    }
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

    private static ProjectManagerClient MakeClient(BaseOptions options)
    {
        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("PM_API_KEY");
        var env = options.Env ?? Environment.GetEnvironmentVariable("PM_ENV");
        return ProjectManagerClient
            .WithCustomEnvironment(env)
            .WithBearerToken(apiKey)
            .WithAppName("PmTask");
    }
}