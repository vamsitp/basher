namespace Burner
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Basher.Models;

    using ColoredConsole;

    using Flurl.Http;
    using Flurl.Http.Content;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Serilog;

    public class AzureDevOps
    {
        private const string MediaType = "application/json";
        private const string AuthHeader = "Authorization";
        private const string BasicAuthHeaderPrefix = "Basic ";
        private const string WorkItemsTokenPath = ".workItems";
        private const string IdTokenPath = ".id";
        private const char WorkItemsDelimiter = ',';
        private const char Colon = ':';
        private const string wiqlQuery = "SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [Microsoft.VSTS.Common.Severity], [Microsoft.VSTS.Common.Priority], [Microsoft.VSTS.Common.ResolvedBy], [Microsoft.VSTS.Common.ClosedBy], [System.CreatedBy], [System.CreatedDate], [System.ChangedBy], [System.Tags] FROM WorkItems WHERE [System.TeamProject] = '{0}' AND [System.WorkItemType] IN ('Bug', 'Task') AND [System.State] IN ('Active', 'New', 'Committed', 'To Do') AND [System.AssignedTo] = '{1}' ORDER BY [System.WorkItemType]";

        private static string BaseUrl => $"https://{Account}.visualstudio.com/{Project}/_apis/wit";
        private static string UsersUrl => $"https://vssps.dev.azure.com/{Account}/_apis/graph/users?api-version={ApiVersion}-preview";
        private static string WiqlUrl => $"{BaseUrl}/wiql?api-version={ApiVersion}";
        private static string WorkItemsUrl => $"{BaseUrl}/workitems?ids={{0}}&amp;fields=System.Id,System.WorkItemType,System.Title,System.AssignedTo,System.State,System.IterationPath,Microsoft.VSTS.Common.Severity,Microsoft.VSTS.Common.Priority,Microsoft.VSTS.Common.ResolvedBy,Microsoft.VSTS.Common.ClosedBy,System.CreatedBy,System.ChangedBy,System.CreatedDate&api-version={ApiVersion}";
        private static string WorkItemUpdateUrl => $"{BaseUrl}/workItems/{{0}}?api-version={ApiVersion}";

        public static NameValueCollection Accounts => ConfigurationManager.GetSection("accounts") as NameValueCollection;

        public static string[] DefaultAccount => ConfigurationManager.AppSettings[nameof(DefaultAccount)]?.Split('^');

        public static string Account => DefaultAccount?.FirstOrDefault();

        public static string Project => DefaultAccount?.LastOrDefault();

        public static string Token => Accounts[ConfigurationManager.AppSettings[nameof(DefaultAccount)]];

        private static string ApiVersion => ConfigurationManager.AppSettings[nameof(ApiVersion)];

        public static async Task<List<WorkItem>> GetWorkItems(string user)
        {
            var pat = GetBase64Token(Token);
            var wiql = new
            {
                query = string.Format(wiqlQuery, Project.Trim(), user.Trim())
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

        public static async Task<List<AzureDevOpsUser>> GetUsers()
        {
            var pat = GetBase64Token(Token);
            var usersList = new List<AzureDevOpsUser>();
            try
            {
                var result = await UsersUrl
                    .WithHeader(AuthHeader, pat)
                    .GetJsonAsync<AzureDevOpsUsers>()
                    .ConfigureAwait(false);

                usersList = result?.Items?.ToList();
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

            return usersList;
        }

        public static async Task<bool> SaveWorkItemsAsync(WorkItem workItem)
        {
            var pat = GetBase64Token(Token);
            var ops = new[]
                {
                    new Op { op = "add", path = "/fields/Microsoft.VSTS.Scheduling.CompletedWork", value = workItem.Fields.CompletedWork },
                    new Op { op = "add", path = "/fields/Microsoft.VSTS.Scheduling.RemainingWork", value = workItem.Fields.RemainingWork }
                }.ToList();

            var content = new CapturedStringContent(ops?.ToJson(), Encoding.UTF8, "application/json-patch+json");
            try
            {
                var result = await string.Format(CultureInfo.InvariantCulture, WorkItemUpdateUrl, workItem.Id)
                            .WithHeader(AuthHeader, pat)
                            .PatchAsync(content)
                            .ConfigureAwait(false);

                var success = result.StatusCode == System.Net.HttpStatusCode.OK;
                return success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to save Work-item");
                return false;
            }
        }

        private static void LogError(Exception ex, string message)
        {
            var fex = ex as FlurlHttpException;
            if (fex != null)
            {
                var vex = fex.GetResponseJsonAsync<VstsException>().GetAwaiter().GetResult();
                message = vex?.Message ?? ex.Message;
            }

            Log.Error(ex, message);
            ColorConsole.WriteLine(message?.Red() ?? string.Empty);
        }

        private static string GetBase64Token(string accessToken)
        {
            return BasicAuthHeaderPrefix + Convert.ToBase64String(Encoding.ASCII.GetBytes(Colon + accessToken.TrimStart(Colon)));
        }

        private class Op
        {
            public string op { get; set; }

            public string path { get; set; }

            public dynamic value { get; set; }
        }
    }
}
