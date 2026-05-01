using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamODD.ODDB.Editors.CodeGen
{
    /// <summary>
    /// Lazily loads CodeGen template assets from Resources and caches them
    /// for the lifetime of the editor AppDomain. Domain reload resets the cache.
    /// </summary>
    internal static class TemplateLoader
    {
        private static string _classTemplate;
        private static string _fieldTemplate;
        private static IReadOnlyCollection<string> _reservedWords;

        public static string ClassTemplate => _classTemplate ??= Load("CodeGen/class");
        public static string FieldTemplate => _fieldTemplate ??= Load("CodeGen/field");
        public static IReadOnlyCollection<string> ReservedWords =>
            _reservedWords ??= ParseLines(Load("CodeGen/reserved-words"));

        private static string Load(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
                throw new InvalidOperationException(
                    $"ODDB CodeGen template missing: Resources/{resourcePath}.txt — did the package install correctly?");
            return asset.text;
        }

        private static IReadOnlyCollection<string> ParseLines(string text)
        {
            var set = new HashSet<string>();
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;
                set.Add(line);
            }
            return set;
        }
    }
}
