using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.CodeGen
{
    /// <summary>
    /// Renders a single View into a C# source file by substituting placeholders
    /// in the class.txt and field.txt templates loaded from Resources/CodeGen/.
    /// Per-field private/property pattern lives in field.txt; class scaffold,
    /// usings, and partial declaration live in class.txt.
    /// </summary>
    internal sealed class ViewClassWriter
    {
        private const string Namespace = "ODDB.Generated";
        private const string EntityNamespace = "TeamODD.ODDB.Runtime.Entities";
        private const string EntityBase = "ODDBEntity";

        private readonly TypeMapper _typeMapper;
        private readonly Dictionary<string, string> _batchClassNames; // ViewID → ClassName
        private readonly IODatabaseView _viewLookup;

        public ViewClassWriter(
            TypeMapper typeMapper,
            Dictionary<string, string> batchClassNames,
            IODatabaseView viewLookup)
        {
            _typeMapper = typeMapper;
            _batchClassNames = batchClassNames;
            _viewLookup = viewLookup;
        }

        public string Write(IView view, string className)
        {
            var usings = new SortedSet<string>(StringComparer.Ordinal) { EntityNamespace };
            var bodyBuilder = new StringBuilder();

            string baseClass = ResolveBaseClass(view, usings);
            EmitFields(view, bodyBuilder, usings);

            string usingsBlock = string.Join("\n", usings.Select(ns => $"using {ns};"));
            return Render(TemplateLoader.ClassTemplate, new Dictionary<string, string>
            {
                ["ViewId"] = view.ID.ToString(),
                ["Usings"] = usingsBlock,
                ["Namespace"] = Namespace,
                ["ClassName"] = className,
                ["ParentClass"] = baseClass,
                ["ClassContent"] = bodyBuilder.ToString().TrimEnd('\r', '\n'),
            });
        }

        private string ResolveBaseClass(IView view, SortedSet<string> usings)
        {
            if (view.ParentView == null)
                return EntityBase;

            // Parent in current batch: extend its generated class.
            if (_batchClassNames.TryGetValue(view.ParentView.ID.ToString(), out var className))
                return className;

            // Parent already bound: extend its existing BindType.
            if (view.ParentView.BindType != null)
            {
                if (!string.IsNullOrEmpty(view.ParentView.BindType.Namespace))
                    usings.Add(view.ParentView.BindType.Namespace);
                return view.ParentView.BindType.Name;
            }

            // Should have been caught by validation. Defensive fallback.
            return EntityBase;
        }

        private void EmitFields(IView view, StringBuilder body, SortedSet<string> usings)
        {
            foreach (var field in view.ScopedFields)
            {
                var resolved = _typeMapper.Resolve(field.Type, _viewLookup);
                if (!resolved.Ok)
                {
                    body.AppendLine($"        // unresolved field '{field.Name}': {resolved.FailureReason}");
                    continue;
                }
                if (!string.IsNullOrEmpty(resolved.Namespace))
                    usings.Add(resolved.Namespace);

                var backingName = "_" + char.ToLowerInvariant(field.Name[0]) + field.Name.Substring(1);
                body.Append(Render(TemplateLoader.FieldTemplate, new Dictionary<string, string>
                {
                    ["Type"] = resolved.TypeName,
                    ["Backing"] = backingName,
                    ["Name"] = field.Name,
                }));
            }
        }

        private static string Render(string template, IReadOnlyDictionary<string, string> vars)
        {
            var sb = new StringBuilder(template);
            foreach (var kvp in vars)
                sb.Replace("@" + kvp.Key, kvp.Value ?? string.Empty);
            return sb.ToString();
        }
    }
}
