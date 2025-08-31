using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WatchNow.Avalonia.ViewModels.Sources;
using WatchNow.Helpers;
using Newtonsoft.Json.Linq;

namespace WatchNow.Avalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    //Todo: before we can just stop using the WinForms version, we should have the following things:
    // - There's a bug where the scroll location "syncs" across separate source tabs (ie if you change
    // the scroll position on Raindrop and go to Dream, the scroll position will be the same there). I feel
    // like I've fixed this before (either on WatchNow - WinForms or in BECE)

    /* =======================================================================
     * Ideas (copied from WinForms notes):
     * - It would be cool to "sync" playback times. Like if you pause a long video, when you open that same video on a different computer, it would suggest starting at the point you left off.
     *   Things that would have to be figured out:
     *   - Where to sync "to" (a server or some other location)
     *   - How to send messages from CEF JavaScript back to the application
     *   - (Easy) How to read the stored data & check for the same video, then skip ahead
     * 
     * TODO BEFORE RELEASE:
     * - Need to figure out WebView2Loader.dll. For the Avalonia.WebView project, this is simply checked-in within a "runtimes" folder: eg https://github.com/MicroSugarDeveloperOrg/Avalonia.WebView/tree/main/Source/Platform/Windows/Microsoft.Web.WebView2.Core/runtimes
     *   But we may want to figure out a better place to get this (eg nuget package or whatever)
     * - There is a problem with SponsorBlock segements that go until the end of the video, causing the video to loop back to the beginning
     * - "new video count" should be per video, not per channel/source
     *   - maybe along with a way to "mark watched" a video to mark is as "not new" (that would also be done automatically when you play a video)
     * - There should be a way to manage the channels you want to show (a settings window)
     * =======================================================================
     */

    private string currentUrl = ""; //Todo: this is the default for now, but should maybe be empty by default
    private string textBoxText;
    private int progress;
    private int progressMax;
    private int selectedTabIndex = 0;
    private ObservableCollection<ScreenSnapViewModel> screenSnapOptions = new ObservableCollection<ScreenSnapViewModel>();
    private bool removingSource = false;
    private ObservableCollection<SourceViewModel> sources = new ObservableCollection<SourceViewModel>();
    private SourceViewModel selectedSource;

    public MainViewModel()
    {
        MiniPlayerViewModel = new MiniPlayerViewModel(this);
    }

    public string CurrentUrl
    {
        get
        {
            return currentUrl;
        }
        set
        {
            currentUrl = value;
            FirePropertyChanged();
        }
    }

    public string TextBoxText
    {
        get
        {
            return textBoxText;
        }
        set
        {
            textBoxText = value;
            FirePropertyChanged();
        }
    }

    public int SelectedTabIndex //Todo: make this an enum
    {
        get
        {
            return selectedTabIndex;
        }
        set
        {
            selectedTabIndex = value;
            FirePropertyChanged();
        }
    }

    public ObservableCollection<ScreenSnapViewModel> ScreenSnapOptions
    {
        get
        {
            return screenSnapOptions;
        }
        set
        {
            screenSnapOptions = value;
            FirePropertyChanged();
        }
    }

    public ObservableCollection<SourceViewModel> Sources
    {
        get
        {
            return sources;
        }
        set
        {
            sources = value;
            FirePropertyChanged();
        }
    }

    public SourceViewModel SelectedSource
    {
        get
        {
            return selectedSource;
        }
        set
        {
            selectedSource = value;
            FirePropertyChanged();

            if (!string.IsNullOrEmpty(selectedSource.UrlToOpen) && !removingSource /*Don't call the "LoadUrl" line below if this gets triggered while removing a source*/)
            {
                LoadUrl(selectedSource.UrlToOpen);
            }
        }
    }

    public int Progress
    {
        get
        {
            return progress;
        }
        set
        {
            progress = value;
            FirePropertyChanged();
        }
    }

    public int ProgressMax
    {
        get
        {
            return progressMax;
        }
        set
        {
            progressMax = value;
            FirePropertyChanged();
        }
    }

    public MiniPlayerViewModel MiniPlayerViewModel { get; set; }

    public Func<(int, int, int, int)> GetWindowSizeAndPosition { get; set; }

    public Action EscExitFullScreenAction { get; set; }
    public Action OpenWebViewDevToolsAction { get; set; }
    public Action SwitchToMiniPlayerAction { get; set; }
    public Action MinimizeAction { get; set; }
    public Action CloseAction { get; set; }
    public Action PlayPauseAction { get; set; }

    public void NavigateUrlFromTextBox()
    {
        LoadUrl(TextBoxText);
    }

    public void AddSourceFromTextBox()
    {
        if (AddSource(TextBoxText, out SourceViewModel source))
        {
            LoadItemsFromSourceAsync(source);
            SelectedSource = source;

            Common.ShowMessageBox("Source added", $"Source {TextBoxText} successfully added!");

            TextBoxText = "";
        }
        else
        {
            Common.ShowMessageBox("Could not add source", $"Could not add source {TextBoxText}");
        }
    }

    public void Refresh()
    {
        SelectedSource.Items.Clear();

        if (string.IsNullOrEmpty(SelectedSource.UrlToOpen))
        {
            LoadItemsFromSourceAsync(SelectedSource);
        }
    }

    public void RefreshAll() //Todo: I could probably de-dupe a lot of this method with the other LoadSources method below
    {
        ResetProgressBar();

        Task.Run(() =>
        {
            foreach (SourceViewModel source in Sources)
            {
                source.Items.Clear();

                if (string.IsNullOrEmpty(source.UrlToOpen))
                {
                    LoadItemsFromSourceAsync(source, false);
                }
            }
        });
    }

    public void PlayPause()
    {
        PlayPauseAction();
    }

    public void EscExitFullScreen()
    {
        EscExitFullScreenAction();
    }

    public void OpenWebViewDevTools()
    {
        OpenWebViewDevToolsAction();
    }

    public void SwitchToMiniPlayer()
    {
        SwitchToMiniPlayerAction();
    }

    public void MinimizeWindow()
    {
        MinimizeAction?.Invoke();
    }

    public void CloseWindow()
    {
        (int, int, int, int) mainWindowSizeAndPosition = GetWindowSizeAndPosition();
        Common.SaveSetting("FormLeft", mainWindowSizeAndPosition.Item1);
        Common.SaveSetting("FormTop", mainWindowSizeAndPosition.Item2);
        Common.SaveSetting("FormWidth", mainWindowSizeAndPosition.Item3);
        Common.SaveSetting("FormHeight", mainWindowSizeAndPosition.Item4);

        Common.SaveSetting("Sources", string.Join(",", Sources.Where(s => s.CanBeSerialized).Select(s => s is YouTubePlaylistSource ytPlaylistSource ? ytPlaylistSource.PlaylistId : s.Header)));

        CloseAction?.Invoke();
    }

    public void LoadUrl(string url)
    {
        CurrentUrl = url;
        SelectedTabIndex = 1;
    }

    public string GetSponsorBlockJSForCurrentUrl()
    {
        if (YouTubeHelper.TryParseYouTubeVideoId(CurrentUrl, out string youtubeId))
        {
            JArray sponsorSegments;
            try
            {
                sponsorSegments = Common.GetSponsorBlockSegments(youtubeId);
                if (sponsorSegments.Count <= 0)
                    return "";
            }
            catch
            {
                return "";
            }

            List<(double, double)> sponsorTimes = sponsorSegments.Select(seg => (seg["segment"][0].Value<double>(), seg["segment"][1].Value<double>())).ToList();

            //If the first ad starts at the beginning, change the start time to 2s so we have a chance to "hit it" (if it's 0s then we'll never hit it because playback
            //starts after 0s, eg 0.1s)
            if (sponsorTimes[0].Item1 < 2 && sponsorTimes[0].Item2 > 2)
            {
                //sponsorTimes[0] = (2, sponsorTimes[0].Item2);
                //Todo: I think there's some issue with this (try with https://www.youtube.com/watch?v=KY8jvFqpZ_o). Maybe instead of adjusting this at all,
                //down below in the JS construction we can have something like "else if (seg.Item1 < 2 && videoTime < seg.Item2) video.CurrentTime = seg.Item2".
                //(Though the "if seg.Item1 < 2" part might be C#, not written into the JS)
            }

            //Todo: sometimes the segments can overlap eachother, such as https://sponsor.ajay.app/api/skipSegments?videoID=y4WNxmPh_aU&categories=[%22sponsor%22,%22intro%22,%22outro%22,%22selfpromo%22,%22interaction%22]
            //We should add some logic to consolidate segments into the "outermost bounds" of overlapping segments

            //Todo: sponsor times that extend to the end of the video seem to cause the video to repeat from the beginning

            string sponsorSegmentsTooltip = "SponsorBlock segments:&#10;" + string.Join("&#10;", sponsorTimes.Select(t => $"[{t.Item1}-{t.Item2}]"));

            return "var video = document.querySelector(\"video\");\n" +
                   "video.addEventListener('playing', skipSponsor)\n" +
                   "function skipSponsor(){\n" +
                   "  var videoTime = Math.trunc(video.currentTime);\n" +
                   "  if (videoTime == -1) {}\n" + //Do nothing (this is added here so "else if" can just be repeated below)
                   string.Join("", sponsorTimes.Select(seg =>
                       $"  else if (videoTime == {Math.Round(seg.Item1)})\n" +
                       $"    video.currentTime = {seg.Item2};\n"
                   )) +
                   "\n" +
                   "  setTimeout(skipSponsor, 500);\n" +
                   "}\n" +
                   "\n" +
                   // https://stackoverflow.com/a/78922252/2246411
                   "if (window.trustedTypes && window.trustedTypes.createPolicy && !window.trustedTypes.defaultPolicy) {\n" +
                   "    window.trustedTypes.createPolicy('default', {\n" +
                   "        createHTML: string => string\n" +
                   "    });\n" +
                   "}\n" +
                   "\n" +
                   $"if ({sponsorTimes.Count} > 0 && !document.getElementById('sponsorBlockIndicator')){{\n" + //Add a icon to the player to indicate whether we've added sponsorblock for this video
                   "  var indicator = document.createElement('button');\n" +
                   "  indicator.id = 'sponsorBlockIndicator'\n" +
                   "  indicator.draggable = false;\n" +
                   "  indicator.className = 'playerButton ytp-button';\n" +
                   "  indicator.style = 'vertical-align: top';\n" +
                   $" indicator.innerHTML = '<img style=\"height: 60%; top: 0px; bottom: 0px; display: block; margin: auto;\" src=\"https://sponsor.ajay.app/LogoSponsorBlockSimple256px.png\" title=\"{sponsorSegmentsTooltip}\">';\n" +
                   "  document.querySelector('.ytp-right-controls').prepend(indicator);\n" +
                   "}";
        }
        else
        {
            return "";
        }
    }

    public void LoadSources()
    {
        ResetProgressBar();

        string[] sources = Common.ReadSetting("Sources", "").Split(',');

        Task.Run(() =>
        {
            foreach (string source in sources)
            {
                AddSource(source, out SourceViewModel sourceVM);
                if (sourceVM != null)
                {
                    LoadItemsFromSourceAsync(sourceVM, false);
                }
            }
        });
    }

    private void LoadItemsFromSourceAsync(SourceViewModel source, bool resetProgressBar = true)
    {
        if (resetProgressBar)
        {
            ResetProgressBar();
        }

        Task.Run(() =>
        {
            source.LoadItems(30,
                AddToProgressMax,
                IncrementProgressBar,
                url => LoadUrl(url))
            .ContinueWith(t =>
            {
                //Fire again after loading items to update the UI (as the UI originally gets the value before any items are loaded)
                source.FirePropertyChanged(nameof(source.HasNewItems));
                MiniPlayerViewModel.FirePropertyChanged(nameof(MiniPlayerViewModel.NewItemsCount));
            });
        });
    }

    private void ResetProgressBar()
    {
        Progress = 0;
        ProgressMax = 0;

        MiniPlayerViewModel.Progress = 0;
        MiniPlayerViewModel.ProgressMax = 0;
    }

    private void IncrementProgressBar()
    {
        Progress++;
        MiniPlayerViewModel.Progress++;
    }

    private void AddToProgressMax(int value)
    {
        ProgressMax += value;
        MiniPlayerViewModel.ProgressMax += value;
    }

    private bool AddSource(string source, out SourceViewModel addedSource)
    {
        source = source.Trim();
        addedSource = null;
        if (string.IsNullOrWhiteSpace(source))
            return false;

        if (source.ToLower() == "raindrop" || source.ToLower() == "raindrop.io")
        {
            addedSource = new RaindropSource();
            addedSource.Auth();
            AddSourceToSources(addedSource);

            return true;
        }
        else if (source.ToLower() == "/r/videos")
        {
            addedSource = new SubredditSource("/r/videos");
            addedSource.Auth();
            AddSourceToSources(addedSource);

            return true;
        }
        else if (source.ToLower() == "hulu")
        {
            addedSource = new HuluSource();
            AddSourceToSources(addedSource);
            return true;
        }
        else if (source.ToLower() == "plex")
        {
            addedSource = new PlexSource();
            AddSourceToSources(addedSource);
            return true;
        }
        else //Assume anything else is a YouTube Playlist (shouldn't call source.ToLower() here because playlist ids are case-sensitive)
        {
            try
            {
                //Don't add one that already exists
                if (Sources.Where(s => s is YouTubePlaylistSource).Cast<YouTubePlaylistSource>().Any(p => p.PlaylistId == source))
                    return false;

                addedSource = new YouTubePlaylistSource(source);
                AddSourceToSources(addedSource);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Cound not find a YouTube channel with the id \"{source}\"");
            }
        }

        return false;
    }

    private void AddSourceToSources(SourceViewModel source)
    {
        Sources.Add(source);
        source.RemoveSourceAction = () =>
        {
            removingSource = true;
            Sources.Remove(source);
            removingSource = false;
        };
    }
}
