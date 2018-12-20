namespace Burner
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Serilog;

    public static class Extensions
    {
        public static string ToJson(this object source, Formatting formatting = Formatting.Indented, JsonSerializerSettings jsonSerializerSettings = null, bool camelizePropertyNames = false)
        {
            if (jsonSerializerSettings == null)
            {
                jsonSerializerSettings = new JsonSerializerSettings
                {
                    Error = (o, e) =>
                    {
                        var currentError = e.ErrorContext.Error.Message;
                        Log.Warning(currentError);
                        e.ErrorContext.Handled = true;
                    },
                    ContractResolver = camelizePropertyNames ? new CamelCasePropertyNamesContractResolver() : new DefaultContractResolver()
                };
            }

            return source != null ? JsonConvert.SerializeObject(source, formatting, jsonSerializerSettings) : string.Empty;
        }
    }
}
