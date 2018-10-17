namespace Basher.Models
{
    using Windows.UI.Xaml.Media;

    public sealed class MarqueeItem
    {
        public MarqueeItem()
        {
        }

        public MarqueeItem(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }

        public MarqueeItem(string key, string value, SolidColorBrush color)
        {
            this.Key = key;
            this.Value = value;
            this.Color = color;
        }

        public string Key { get; set; }

        public string Value { get; set; }

        public SolidColorBrush Color { get; set; }

        public override bool Equals(object obj)
        {
            var item = obj as MarqueeItem;
            if (item == null)
            {
                return false;
            }

            return item.Key == this.Key && item.Value == this.Value;
        }

        public override int GetHashCode()
        {
            return this.Key.GetHashCode() ^ this.Value.GetHashCode();
        }
    }
}
