using System;

namespace TeamODD.ODDB.Runtime.Utils.Converters
{
    public sealed class ODDBLoadReport
    {
        public bool IsSafeToSave { get; }
        public string FailureStage { get; }
        public string FailureReason { get; }
        public long FileSize { get; }
        public int DtoTableCount { get; }
        public int DtoViewCount { get; }
        public int RestoredTableCount { get; }
        public int RestoredViewCount { get; }
        public int UnmappedFieldTypeCount { get; }
        public string SourceFormatVersion { get; }

        private ODDBLoadReport(
            bool isSafeToSave,
            string failureStage,
            string failureReason,
            long fileSize,
            int dtoTableCount,
            int dtoViewCount,
            int restoredTableCount,
            int restoredViewCount,
            int unmappedFieldTypeCount,
            string sourceFormatVersion)
        {
            IsSafeToSave = isSafeToSave;
            FailureStage = failureStage;
            FailureReason = failureReason;
            FileSize = fileSize;
            DtoTableCount = dtoTableCount;
            DtoViewCount = dtoViewCount;
            RestoredTableCount = restoredTableCount;
            RestoredViewCount = restoredViewCount;
            UnmappedFieldTypeCount = unmappedFieldTypeCount;
            SourceFormatVersion = sourceFormatVersion;
        }

        public static ODDBLoadReport Success(
            long fileSize,
            int dtoTableCount,
            int dtoViewCount,
            int restoredTableCount,
            int restoredViewCount,
            string sourceFormatVersion,
            int unmappedFieldTypeCount = 0)
            => new ODDBLoadReport(
                isSafeToSave: true,
                failureStage: ODDBLoadFailureStage.None,
                failureReason: null,
                fileSize: fileSize,
                dtoTableCount: dtoTableCount,
                dtoViewCount: dtoViewCount,
                restoredTableCount: restoredTableCount,
                restoredViewCount: restoredViewCount,
                unmappedFieldTypeCount: unmappedFieldTypeCount,
                sourceFormatVersion: sourceFormatVersion);

        public static ODDBLoadReport Failure(
            string failureStage,
            string failureReason,
            long fileSize,
            int dtoTableCount,
            int dtoViewCount,
            int restoredTableCount,
            int restoredViewCount,
            int unmappedFieldTypeCount,
            string sourceFormatVersion)
            => new ODDBLoadReport(
                isSafeToSave: false,
                failureStage: failureStage,
                failureReason: failureReason,
                fileSize: fileSize,
                dtoTableCount: dtoTableCount,
                dtoViewCount: dtoViewCount,
                restoredTableCount: restoredTableCount,
                restoredViewCount: restoredViewCount,
                unmappedFieldTypeCount: unmappedFieldTypeCount,
                sourceFormatVersion: sourceFormatVersion);
    }

    public static class ODDBLoadFailureStage
    {
        public const string None = null;
        public const string FileMissing = "file-missing";
        public const string Read = "read";
        public const string Restore = "restore";
        public const string Gzip = "gzip";
        public const string Json = "json";
        public const string EmptyDtoOnExistingFile = "empty-dto-on-existing-file";
        public const string EmptyRestoredOnNonEmptyDto = "empty-restored-on-non-empty-dto";
        public const string UnmappedFieldType = "unmapped-field-type";
    }

    public sealed class ODDBLoadException : Exception
    {
        public string FailureStage { get; }
        public string FailureReason { get; }

        public ODDBLoadException(string failureStage, string failureReason)
            : base($"ODDB load failed: stage={failureStage} reason={failureReason}")
        {
            FailureStage = failureStage;
            FailureReason = failureReason;
        }
    }
}
