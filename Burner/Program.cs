namespace Burner
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Basher.Models;

    using Flurl.Http;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Serilog;
    using Serilog.Sinks.SystemConsole.Themes;

    public class Program
    {
        private const string BaseUrl = "https://toyotavso.visualstudio.com/TMHGTelematics/_apis/wit/";
        private const string wiqlQuery = "SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [Microsoft.VSTS.Common.Severity], [Microsoft.VSTS.Common.Priority], [Microsoft.VSTS.Common.ResolvedBy], [Microsoft.VSTS.Common.ClosedBy], [System.CreatedBy], [System.CreatedDate], [System.ChangedBy], [System.Tags] FROM WorkItems WHERE [System.TeamProject] = '{0}' AND [System.WorkItemType] IN ('Bug', 'Task') AND [System.State] IN ('Active', 'New', 'Committed', 'To Do') AND [System.AssignedTo] = '{1}' ORDER BY [System.WorkItemType]";
        private const string WiqlUrl = BaseUrl + "wiql?api-version=4.1";
        private const string WorkItemsUrl = BaseUrl + "workitems?ids={0}&amp;fields=System.Id,System.WorkItemType,System.Title,System.AssignedTo,System.State,System.IterationPath,Microsoft.VSTS.Common.Severity,Microsoft.VSTS.Common.Priority,Microsoft.VSTS.Common.ResolvedBy,Microsoft.VSTS.Common.ClosedBy,System.CreatedBy,System.ChangedBy,System.CreatedDate&api-version=4.1";
        private const string AliasPrefix = "@microsoft.com";

        private static void Main(string[] args)
        {
            var logPath = Path.Combine("./Logs", "Basher_{Date}.log");
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "{Message:lj}{NewLine}{Exception}").WriteTo.RollingFile(logPath, outputTemplate: "{Timestamp:dd-MMM-yyyy HH:mm:ss} | [{Level:u3}] {Message}{NewLine}{Exception}").Enrich.FromLogContext().CreateLogger();
            var users = args.SelectMany(a => a.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)?.Select(x => x.Trim()))?.ToList();
            if (users != null)
            {
                foreach (var u in users)
                {
                    var user = u?.EndsWith(AliasPrefix, StringComparison.OrdinalIgnoreCase) == true ? u : u + AliasPrefix;
                    var workItems = GetWorkItems(user).GetAwaiter().GetResult();
                    Log.Fatal($"\n{workItems?.FirstOrDefault()?.Fields?.AssignedTo.ToUpperInvariant() ?? user.ToUpperInvariant()}: {workItems?.Count}\n");
                    var i = 0;
                    workItems?.ForEach(wi =>
                    {
                        i++;
                        var index = i.ToString().PadLeft(2, '0');
                        Log.Information($"  [{index}.{wi.Fields.WorkItemType.FirstOrDefault()}] {wi.Id}: {wi.Fields.Title}");
                        Log.Information($"         S{wi.Fields.Severity} / P{wi.Fields.Priority}");
                        Log.Information($"         {wi.Fields.CreatedDate.ToString("dd.MMM.yy")}");
                        Log.Information($"         {wi.Fields.CompletedWork} (C) + {wi.Fields.RemainingWork} (R) ~ {wi.Fields.OriginalEstimate} (O)");
                        Console.WriteLine();
                    });
                }
            }
            else
            {
                Log.Error("Provide a list of aliases to track...");
            }

            Console.ReadLine();
        }

        public static async Task<List<WorkItem>> GetWorkItems(string user)
        {
            var pat = GetBase64Token("");
            var wiql = new
            {
                query = string.Format(wiqlQuery, "TMHGTelematics", user)
            };

            var workItemsList = new List<WorkItem>();
            try
            {
                var postValue = new StringContent(JsonConvert.SerializeObject(wiql), Encoding.UTF8, "application/json");
                var result = await WiqlUrl
                    .WithHeader("Authorization", pat)
                    .PostAsync(postValue)
                    .ReceiveJson<JObject>()
                    .ConfigureAwait(false);

                var response = result?.SelectTokens(".workItems")?.Values<JObject>()?.ToList();
                if (response != null)
                {
                    var ids = response.Select(x => x.SelectToken(".id").Value<int>());
                    var joinedIds = string.Join(",", ids);
                    if (string.IsNullOrWhiteSpace(joinedIds))
                    {
                        return null;
                    }

                    var workItems = await string.Format(CultureInfo.InvariantCulture, WorkItemsUrl, joinedIds.Trim(','))
                        .WithHeader("Authorization", pat)
                        .GetJsonAsync<WorkItems>()
                        .ConfigureAwait(false);
                    workItemsList = workItems?.Items?.ToList();
                }
            }
            catch (FlurlHttpException ex)
            {
                var vex = await ex.GetResponseJsonAsync<VstsException>();
                Log.Error(ex, vex?.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
            }

            return workItemsList;
        }

        private static string GetBase64Token(string accessToken)
        {
            return "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + accessToken.TrimStart(':')));
        }
    }
}
