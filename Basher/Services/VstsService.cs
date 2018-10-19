namespace Basher.Services
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Basher.Helpers;
    using Basher.Models;

    using Flurl.Http;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class VstsService : IVstsService
    {
        private static readonly string Project = App.Settings.Project;
        private static readonly string BaseUrl = App.Settings.VstsBaseUrl.OriginalString;
        private static readonly string Token = App.Settings.AccessToken;
        private static readonly string ApiVersion = App.Settings.ApiVersion;

        private const string Slash = "/";
        private const string WiqlUrl = "wit/wiql";
        private const string BugsQuery = "SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [Microsoft.VSTS.Common.Severity], [Microsoft.VSTS.Common.Priority], [Microsoft.VSTS.Common.ResolvedBy], [Microsoft.VSTS.Common.ClosedBy], [System.CreatedBy], [System.CreatedDate], [System.ChangedBy], [System.Tags] FROM WorkItems WHERE [System.TeamProject] = '{0}' AND [System.WorkItemType] = 'Bug' AND [System.State] IN ('Active', 'New'){1}ORDER BY [System.AssignedTo]";
        private const string UserStoriesQuery = @"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [System.Tags], [System.IterationPath], [Microsoft.VSTS.Scheduling.OriginalEstimate], [Microsoft.VSTS.Scheduling.RemainingWork], [Microsoft.VSTS.Scheduling.CompletedWork], [Microsoft.VSTS.Scheduling.StoryPoints] FROM WorkItemLinks WHERE ([Source].[System.TeamProject] = 'TMHGTelematics'  AND  [Source].[System.WorkItemType] = 'User Story'  AND  [Source].[System.State] = 'Committed') And ([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward') And ([Target].[System.WorkItemType] = 'Task') ORDER BY [System.Id] mode(Recursive)";
        private const string WorkItemsUrl = @"wit/workitems?ids={0}&amp;fields=System.Id,System.Title,System.AssignedTo,System.State,System.IterationPath,Microsoft.VSTS.Common.Severity,Microsoft.VSTS.Common.Priority,Microsoft.VSTS.Common.ResolvedBy,Microsoft.VSTS.Common.ClosedBy,System.CreatedBy,System.ChangedBy,System.CreatedDate&";

        public async Task<IList<WorkItem>> GetBugs(List<int> currentBugIds)
        {
            try
            {
                var apiVersion = $"api-version={ApiVersion}";
                var baseUrl = BaseUrl.EndsWith(Slash) ? BaseUrl : BaseUrl + Slash;
                var wiql = new
                {
                    query = string.Format(BugsQuery, Project, string.IsNullOrWhiteSpace(App.Settings.CustomWiqlFilter) ? " " : App.Settings.CustomWiqlFilter.Trim().Prefix("AND ").PrefixAndSuffix())
                };
                var postValue = new StringContent(JsonConvert.SerializeObject(wiql), Encoding.UTF8, "application/json");
                var result = await WiqlUrl.GetAuthRequest(Project, baseUrl, Token, apiVersion)
                        .PostAsync(postValue)
                        .ReceiveJson<JObject>()
                        .ConfigureAwait(false);

                var response = result.SelectTokens(".workItems").Values<JObject>().ToList();
                var ids = response.Select(x => x.SelectToken(".id").Value<int>());
                if (currentBugIds?.Count > 0)
                {
                    ids = ids.Union(currentBugIds);
                }

                var joinedIds = string.Join(",", ids);
                if (string.IsNullOrWhiteSpace(joinedIds))
                {
                    return null;
                }

                var bugs = await string.Format(CultureInfo.InvariantCulture, WorkItemsUrl, joinedIds.Trim(',')).GetAuthRequest(Project, baseUrl, Token, apiVersion)
                                 .GetJsonAsync<WorkItems>()
                                 .ConfigureAwait(false);
                var r = bugs.Items.ToList();
                return r;
            }
            catch (FlurlHttpException ex)
            {
                var vex = await ex.GetResponseJsonAsync<VstsException>();
                throw new FlurlHttpException(ex.Call, vex.Message, ex);
            }
        }

        public async Task<IList<UserStory>> GetUserStories(List<int> currentUserStoryIds)
        {
            try
            {
                var userStoriesList = new List<UserStory>();
                var apiVersion = $"api-version={ApiVersion}";
                var baseUrl = BaseUrl.EndsWith(Slash) ? BaseUrl : BaseUrl + Slash;
                var wiql = new
                {
                    query = string.Format(UserStoriesQuery, Project, string.IsNullOrWhiteSpace(App.Settings.CustomWiqlFilter) ? " " : App.Settings.CustomWiqlFilter.Trim().Prefix("AND ").PrefixAndSuffix())
                };
                var postValue = new StringContent(JsonConvert.SerializeObject(wiql), Encoding.UTF8, "application/json");
                var result = await WiqlUrl.GetAuthRequest(Project, baseUrl, Token, apiVersion)
                        .PostAsync(postValue)
                        .ReceiveJson<JObject>()
                        .ConfigureAwait(false);

                var response = result.SelectTokens(".workItemRelations").Values<JObject>().ToList();
                var userStoryIds = response.Where(x => x.SelectToken(".source.id") == null).Select(x => x.SelectToken(".target.id").Value<int>());
                if (currentUserStoryIds?.Count > 0)
                {
                    userStoryIds = userStoryIds.Union(currentUserStoryIds);
                }

                var userStories = await string.Format(CultureInfo.InvariantCulture, WorkItemsUrl, string.Join(",", userStoryIds).Trim(',')).GetAuthRequest(Project, baseUrl, Token, apiVersion)
                            .GetJsonAsync<UserStories>()
                            .ConfigureAwait(false);

                foreach (var userStory in userStories.Items)
                {
                    var taskIds = response.Where(x => x.SelectToken("source.id")?.Value<int>() == userStory.Id).Select(x => x.SelectToken(".target.id").Value<int>());
                    var joinedIds = string.Join(",", taskIds);
                    if (string.IsNullOrWhiteSpace(joinedIds))
                    {
                        continue;
                    }

                    var tasks = await string.Format(CultureInfo.InvariantCulture, WorkItemsUrl, joinedIds.Trim(',')).GetAuthRequest(Project, baseUrl, Token, apiVersion)
                        .GetJsonAsync<WorkItems>()
                        .ConfigureAwait(false);

                    userStory.Tasks = tasks.Items;
                    userStoriesList.Add(userStory);
                }

                return userStoriesList;
            }
            catch (FlurlHttpException ex)
            {
                var vex = await ex.GetResponseJsonAsync<VstsException>();
                throw new FlurlHttpException(ex.Call, vex.Message, ex);
            }
        }
    }
}
