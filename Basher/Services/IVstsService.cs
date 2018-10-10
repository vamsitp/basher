namespace Basher.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Basher.Models;

    public interface IVstsService
    {
        Task<IList<WorkItem>> GetBugs(List<int> currentBugIds);

        Task<IList<UserStory>> GetUserStories(List<int> currentUserStoryIds);
    }
}
