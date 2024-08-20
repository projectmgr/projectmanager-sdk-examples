using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using PmTask.Sync;
using ProjectManager.SDK.Models;
using RestSharp;
using RestSharp.Authenticators.OAuth2;

namespace PmTask.SonarClient;

public class SonarClient
{
    private static JsonSerializerOptions _jsonOptions = new ()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNameCaseInsensitive = true,
    };

    private string _sonarApiKey;
    private RestClientOptions _options;
    private RestClient _client;

    public SonarClient(string apiKey)
    {
        _sonarApiKey = apiKey;
        _options = new RestClientOptions("https://sonarcloud.io/api")
        {
            Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(_sonarApiKey, "Bearer")
        };
        _client = new RestClient(_options);
    }

    /// <summary>
    /// Fetch as many pages as required to load all items
    /// </summary>
    /// <param name="projectCode"></param>
    /// <returns></returns>
    public async Task<List<SonarHotspot>?> GetHotspots(string projectCode)
    {
        var results = new List<SonarHotspot>();
        int pageNumber = 1;
        while (true)
        {
            if (pageNumber % 10 == 0)
            {
                // To avoid the risk that this program runs forever, put a slight delay in every 10 pages
                // and allow the user to hit Ctrl+C
                Console.WriteLine($"Fetching SonarCloud hotspots for '{projectCode}' - page {pageNumber}...");
                await Task.Delay(1000);
            }

            // Fetch this page - if it fails we are done
            var page = await GetHotspotPage(projectCode, pageNumber);
            if (page == null)
            {
                return null;
            }

            // Add these items and see if we're at the end
            results.AddRange(page.hotspots);
            if (page.hotspots.Length < page.paging.pageSize)
            {
                break;
            }
            pageNumber++;
        }

        // Here's your result
        return results;
    }


    /// <summary>
    /// Fetch as many pages as required to load all items
    /// </summary>
    /// <param name="projectCode"></param>
    /// <returns></returns>
    public async Task<List<SonarIssue>?> GetIssues(string projectCode)
    {
        var results = new List<SonarIssue>();
        int pageNumber = 1;
        while (true)
        {
            if (pageNumber % 10 == 0)
            {
                // To avoid the risk that this program runs forever, put a slight delay in every 10 pages
                // and allow the user to hit Ctrl+C
                Console.WriteLine($"Fetching SonarCloud issues for '{projectCode}' - page {pageNumber}...");
                await Task.Delay(1000);
            }

            // Fetch this page - if it fails we are done
            var page = await GetIssuePage(projectCode, pageNumber);
            if (page == null)
            {
                return null;
            }

            // Add these items and see if we're at the end
            results.AddRange(page.issues);
            if (page.issues.Length < page.paging.pageSize)
            {
                break;
            }
            pageNumber++;
        }

        // Here's your result
        return results;
    }

    private async Task<SonarHotspotResponse?> GetHotspotPage(string projectCode, int pageNumber)
    {
        var request = new RestRequest($"hotspots/search?projectKey={projectCode}&p={pageNumber}");
        var response = await _client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            Console.WriteLine($"Failed to query SonarCloud: {response.Content}");
            return null;
        }

        // Print out what we found
        SonarHotspotResponse? sonar = null;
        try
        {
            sonar = JsonSerializer.Deserialize<SonarHotspotResponse>(response.Content ?? string.Empty, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Parsing exception: {ex.Message}");
        }

        if (sonar == null)
        {
            Console.WriteLine($"Failed to parse SonarCloud response: {response.Content}");
            return null;
        }

        return sonar;
    }


    private async Task<SonarIssueResponse?> GetIssuePage(string projectCode, int pageNumber)
    {
        var request = new RestRequest($"issues/search?projects={projectCode}&p={pageNumber}");
        var response = await _client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            Console.WriteLine($"Failed to query SonarCloud: {response.Content}");
            return null;
        }

        // Print out what we found
        SonarIssueResponse? sonar = null;
        try
        {
            sonar = JsonSerializer.Deserialize<SonarIssueResponse>(response.Content ?? string.Empty, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Parsing exception: {ex.Message}");
        }

        if (sonar == null)
        {
            Console.WriteLine($"Failed to parse SonarCloud response: {response.Content}");
            return null;
        }

        return sonar;
    }

    public async Task<List<RemoteSystemTaskModel>> RetrieveAllTasks(string scProjectCode)
    {
        var list = new List<RemoteSystemTaskModel>();

        // Retrieve hotspots from SonarCloud
        // not sure why SonarCloud classifies "issues" and "hotspots" as different things but that's just how it is
        var hotspots = await GetHotspots(scProjectCode);
        if (hotspots != null)
        {

            // Print results
            Console.WriteLine($"Found {hotspots.Count} hotspots in SonarCloud for the project {scProjectCode}.");

            // Scan hotspots to determine what tasks to create
            foreach (var item in hotspots)
            {
                var taskCreate = new TaskCreateDto()
                {
                    Name = $"[{item.vulnerabilityProbability}] - {item.message}",
                    Description =
                        $"- **Problem**: {HttpUtility.HtmlEncode(item.message)}\n" +
                        $"- **Source**: {item.component.Replace(scProjectCode + ":", "")} Line {item.line}\n" +
                        $"- **Detected**: {item.creationDate}\n" +
                        $"- **Blame**: {item.author}\n" +
                        $"- **Link**: https://sonarcloud.io/project/security_hotspots?id={scProjectCode}&hotspots={item.key}\n" +
                        $"- **SonarCloud ID**: {item.key}",
                    // TODO: TaskCreate doesn't allow you to set color yet
                    // Color = GetColor(item.vulnerabilityProbability),
                };
                list.Add(new RemoteSystemTaskModel() { UniqueId = item.key, TaskCreate = taskCreate });
            }
        }

        // Now handle issues
        var issues = await GetIssues(scProjectCode);
        if (issues != null)
        {
            Console.WriteLine($"Found {issues.Count} issues in SonarCloud for the project {scProjectCode}.");

            // Analyze issues to see which ones to create
            foreach (var item in issues)
            {
                var taskCreate = new TaskCreateDto()
                {
                    Name = $"[{item.severity}] - {item.message}",
                    Description =
                        $"- **Problem**: {HttpUtility.HtmlEncode(item.message)}\n" +
                        $"- **Source**: {item.component.Replace(scProjectCode + ":", "")} Line {item.line}\n" +
                        $"- **Detected**: {item.creationDate}\n" +
                        $"- **Blame**: {item.author}\n" +
                        $"- **Link**: https://sonarcloud.io/project/issues?resolved=false&id={scProjectCode}&open={item.key}\n" +
                        $"- **SonarCloud ID**: {item.key}",
                    // TODO: TaskCreate doesn't allow you to set color yet
                    // Color = GetColor(item.vulnerabilityProbability),
                };
                list.Add(new RemoteSystemTaskModel() { UniqueId = item.key, TaskCreate = taskCreate });
            }
        }

        return list;
    }
}
