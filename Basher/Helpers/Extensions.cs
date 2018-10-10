namespace Basher.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using Basher.Models;

    using Flurl.Http;

    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Animation;

    public static class Extensions
    {
        public const int MarqueeKeyPadding = 20;

        public const string Paused = "Paused";
        public const string Playing = "Playing";

        private const int RandNext = 2;
        private const int RandMin = 7;
        private const int RandMax = 23;
        private const int EdgeBuffer = 100;

        private static Random random = new Random();
        private static readonly List<Func<double, double, double, double>> ops = new List<Func<double, double, double, double>> {
            (position, time, max) =>
            {
                var result = position - (time * random.Next(RandMin, RandMax));
                if(result >= EdgeBuffer)
                {
                    return result;
                }
                else
                {
                    return ops[random.Next(RandNext)](position, time, max);
                }
            },
            (position, time, max) =>
            {
                var result = position + (time * random.Next(RandMin, RandMax));
                if(result >= max - EdgeBuffer)
                {
                    return ops[random.Next(RandNext)](position, time, max);
                }
                else
                {
                    return result;
                }
            }
        };

        public static void Animate(this FrameworkElement target, int[] times, double maxWidth, double maxHeight, bool autoReverse = true)
        {
            var top = Canvas.GetTop(target);
            var left = Canvas.GetLeft(target);

            // var storyboards = new List<Storyboard>();
            foreach (var time in times)
            {
                var storyboard = new Storyboard { AutoReverse = autoReverse, RepeatBehavior = RepeatBehavior.Forever };
                target.Animate(storyboard, time, top, ops[random.Next(2)](top, time, maxHeight), "(Canvas.Top)");
                target.Animate(storyboard, time, left, ops[random.Next(2)](left, time, maxWidth), "(Canvas.Left)");
                storyboard.Begin();
                // target.Tapped += (sender, e) =>
                // {
                //     if (target.Tag.Equals(Playing))
                //     {
                //         target.Tag = Paused;
                //         storyboard.Pause();
                //     }
                //     else
                //     {
                //         target.Tag = Playing;
                //         storyboard.Resume();
                //     }
                // };
                // storyboards.Add(storyboard);
            }

            // target.Tag = storyboards;
        }

        public static void Animate(this FrameworkElement target, Storyboard storyboard, double time, double current, double to, string property)
        {
            // var doubleAnimation = new DoubleAnimation { To = time * 20, Duration = new Duration(TimeSpan.FromSeconds(time)) };
            var doubleAnimation = new DoubleAnimation { From = current, To = to, Duration = new Duration(TimeSpan.FromSeconds(time)) };
            Storyboard.SetTarget(doubleAnimation, target);
            Storyboard.SetTargetProperty(doubleAnimation, property);
            storyboard.Children.Add(doubleAnimation);
        }

        public static void Disappear(this FrameworkElement target)
        {
            if (target == null)
            {
                return;
            }

            // target.Animate(storyboard, 2000, 0.1, "Opacity");
            var storyboard = new Storyboard { AutoReverse = false };
            var doubleAnimation = new DoubleAnimation { From = 1.0, To = 0.1, BeginTime = TimeSpan.FromSeconds(1), EasingFunction = new BounceEase { Bounces = 5 } };
            Storyboard.SetTarget(doubleAnimation, target);
            Storyboard.SetTargetProperty(doubleAnimation, "Opacity");
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
        }

        public static void Flip(this Image target)
        {
            var transform = new ScaleTransform
            {
                ScaleX = -1
            };
            target.RenderTransform = transform;
        }

        public static List<double> ToParts(this double total, int count)
        {
            var result = new List<double>();
            for (var i = 1; i <= count; i++)
            {
                result.Add((total / count) * i);
            }

            return result;
        }

        public static IFlurlRequest GetAuthRequest(this string apiRoute, string project, string baseUrl, string token, string apiVersion = "api-version=4.1")
        {
            var url = string.Format(baseUrl, project) + (apiRoute.EndsWith("?") || apiRoute.EndsWith("&") ? apiRoute : apiRoute + "?") + apiVersion;
            var pat = GetBase64Token(token);
            return url.WithHeader("Authorization", pat);
        }

        private static string GetBase64Token(string accessToken)
        {
            return "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + accessToken.TrimStart(':')));
        }

        public static string GetText(this WorkItem bug)
        {
            return bug.Id.ToString().Trim() + Environment.NewLine + bug.Fields.AssignedTo;
        }

        public static bool IsCloseTo(this Color color1, Color color2, int threshold = 48)
        {
            //int r = (int)a.R - z.R,
            //    g = (int)a.G - z.G,
            //    b = (int)a.B - z.B;
            //return (r * r + g * g + b * b) <= threshold * threshold;

            var rDist = Math.Abs(color1.R - color2.R);
            var gDist = Math.Abs(color1.G - color2.G);
            var bDist = Math.Abs(color1.B - color2.B);
            { }
            return rDist + gDist + bDist < threshold;
        }

        public static string PrefixAndSuffix(this string text, string prefixAndSuffix = " ")
        {
            text = text.Prefix(prefixAndSuffix).Suffix(prefixAndSuffix);
            return text;
        }

        public static string Prefix(this string text, string prefix = " ")
        {
            text = text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? text : prefix + text;
            return text;
        }

        public static string Suffix(this string text, string suffix = " ")
        {
            text = text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ? text : text + suffix;
            return text;
        }

        public static string ToMarqueeKey(this string key, bool upperCase = true, string nameSuffix = " > ", int leftPadding = MarqueeKeyPadding)
        {
            return (((upperCase ? key?.ToUpperInvariant() : key) ?? string.Empty) + nameSuffix).PadLeft(leftPadding);
        }

        public static List<KeyValuePair<string, string>> GetPropertyNamesAndValues(this object obj, bool nameUpperCase = true, string nameSuffix = " > ", params string[] ignoreNames)
        {
            var list = new List<KeyValuePair<string, string>>();
            foreach (var pi in obj?.GetType()?.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                var t = pi.PropertyType;
                if (t.IsPrimitive || t == typeof(decimal) || t == typeof(string) || t == typeof(DateTimeOffset))
                {
                    if (!ignoreNames.Any(pi.Name.Contains))
                    {
                        var name = pi.Name.ToMarqueeKey(nameUpperCase, nameSuffix);
                        list.Add(new KeyValuePair<string, string>(name, pi.GetValue(obj, null)?.ToString() ?? string.Empty));
                    }
                }
                else
                {
                    var val = pi.GetValue(obj, null);
                    if (val != null)
                    {
                        list.AddRange(val.GetPropertyNamesAndValues(nameUpperCase, nameSuffix, ignoreNames));
                    }
                }
            }

            return list;
        }
    }
}
