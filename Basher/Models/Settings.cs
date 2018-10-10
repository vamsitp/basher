namespace Basher.Models
{
    using System;
    using System.Linq;

    using Newtonsoft.Json;

    public class Settings
    {
        [JsonIgnore]
        public Uri VstsBaseUrl => new Uri($"https://{this.Account}.visualstudio.com/{this.Project}/_apis/");

        public string Account { get; set; }

        public string Project { get; set; }

        public string CustomWiqlFilter { get; set; }

        public string AccessToken { get; set; }

        public string CriticalityField { get; set; }

        [JsonIgnore]
        public string Criticality => this.CriticalityField.FirstOrDefault().ToString()?.ToUpperInvariant() ?? "S";

        public string ApiVersion { get; set; }

        public double RefreshIntervalInSecs { get; set; }

        public string SpeechLocale { get; set; }

        public string Background { get; set; }

        public string SpeechRecognizerName { get; set; }
    }
}
