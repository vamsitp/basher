namespace Basher.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;

    using Basher.Models;

    using GalaSoft.MvvmLight;

    public class ItemViewModel : ViewModelBase
    {
        public static (int allTasks, int closed, int inProgress, int notStarted, float original, float completed, float remaining) GetWork(UserStory userStory)
        {
            var allTasks = userStory.Tasks.Where(x => !x.Fields.State.Equals("Removed")).ToList();
            return GetWork(allTasks);
        }

        public static (int allTasks, int closed, int inProgress, int notStarted, float original, float completed, float remaining) GetWork(IEnumerable<UserStory> userStories)
        {
            var allTasks = userStories.SelectMany(u => u.Tasks.Where(x => !x.Fields.State.Equals("Removed"))).ToList();
            return GetWork(allTasks);
        }

        private static (int allTasks, int closed, int inProgress, int notStarted, float original, float completed, float remaining) GetWork(List<WorkItem> allTasks)
        {
            var total = allTasks.Count;
            var closed = allTasks.Count(x => x.Fields.State.Equals("Closed"));
            var inProgress = allTasks.Count(x => x.Fields.State.Equals("In Progress"));
            var notStarted = allTasks.Count(x => x.Fields.State.Equals("To Do"));
            var original = allTasks.Sum(x => x.Fields.OriginalEstimate);
            var completed = allTasks.Sum(x => x.Fields.CompletedWork);
            var remaining = allTasks.Sum(x => x.Fields.RemainingWork);
            return (total, closed, inProgress, notStarted, original, completed, remaining);
        }
    }
}
