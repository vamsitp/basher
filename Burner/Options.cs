namespace Burner
{
    using System.Collections.Generic;

    using CommandLine;
    using CommandLine.Text;

    public class Options
    {
        ////[Option('v', "verbose", Required = false, HelpText = "Prints all messages to standard output.")]
        ////public bool Verbose { get; set; }
    }

    [Verb("setup", HelpText = "Set default Azure DevOps account (-a <accountName> -p <projectName>) -t <pat>")]
    class SetupOptions : Options
    {
        [Option('a', "account", Required = true, HelpText = "Azure DevOps Account/Org")]
        public string Account { get; set; }

        [Option('p', "project", Required = false, HelpText = "Azure DevOps Project")]
        public string Project { get; set; }

        [Option('t', "token", Required = false, HelpText = "Azure DevOps PAT (Token)")]
        public string Token { get; set; }

        [Usage(ApplicationAlias = "burner")]

        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("\n\nSet Azure DevOps Account", new SetupOptions { Account = "vamsitp", Project = "VstsDemoGenerator", Token = "ds2f3m35a56s7i78l8w8efksjdcbsklfhwuie" });
            }
        }
    }

    [Verb("update", HelpText = "Update Work-item (-i <id> -c <completedWork>) -r <remainingWork>)")]
    class UpdateItemOptions : Options
    {
        [Option('i', "id", Required = true, HelpText = "ID")]
        public int Id { get; set; }

        [Option('c', "completed", Required = true, HelpText = "Completed Work")]
        public int CompletedWork { get; set; }

        [Option('r', "remaining", Required = true, HelpText = "Remaining Work")]
        public int RemainingWork { get; set; }

        [Usage(ApplicationAlias = "burner")]

        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("\nUpdate Work-item", UnParserSettings.WithGroupSwitchesOnly(), new UpdateItemOptions { Id = 213, CompletedWork = 4, RemainingWork = 12 });
            }
        }
    }

    [Verb("items", HelpText = "Get User-assignments (-u <space-delimited aliass>)")]
    class UserAssignmentOptions : Options
    {
        [Option('u', "users", Required = true, HelpText = "Users")]
        public IEnumerable<string> Users { get; set; }

        [Usage(ApplicationAlias = "burner")]

        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("\nGet Work-items for Users", UnParserSettings.WithGroupSwitchesOnly(), new UserAssignmentOptions { Users = new[] { "vamsitp", "someone@outlook.com" } });
            }
        }
    }

    [Verb("details", HelpText = "Get Work-item details (-i <ids>)")]
    class DetailsOptions : Options
    {
        [Option('i', "ids", Required = true, HelpText = "IDs")]
        public IEnumerable<string> Ids { get; set; }

        [Usage(ApplicationAlias = "burner")]

        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("\nGet Item Details", UnParserSettings.WithGroupSwitchesOnly(), new DetailsOptions { Ids = new[] { "213", "546" } });
            }
        }
    }

    [Verb("clear", HelpText = "Clear account settings")]
    class ClearSettingsOptions : Options
    {
        [Usage(ApplicationAlias = "burner")]

        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("\nClear account settings", UnParserSettings.WithGroupSwitchesOnly(), new ClearSettingsOptions());
            }
        }
    }
}
