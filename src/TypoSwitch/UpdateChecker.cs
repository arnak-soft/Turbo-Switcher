using System.Net.Http.Headers;
using System.Text.Json;

namespace TypoSwitch;

internal sealed class UpdateChecker : IDisposable
{
    private static readonly Uri LatestReleaseUri =
        new("https://api.github.com/repos/arnak-soft/Turbo-Switcher/releases/latest");

    private readonly HttpClient _http;
    private readonly System.Threading.Timer _timer;
    private readonly object _gate = new();
    private bool _busy;

    public event Action<UpdateInfo?>? UpdateAvailable;

    public UpdateChecker()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TurboSwitcher", AppVersion.Display));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _timer = new System.Threading.Timer(_ => _ = CheckAsync(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        _ = CheckAsync();
        _timer.Change(TimeSpan.FromHours(12), TimeSpan.FromHours(12));
    }

    public async Task CheckAsync()
    {
        var result = await FetchLatestUpdateAsync().ConfigureAwait(false);
        if (result.Status == UpdateFetchStatus.Available)
            UpdateAvailable?.Invoke(result.Update);
        else if (result.Status == UpdateFetchStatus.UpToDate)
            UpdateAvailable?.Invoke(null);
    }

    public async Task<UpdateFetchResult> FetchLatestUpdateAsync()
    {
        lock (_gate)
        {
            if (_busy) return new UpdateFetchResult(UpdateFetchStatus.Busy, null);
            _busy = true;
        }

        try
        {
            using var response = await _http.GetAsync(LatestReleaseUri).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new UpdateFetchResult(UpdateFetchStatus.Failed, null);

            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
            var root = doc.RootElement;

            var tag = root.GetProperty("tag_name").GetString();
            var url = root.GetProperty("html_url").GetString();
            if (!VersionParsing.TryParseTag(tag, out var latest) || string.IsNullOrWhiteSpace(url))
                return new UpdateFetchResult(UpdateFetchStatus.Failed, null);

            if (VersionParsing.IsNewer(latest, AppVersion.Current))
                return new UpdateFetchResult(UpdateFetchStatus.Available, new UpdateInfo(latest.ToString(3), url));

            return new UpdateFetchResult(UpdateFetchStatus.UpToDate, null);
        }
        catch
        {
            return new UpdateFetchResult(UpdateFetchStatus.Failed, null);
        }
        finally
        {
            lock (_gate) _busy = false;
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        _http.Dispose();
    }
}
