namespace Basher.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;

    using Basher.Models;

    using GalaSoft.MvvmLight;

    public class ItemViewModel : ViewModelBase
    {
        public static (int allTasks, int closedTasks, float original, float completed, float remaining) GetWork(UserStory userStory)
        {
            var allTasks = userStory.Tasks.Where(x => !x.Fields.State.Equals("Removed")).ToList();
            var total = allTasks.Count;
            var closed = allTasks.Count(x => x.Fields.State.Equals("Closed"));
            var original = allTasks.Sum(x => x.Fields.OriginalEstimate);
            var completed = allTasks.Sum(x => x.Fields.CompletedWork);
            var remaining = allTasks.Sum(x => x.Fields.RemainingWork);
            return (total, closed, original, completed, remaining);
        }

        public static (int allTasks, int closedTasks, float original, float completed, float remaining) GetWork(IEnumerable<UserStory> userStories)
        {
            var allTasks = userStories.SelectMany(u => u.Tasks.Where(x => !x.Fields.State.Equals("Removed"))).ToList();
            var total = allTasks.Count;
            var closed = allTasks.Count(x => x.Fields.State.Equals("Closed"));
            var original = allTasks.Sum(x => x.Fields.OriginalEstimate);
            var completed = allTasks.Sum(x => x.Fields.CompletedWork);
            var remaining = allTasks.Sum(x => x.Fields.RemainingWork);
            return (total, closed, original, completed, remaining);
        }
    }
}
