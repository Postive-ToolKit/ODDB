using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TeamODD.ODDB.Editors.Utils.Sheets.Validation
{
    public sealed class SheetValidationReport
    {
        private readonly List<SheetValidationIssue> _issues = new();

        public IReadOnlyList<SheetValidationIssue> Issues => _issues;
        public bool HasErrors => _issues.Any(issue => issue.Severity == SheetValidationSeverity.Error);
        public int ErrorCount => _issues.Count(issue => issue.Severity == SheetValidationSeverity.Error);
        public int WarningCount => _issues.Count(issue => issue.Severity == SheetValidationSeverity.Warning);

        public void Add(SheetValidationIssue issue)
        {
            if (issue != null)
                _issues.Add(issue);
        }

        public string ToSummaryString(int maxIssues = 10)
        {
            var sb = new StringBuilder();
            sb.Append("Sheet import validation found ");
            sb.Append(ErrorCount);
            sb.Append(" error(s), ");
            sb.Append(WarningCount);
            sb.Append(" warning(s).");

            foreach (var issue in _issues.Take(maxIssues))
            {
                sb.AppendLine();
                sb.Append("- ");
                sb.Append(issue);
            }

            if (_issues.Count > maxIssues)
            {
                sb.AppendLine();
                sb.Append("- ... ");
                sb.Append(_issues.Count - maxIssues);
                sb.Append(" more issue(s)");
            }

            return sb.ToString();
        }

        public string ToFailureString(int maxIssues = 25)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Import cannot continue because the source contains structural errors.");
            sb.AppendLine();
            sb.AppendLine(ToSummaryString(maxIssues));
            sb.AppendLine();
            sb.AppendLine("How to fix:");
            sb.AppendLine($"- Keep {SheetConfig.ROW_NAME_MARKER} as the first row of every table block.");
            sb.AppendLine("- Give every data row a non-empty, unique ID in the ID column.");
            sb.AppendLine("- Keep each imported field header unique and include every selected table/group.");
            sb.Append("Extra source columns and missing local columns are supported and do not block import.");
            return sb.ToString();
        }
    }
}
