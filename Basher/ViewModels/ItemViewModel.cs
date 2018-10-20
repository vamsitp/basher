namespace Basher.ViewModels
{
    using System.Linq;

    using Basher.Models;

    using GalaSoft.MvvmLight;

    public class ItemViewModel : ViewModelBase
    {
        public static (int total, int closed, float Original, float Completed, float Remaining) GetWork(UserStory userStory)
        {
            var allTasks = userStory.Tasks.Where(x => !x.Fields.State.Equals("Removed")).ToList();
            var total = allTasks.Count;
            var closed = allTasks.Count(x => x.Fields.State.Equals("Closed"));
            var original = allTasks.Sum(x => x.Fields.OriginalEstimate);
            var completed = allTasks.Sum(x => x.Fields.CompletedWork);
            var remaining = allTasks.Sum(x => x.Fields.RemainingWork);
            return (total, closed, original, completed, remaining);
        }
    }
}
