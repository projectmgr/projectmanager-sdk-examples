using System.Reflection;
using CommandLine;
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
            .WithNotParsed(HandleErrors);
    }

    private static void ListTask(ListOptions options)
    {
        Console.WriteLine("About to create OData client");
        // Try the same thing but with OData
        var projectsClient = MakeClient(options);
        Console.WriteLine("About to query OData client");
        var projects = projectsClient.Project.QueryProjects().Result.Data;
        Console.WriteLine("About to use data from OData client");
        
        // Fetch all projects and find the one that matches locally so we can give debugging information
        var project = projects.FirstOrDefault(project =>
            project.ShortId == options.Project 
            || string.Equals(project.Name, options.Project, StringComparison.OrdinalIgnoreCase) 
            || string.Equals(project.Id.ToString(), options.Project, StringComparison.OrdinalIgnoreCase));
        if (project?.Id == null)
        {
            Console.WriteLine($"Found {projects.Count()} project(s), but none with ID, shortID, or name '{options.Project}'.");
            foreach (var item in projects)
            {
                Console.WriteLine($"    {item.ShortId} - {item.Name} ({item.Id})");
            }
        }
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
        var projects = client.Project.QueryProjects().Result;
        if (!projects.Success)
        {
            Console.WriteLine($"Could not retrieve projects: {projects.Error.Message}");
            return;
        }
        var project = projects.Data.FirstOrDefault(project =>
            project.ShortId == options.Project 
            || string.Equals(project.Name, options.Project, StringComparison.OrdinalIgnoreCase) 
            || string.Equals(project.Id.ToString(), options.Project, StringComparison.OrdinalIgnoreCase));
        if (project?.Id == null)
        {
            Console.WriteLine($"Found {projects.Data.Count()} project(s), but none with ID, shortID, or name '{options.Project}'.");
            foreach (var item in projects.Data)
            {
                Console.WriteLine($"    {item.ShortId} - {item.Name} ({item.Id})");
            }
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