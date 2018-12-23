namespace Burner
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Basher.Models;

    using ColoredConsole;

    using CommandLine;

    using Serilog;

    public class Program
    {
        private const string DateFormat = "dd.MMM.yy";
        private const string SpacesPrefix = "         ";

        public static List<AzureDevOpsUser> Users { get; private set; }

        private static void Main(string[] args)
        {
            var logPath = Path.Combine("./Logs", "Burner_{Date}.log");
            Log.Logger = new LoggerConfiguration().MinimumLevel.Warning().WriteTo.RollingFile(logPath, outputTemplate: "{Timestamp:dd-MMM-yyyy HH:mm:ss} | [{Level:u3}] {Message}{NewLine}{Exception}").Enrich.FromLogContext().CreateLogger();
            while (true)
            {
                var result = Parser.Default
                            .ParseArguments<AddAccountOptions, UserAssignmentOptions, UpdateItemOptions, DefaultAccountOptions, ClearSettingsOptions >(args)
                            .MapResult(
                            (AddAccountOptions opts) => AddAccount(opts.Account, opts.Project, opts.Token),
                            (UserAssignmentOptions opts) => GetUserAssignments(opts.Users).GetAwaiter().GetResult(),
                            (UpdateItemOptions opts) => UpdateItem(opts.Id, opts.CompletedWork, opts.RemainingWork).GetAwaiter().GetResult(),
                            (DefaultAccountOptions opts) => SetDefault(opts.Account, opts.Project),
                            (ClearSettingsOptions opts) => AddAccount(opts.Account, opts.Project, string.Empty),
                            errs => HandleParseErrors(errs?.ToList()));

                Log.Information("Result: " + result);
                args = Console.ReadLine()?.Trim()?.Split(' ');
            }
        }

        private static async Task<int> GetUserAssignments(IEnumerable<string> users)
        {
            await CheckSettings();
            if (users?.Any() == true)
            {
                foreach (var u in users)
                {
                    try
                    {
                        var user = Users?.FirstOrDefault(x => (u?.Contains("@") == true && x?.PrincipalName?.Equals(u, StringComparison.OrdinalIgnoreCase) == true) || ((u?.Contains("@") != true) && x?.PrincipalName?.Split('@')?.FirstOrDefault()?.Equals(u, StringComparison.OrdinalIgnoreCase) == true))?.PrincipalName ?? u;
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

            return 0;
        }

        private static async Task<int> UpdateItem(int id, float completedWork, float remainingWork)
        {
            await CheckSettings();
            var update = await AzureDevOps.SaveWorkItemsAsync(new WorkItem { Id = id, Fields = new Fields { CompletedWork = completedWork, RemainingWork = remainingWork } });
            ColorConsole.WriteLine(update ? $"{id} updated! ".Green() : $" Could not update {id}! ".Red());
            return 0;
        }

        private static int HandleParseErrors(List<Error> errs)
        {
            if (errs.Count > 1)
            {
                errs.ToList().ForEach(e => Log.Error(e.ToString()));
            }

            return errs.Count;
        }

        private static async Task CheckSettings()
        {
            if (string.IsNullOrWhiteSpace(AzureDevOps.Account) || string.IsNullOrWhiteSpace(AzureDevOps.Project) || string.IsNullOrWhiteSpace(AzureDevOps.Token))
            {
                ColorConsole.WriteLine("\n Please provide Azure DevOps details using the 'setup' option".Black().OnCyan());
                var details = Console.ReadLine().Split(' ');
                Parser.Default
                            .ParseArguments<AddAccountOptions>(details)
                            .MapResult(
                            (AddAccountOptions opts) => AddAccount(opts.Account, opts.Project, opts.Token),
                            errs => HandleParseErrors(errs?.ToList()));
            }

            if (Users == null || Users.Count == 0)
            {
                Users = await AzureDevOps.GetUsers().ConfigureAwait(false);
            }
        }

        private static int SetDefault(string account, string project)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var section = config.GetSection("accounts") as AppSettingsSection;
            var settings = section.Settings;
            var key = account + "^" + project;
            var current = config.AppSettings.Settings[nameof(AzureDevOps.DefaultAccount)].Value;
            if (current.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                LogMessage("Default already set!");
                return 0;
            }
            else if (settings.AllKeys.Contains(key))
            {
                config.AppSettings.Settings[nameof(AzureDevOps.DefaultAccount)].Value = key;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
                Users = null;
                LogMessage("Default set!");
                return 0;
            }

            Log.Warning("Please 'add' the Account/Project first to set as Default!");
            return -1;
        }

        private static int AddAccount(string account, string project, string token)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var section = config.GetSection("accounts") as AppSettingsSection;
            var settings = section.Settings;
            var key = account + "^" + project;
            if (key.Equals("^"))
            {
                settings.Clear();
                LogMessage("Accounts cleared!");
            }
            else if (settings.AllKeys.Contains(key))
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    settings.Remove(key);
                    LogMessage("Account removed!");
                }
                else
                {
                    settings[key].Value = token;
                    LogMessage("Account updated!");
                }
            }
            else
            {
                settings.Add(key, token);
                LogMessage("Accounts added!");
            }

            config.AppSettings.Settings[nameof(AzureDevOps.DefaultAccount)].Value = key;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(section.SectionInformation.Name);
            ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
            Users = null;
            return 0;
        }

        private static void LogMessage(string message)
        {
            Log.Information(message);
            ColorConsole.WriteLine(message.Green());
        }
    }
}
