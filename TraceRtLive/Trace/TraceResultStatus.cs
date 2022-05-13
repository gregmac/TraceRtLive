namespace TraceRtLive.Trace
{
    public enum TraceResultStatus
    {
        /// <summary>Currently waiting for result</summary>
        InProgress,

        /// <summary>Intermediate hop</summary>
        HopResult,

        /// <summary>Final destination reply</summary>
        FinalResult,

        /// <summary>Removed lookup (irrelevant, eg already have a reply from earlier attempt)</summary>
        Obsolete,
    }
}
