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
    
    [Verb("discuss", HelpText = "Add a comment to a task within a project")]
    private class DiscussOptions : BaseOptions
    {
        [Option("project", HelpText = "The name, ID, or ShortID of the project in which the task")]
        public string? Project { get; set; }
        
        [Option("task", HelpText = "The name, ID, or ShortID of the task to comment upon")]
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
            .WithNotParsed(HandleErrors);
    }

    private static void DiscussTask(DiscussOptions obj)
    {
        throw new NotImplementedException();
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
        if (project == null)
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