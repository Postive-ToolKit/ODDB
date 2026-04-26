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
    }
}
