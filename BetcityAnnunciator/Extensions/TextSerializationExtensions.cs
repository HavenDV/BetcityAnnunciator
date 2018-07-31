using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BetcityAnnunciator.Extensions
{
    public static class TextSerializationExtensions
    {
        #region Serialize/Deserialize as text

        private static char SerializeAsTextSeparator { get; } = ';';

        public static string SerializeAsText(this object obj)
        {
            var lines = new List<string>();

            var type = obj.GetType();
            foreach (var info in type.GetProperties())
            {
                var value = info.GetValue(obj, null);

                string stringValue;
                if (value is List<string> listOfStrings)
                {
                    stringValue = string.Join(SerializeAsTextSeparator.ToString(), listOfStrings.ToArray());
                }
                else if (value is Enum enumValue)
                {
                    stringValue = enumValue.ToString("G");
                }
                else
                {
                    stringValue = value?.ToString() ?? string.Empty;
                }

                lines.Add($"{info.Name}: {stringValue}");
            }

            return string.Join(Environment.NewLine, lines.ToArray());
        }

        public static T DeserializeFromText<T>(this string text) where T : new()
        {
            var obj = new T();

            var lines = text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.None);
            foreach (var info in obj.GetType().GetProperties())
            {
                var type = info.PropertyType;
                var line = lines.FirstOrDefault(i => i.StartsWith(info.Name))?.Replace(info.Name, string.Empty).Substring(2);
                if (!info.CanWrite || line == null)
                {
                    continue;
                }

                object value = null;
                if (type == typeof(string))
                {
                    value = !string.IsNullOrEmpty(line) ? line : default(string);
                }
                else if (type == typeof(bool))
                {
                    value = bool.TryParse(line, out var result) && result;
                }
                else if (type == typeof(int))
                {
                    value = int.TryParse(line, out var result) ? result : default(int);
                }
                else if (type.IsEnum)
                {
                    value = Enum.Parse(type, line);
                }
                else if (type == typeof(List<string>))
                {
                    value = line.Split(SerializeAsTextSeparator).ToList();
                }

                info.SetValue(obj, value, null);
            }

            return obj;
        }

        #endregion

        #region IO

        public static T LoadFromText<T>(this string path) where T : new()
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("File is not found", path);
            }

            var text = File.ReadAllText(path);

            return text.DeserializeFromText<T>();
        }

        public static void SaveAsText(this object obj, string path)
        {
            var text = obj.SerializeAsText();

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, text);
        }

        #endregion
    }
}
