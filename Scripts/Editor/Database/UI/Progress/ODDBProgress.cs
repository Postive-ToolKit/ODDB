using System;
using UnityEngine;

namespace TeamODD.ODDB.Editors.UI.Progress
{
    internal static class ODDBProgress
    {
        public static void Report(IProgress<float> progress, string message, float value)
        {
            var clamped = Mathf.Clamp01(value);

            if (progress is IODDBProgressReporter reporter)
            {
                reporter.Report(message, clamped);
                return;
            }

            progress?.Report(clamped);
        }
    }
}
