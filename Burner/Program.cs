namespace Burner
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Basher.Models;

    using ColoredConsole;

    using Serilog;

    public class Program
    {
        private const string DateFormat = "dd.MMM.yy";
        private const string SpacesPrefix = "         ";

        private static readonly NameValueCollection Commands = ConfigurationManager.GetSection("commands") as NameValueCollection;

        public static List<AzureDevOpsUser> Users { get; private set; }

        private static void Main(string[] args)
        {
            var logPath = Path.Combine("./Logs", "Burner_{Date}.log");
            Log.Logger = new LoggerConfiguration().MinimumLevel.Warning().WriteTo.RollingFile(logPath, outputTemplate: "{Timestamp:dd-MMM-yyyy HH:mm:ss} | [{Level:u3}] {Message}{NewLine}{Exception}").Enrich.FromLogContext().CreateLogger();
            CheckSettings().GetAwaiter().GetResult();
            Execute(args).GetAwaiter().GetResult();
            Main(Console.ReadLine().Split(' '));
        }

        private static async Task CheckSettings()
        {
            if (string.IsNullOrWhiteSpace(AzureDevOps.Account) || string.IsNullOrWhiteSpace(AzureDevOps.Project) || string.IsNullOrWhiteSpace(AzureDevOps.Token))
            {
                ColorConsole.WriteLine("\n Please provide Azure DevOps details in the format (without braces): <Account> <Project> <PersonalAccessToken> ".Black().OnCyan());
                var details = Console.ReadLine().Split(' ');
                SetConfig(details);
            }

            Users = await AzureDevOps.GetUsers().ConfigureAwait(false);
        }

        private static void SetConfig(params string[] details)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var section = config.GetSection("appSettings") as AppSettingsSection;
            var settings = section.Settings;

            for (var i = 0; i < 3; i++) // Only 3 values required (settings.AllKeys.Length - 1)
            {
                var key = settings.AllKeys[i];
                settings[key].Value = details[i];
            }

            config.Save(ConfigurationSaveMode.Minimal);
            ConfigurationManager.RefreshSection(section.SectionInformation.Name);
        }

        private static async Task Execute(string[] args)
        {
            var command = Commands.AllKeys.SingleOrDefault(x => args.Length > 0 && Commands[x].StartsWith(args?.FirstOrDefault() ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            if (command != null)
            {
                if (args[0].StartsWith("-u", StringComparison.OrdinalIgnoreCase))
                {
                    var update = await AzureDevOps.SaveWorkItemsAsync(new WorkItem { Id = int.Parse(args[1]), Fields = new Fields { CompletedWork = float.Parse(args[3]), RemainingWork = float.Parse(args[5]) } });
                    ColorConsole.WriteLine(update ? $"{args[1]} updated! ".Green() : $" Could not update {args[1]}! ".Red());
                    await ReExecute();
                }
                else if (args[0].StartsWith("-c", StringComparison.OrdinalIgnoreCase))
                {
                    SetConfig(string.Empty, string.Empty, string.Empty);
                    await CheckSettings();
                    await ReExecute();
                }
            }
            else
            {
                var users = args.SelectMany(a => a.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)?.Select(x => x.Trim()))?.ToList();
                if (users?.Count > 0)
                {
                    foreach (var u in users)
                    {
                        try
                        {
                            var user = Users.FirstOrDefault(x => (u.Contains("@") && x.PrincipalName.Equals(u, StringComparison.OrdinalIgnoreCase)) || (!u.Contains("@") && x.PrincipalName.Split('@').FirstOrDefault().Equals(u, StringComparison.OrdinalIgnoreCase))).PrincipalName;
                            var workItems = await AzureDevOps.GetWorkItems(user);
                            var bugs = workItems?.Count(x => x.Fields.WorkItemType.Equals("Bug")).ToString();
                            var tasks = workItems?.Count(x => x.Fields.WorkItemType.Equals("Task")).ToString();
                            ColorConsole.WriteLine($"\n{workItems?.FirstOrDefault()?.Fields?.AssignedTo.ToUpperInvariant() ?? user.ToUpperInvariant()}: {workItems?.Count}".Cyan(), " (", "B: ", $" {bugs ?? string.Empty} ".White().OnRed(), " / T: ", $" {tasks ?? string.Empty} ".Black().OnYellow(), ")\n");
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
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Invalid Command");
                            ColorConsole.WriteLine("Invalid Command: " + ex.Message?.Red() ?? string.Empty);
                        }
                    }
                }
                else
                {
                    await ReExecute();
                }
            }
        }

        private static Task ReExecute()
        {
            ColorConsole.WriteLine("\n Provide a list of aliases to track (e.g.: vamsitp | vamsitp@outlook.com ) or a Command to execute (e.g.: -update 278 c 10 r 3 | -clear)".White().OnRed());
            var details = Console.ReadLine().Split(' ');
            return Execute(details);
        }
    }
}
