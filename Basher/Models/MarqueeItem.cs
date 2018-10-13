namespace Basher.Models
{
    using Windows.UI.Xaml.Media;

    public class MarqueeItem
    {
        public MarqueeItem(string key, string value, SolidColorBrush color)
        {
            this.Key = key;
            this.Value = value;
            this.Color = color;
        }

        public string Key { get; set; }

        public string Value { get; set; }

        public SolidColorBrush Color { get; set; }
    }
}
