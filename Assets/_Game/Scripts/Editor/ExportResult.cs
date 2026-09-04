namespace ASTeams.SingleLine.Editor
{
    /// <summary>Outcome of writing a campaign to disk.</summary>
    public readonly struct ExportResult
    {
        public int ChapterCount { get; }

        public long Bytes { get; }

        /// <summary>Chapter files from an earlier, longer campaign that were deleted.</summary>
        public int RemovedStaleFiles { get; }

        /// <summary>Null when the export succeeded.</summary>
        public string Error { get; }

        public bool IsSuccess => Error == null;

        public ExportResult(int chapterCount, long bytes, int removedStaleFiles, string error)
        {
            ChapterCount = chapterCount;
            Bytes = bytes;
            RemovedStaleFiles = removedStaleFiles;
            Error = error;
        }
    }
}
