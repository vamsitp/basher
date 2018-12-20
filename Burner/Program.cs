namespace Burner
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Basher.Models;
    using ColoredConsole;
    using Flurl.Http;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Serilog;

    public class Program
    {
        private const string MediaType = "application/json";
        private const string DateFormat = "dd.MMM.yy";
        private const string AuthHeader = "Authorization";
        private const string BasicAuthHeaderPrefix = "Basic ";
        private const string WorkItemsTokenPath = ".workItems";
        private const string IdTokenPath = ".id";
        private const char WorkItemsDelimiter = ',';
        private const char Colon = ':';
        private const string SpacesPrefix = "         ";
        private const string wiqlQuery = "SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [Microsoft.VSTS.Common.Severity], [Microsoft.VSTS.Common.Priority], [Microsoft.VSTS.Common.ResolvedBy], [Microsoft.VSTS.Common.ClosedBy], [System.CreatedBy], [System.CreatedDate], [System.ChangedBy], [System.Tags] FROM WorkItems WHERE [System.TeamProject] = '{0}' AND [System.WorkItemType] IN ('Bug', 'Task') AND [System.State] IN ('Active', 'New', 'Committed', 'To Do') AND [System.AssignedTo] = '{1}' ORDER BY [System.WorkItemType]";

        private static readonly string BaseUrl = $"https://{Account}.visualstudio.com/{Project}/_apis/wit";
        private static readonly string WiqlUrl = $"{BaseUrl}/wiql?api-version={ApiVersion}";
        private static readonly string WorkItemsUrl = $"{BaseUrl}/workitems?ids={{0}}&amp;fields=System.Id,System.WorkItemType,System.Title,System.AssignedTo,System.State,System.IterationPath,Microsoft.VSTS.Common.Severity,Microsoft.VSTS.Common.Priority,Microsoft.VSTS.Common.ResolvedBy,Microsoft.VSTS.Common.ClosedBy,System.CreatedBy,System.ChangedBy,System.CreatedDate&api-version={ApiVersion}";

        private static string DefaultUserDomain => ConfigurationManager.AppSettings[nameof(DefaultUserDomain)];

        private static string ApiVersion => ConfigurationManager.AppSettings[nameof(ApiVersion)];

        private static string Account => ConfigurationManager.AppSettings[nameof(Account)];

        private static string Project => ConfigurationManager.AppSettings[nameof(Project)];

        private static string Token => ConfigurationManager.AppSettings[nameof(Token)];

        private static void Main(string[] args)
        {
            var logPath = Path.Combine("./Logs", "Burner_{Date}.log");
            Log.Logger = new LoggerConfiguration().MinimumLevel.Warning().WriteTo.RollingFile(logPath, outputTemplate: "{Timestamp:dd-MMM-yyyy HH:mm:ss} | [{Level:u3}] {Message}{NewLine}{Exception}").Enrich.FromLogContext().CreateLogger();
            CheckSettings();
            Execute(args).GetAwaiter().GetResult();
            Main(Console.ReadLine().Split(' '));
        }

        private static void CheckSettings()
        {
            if (string.IsNullOrWhiteSpace(Account) || string.IsNullOrWhiteSpace(Project) || string.IsNullOrWhiteSpace(Token))
            {
                ColorConsole.WriteLine("\n Please provide Azure DevOps details in the format (without braces): <Account> <Project> <PersonalAccessToken> ".Black().OnCyan());
                var details = Console.ReadLine().Split(' ');
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var section = config.Sections.OfType<AppSettingsSection>().FirstOrDefault();
                var settings = section.Settings;

                for (var i = 0; i < settings.AllKeys.Length - 2; i++) // Only 3 values required
                {
                    var key = settings.AllKeys[i];
                    settings[key].Value = details[i];
                }

                config.Save(ConfigurationSaveMode.Minimal);
                ConfigurationManager.RefreshSection(section.SectionInformation.Name);
            }
        }

        private static async Task Execute(string[] args)
        {
            var users = args.SelectMany(a => a.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)?.Select(x => x.Trim()))?.ToList();
            if (users?.Count > 0)
            {
                foreach (var u in users)
                {
                    var user = u?.EndsWith(DefaultUserDomain, StringComparison.OrdinalIgnoreCase) == true ? u : u + DefaultUserDomain;
                    var workItems = await GetWorkItems(user);
                    var bugs = workItems?.Count(x => x.Fields.WorkItemType.Equals("Bug")).ToString();
                    var tasks = workItems?.Count(x => x.Fields.WorkItemType.Equals("Task")).ToString();
                    ColorConsole.WriteLine($"\n{workItems?.FirstOrDefault()?.Fields?.AssignedTo.ToUpperInvariant() ?? user.ToUpperInvariant()}: {workItems?.Count}".Cyan(), " (" , $" {bugs ?? string.Empty} ".White().OnRed(), " / ", $" {tasks ?? string.Empty} ".Black().OnYellow(), ")\n");
                    var i = 0;
                    workItems?.ForEach(wi =>
                    {
                        i++;
                        var createdDate = wi.Fields.CreatedDate.ToString(DateFormat);
                        var elapsed = new StringBuilder($" {createdDate} ");
                        for (var e = 1; e < DateTimeOffset.Now.Subtract(wi.Fields.CreatedDate).Days - createdDate.Length; e++)
                        {
                            elapsed.Append(" ");
                        }

                        var completed = $" {wi.Fields.CompletedWork} (C) ".White().OnDarkGreen();
                        var remaining = $" {wi.Fields.RemainingWork} (R) ".White().OnDarkRed();
                        var original = $" {wi.Fields.OriginalEstimate} (O) ".White().OnDarkGray();

                        var severity = $" {wi.Fields.Severity} ".White().OnDarkMagenta();
                        var priority = $" P{wi.Fields.Priority} ".White().OnDarkCyan();

                        var index = i.ToString().PadLeft(2, '0');
                        var wiType = $"[{index}.{wi.Fields.WorkItemType.FirstOrDefault()}]";
                        ColorConsole.WriteLine($"  ", wiType.EndsWith("B]", StringComparison.OrdinalIgnoreCase) ? wiType.Red() : wiType.Yellow(), " ", $" {wi.Id} ".Black().OnWhite(), $" {wi.Fields.Title}");
                        ColorConsole.WriteLine(SpacesPrefix, elapsed.ToString().OnRed());
                        ColorConsole.WriteLine(SpacesPrefix, severity, " / ", priority);
                        ColorConsole.WriteLine(SpacesPrefix, completed, " + ", remaining, " / ", original);
                        Console.WriteLine();
                    });
                }
            }
            else
            {
                ColorConsole.WriteLine("\n Provide a list of aliases to track ".White().OnRed());
                var details = Console.ReadLine().Split(' ');
                await Execute(details);
            }
        }

        public static async Task<List<WorkItem>> GetWorkItems(string user)
        {
            var pat = GetBase64Token(Token);
            var wiql = new
            {
                query = string.Format(wiqlQuery, Project, user)
            };

            var workItemsList = new List<WorkItem>();
            try
            {
                var postValue = new StringContent(JsonConvert.SerializeObject(wiql), Encoding.UTF8, MediaType);
                var result = await WiqlUrl
                    .WithHeader(AuthHeader, pat)
                    .PostAsync(postValue)
                    .ReceiveJson<JObject>()
                    .ConfigureAwait(false);

                var response = result?.SelectTokens(WorkItemsTokenPath)?.Values<JObject>()?.ToList();
                if (response != null)
                {
                    var ids = response.Select(x => x.SelectToken(IdTokenPath).Value<int>());
                    var joinedIds = string.Join(WorkItemsDelimiter, ids);
                    if (string.IsNullOrWhiteSpace(joinedIds))
                    {
                        return null;
                    }

                    var workItems = await string.Format(CultureInfo.InvariantCulture, WorkItemsUrl, joinedIds.Trim(WorkItemsDelimiter))
                        .WithHeader(AuthHeader, pat)
                        .GetJsonAsync<WorkItems>()
                        .ConfigureAwait(false);
                    workItemsList = workItems?.Items?.ToList();
                }
            }
            catch (FlurlHttpException ex)
            {
                var vex = await ex.GetResponseJsonAsync<VstsException>();
                LogError(ex, vex?.Message);
            }
            catch (Exception ex)
            {
                LogError(ex, ex.Message);
            }

            return workItemsList;
        }

        private static void LogError(Exception ex, string message)
        {
            Log.Error(ex, message);
            ColorConsole.WriteLine(message?.Red() ?? string.Empty);
        }

        private static string GetBase64Token(string accessToken)
        {
            return BasicAuthHeaderPrefix + Convert.ToBase64String(Encoding.ASCII.GetBytes(Colon + accessToken.TrimStart(Colon)));
        }
    }
}
