namespace TypoSwitch;

internal enum UpdateFetchStatus
{
    Available,
    UpToDate,
    Failed,
    Busy,
}

internal readonly record struct UpdateFetchResult(UpdateFetchStatus Status, UpdateInfo? Update);
