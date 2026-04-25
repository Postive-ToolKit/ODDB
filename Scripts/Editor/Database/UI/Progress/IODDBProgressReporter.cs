namespace TeamODD.ODDB.Editors.UI.Progress
{
    internal interface IODDBProgressReporter
    {
        void Report(string message, float value);
    }
}
