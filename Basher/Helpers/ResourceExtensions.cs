namespace Basher.Helpers
{
    using Windows.ApplicationModel.Resources;

    internal static class ResourceExtensions
    {
        private static ResourceLoader _resLoader = new ResourceLoader();

        public static string GetLocalized(this string resourceKey)
        {
            return _resLoader.GetString(resourceKey);
        }

        // public static string GetString(this string resourceKey)
        // {
        //     var ctx = new ResourceContext();
        //     ctx.Languages = new string[] { "en-US" };
        //     var rmap = ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
        //     var value = rmap.GetValue(resourceKey, ctx)?.ValueAsString;
        //     return value;
        // }
    }
}
