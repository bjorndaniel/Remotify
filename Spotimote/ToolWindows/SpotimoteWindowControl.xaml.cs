using Newtonsoft.Json;
using SpotifyAPI.Web;
using Spotimote.Model;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Spotimote
{
    public partial class SpotimoteWindowControl : UserControl
    {
        private bool _hasCode = false;
        private string _code = string.Empty;
        private SpotifyClient _spotifyClient;
        private Timer? _timer;
        private SpotimoteSettings _settings;
        private HttpClient _httpClient;

        public SpotimoteWindowControl()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
        }

        protected override async void OnInitialized(EventArgs e)
        {
            var dir = Path.GetDirectoryName(typeof(SpotimotePackage).Assembly.Location);
            var json = Path.Combine(dir, "Resources", "settings.json");
            if (!string.IsNullOrWhiteSpace(json))
            {
                _settings = JsonConvert.DeserializeObject<SpotimoteSettings>(File.ReadAllText(json));
                if (!string.IsNullOrWhiteSpace(_settings?.AccessToken))
                {
                    BtnConnect.Visibility = Visibility.Collapsed;
                }
                await ActivateSpotifyAsync();

            }
            base.OnInitialized(e);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            BtnConnect.Visibility = Visibility.Collapsed;
            await ActivateSpotifyAsync();
        }

        private async Task ActivateSpotifyAsync()
        {
            if (!_hasCode)
            {
                var loginRequest = new LoginRequest(new Uri("http://localhost:5000"), _settings?.ClientId ?? "", LoginRequest.ResponseType.Code)
                {
                    Scope = new[] { Scopes.PlaylistReadPrivate,
                 Scopes.PlaylistReadCollaborative,
                 Scopes.UserReadPrivate,
                 Scopes.UserReadEmail,
                 Scopes.Streaming,
                 Scopes.UserModifyPlaybackState,
                 Scopes.AppRemoteControl,
                 Scopes.PlaylistModifyPublic,
                 Scopes.UserReadPlaybackState,
                 Scopes.UserReadPlaybackPosition }
                };
                WebBrowser.Navigate(loginRequest.ToUri());
            }
            else
            {
                await CreateSpotifyClientAsync();
            }
        }

        private async Task GetCurrentlyPlayingAsync()
        {
            try
            {
                var result = await _spotifyClient!.Player!.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
                if (result == null)
                {
                    Artist.Text = "";
                    Album.Text = "";
                    Track.Text = "No player active";
                    AlbumImage.Source = new BitmapImage(new Uri("pack://application:,,,/Spotimote;component/Resources/logo.png"));
                }
                else if (result?.Item?.Type == ItemType.Track)
                {
                    var track = result.Item as FullTrack;
                    Artist.Text = track!.Artists.ToList().Count() > 1 ? track.Artists.Select(_ => _.Name).Aggregate((a, b) => $"{a}, {b}") : track.Artists[0].Name;
                    Track.Text = track.Name;
                    Album.Text = track.Album.Name;
                    AlbumImage.Source = new BitmapImage(new Uri(track.Album.Images.FirstOrDefault()?.Url ?? ""));
                }
                else if (result?.Item?.Type == ItemType.Episode)
                {
                    var episode = result.Item as FullEpisode;
                    Artist.Text = episode?.Show?.Name;
                    Track.Text = episode?.Name;
                    Album.Text = episode?.Show?.Description;
                    AlbumImage.Source = new BitmapImage(new Uri(episode?.Show?.Images?.FirstOrDefault()?.Url ?? ""));
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("expired"))
                {
                    await RefreshTokenAsync();
                }
            }
        }

        private async Task CreateSpotifyClientAsync()
        {
            if (_settings != null && string.IsNullOrEmpty(_settings?.AccessToken))
            {
                var dto = new SpotimoteDTO
                {
                    Code = _code,
                    Type = 0,
                };
                var content = new StringContent(JsonConvert.SerializeObject(dto), System.Text.Encoding.UTF8, "application/json");
                var result = await _httpClient.PostAsync($"{_settings!.SpotimoteBackend}", content);
                var response = JsonConvert.DeserializeObject<SpotimoteDTO>(await result.Content.ReadAsStringAsync());

                _settings.AccessToken = response?.AccessToken ?? string.Empty;
                _settings.RefreshToken = response?.RefreshToken ?? string.Empty;
                _settings.Expires = DateTimeOffset.UtcNow.AddSeconds(response?.ExpiresIn ?? 0);
                var dir = Path.GetDirectoryName(typeof(SpotimoteWindowControl).Assembly.Location);
                var json = Path.Combine(dir, "Resources", "settings.json");
                File.WriteAllText(json, JsonConvert.SerializeObject(_settings));
            }
            else if (_settings != null && _settings!.Expires.HasValue && Math.Abs(_settings.Expires.Value.UtcDateTime.Subtract(DateTimeOffset.UtcNow.UtcDateTime).TotalSeconds) < 120)
            {
                await RefreshTokenAsync();
            }
            if (_spotifyClient == null)
            {
                _spotifyClient = new SpotifyClient(_settings!.AccessToken);
                _timer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);
                _timer.Elapsed += Timer_Elapsed;
                _timer.Start();
            }
            WebBrowser.Visibility = Visibility.Collapsed;
            NowPlaying.Visibility = Visibility.Visible;
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_spotifyClient != null)
            {
                await Dispatcher.BeginInvoke(async () =>
                {
                    if (_settings!.Expires.HasValue && Math.Abs(_settings.Expires.Value.UtcDateTime.Subtract(DateTimeOffset.UtcNow.UtcDateTime).TotalSeconds) < 120)
                    {
                        await RefreshTokenAsync();

                    }
                    await GetCurrentlyPlayingAsync();
                });
            }
        }

        private async Task RefreshTokenAsync()
        {
            var dto = new SpotimoteDTO
            {
                Type = 1,
                AccessToken = _settings!.AccessToken,
                RefreshToken = _settings!.RefreshToken,
            };
            var content = new StringContent(JsonConvert.SerializeObject(dto));
            var result = await _httpClient.PostAsync($"{_settings!.SpotimoteBackend}", content);
            var response = JsonConvert.DeserializeObject<SpotimoteDTO>(await result.Content.ReadAsStringAsync());
            _settings.AccessToken = response?.AccessToken ?? string.Empty;
            _settings.Expires = DateTimeOffset.UtcNow.AddSeconds(response?.ExpiresIn ?? 0);
            var dir = Path.GetDirectoryName(typeof(SpotimoteWindowControl).Assembly.Location);
            var json = Path.Combine(dir, "Resources", "settings.json");
            File.WriteAllText(json, JsonConvert.SerializeObject(_settings));
            _spotifyClient = new SpotifyClient(_settings.AccessToken);
        }

        private async void WebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {

            if (e.Uri.ToString().Contains("http://localhost:5000") && !_hasCode)
            {
                BtnConnect.Visibility = Visibility.Collapsed;
                var queryDictionary = HttpUtility.ParseQueryString(e.Uri.Query);
                _code = queryDictionary["code"] ?? string.Empty;
                _hasCode = true;
                await CreateSpotifyClientAsync();
            }
        }

        private async void Previous_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _spotifyClient?.Player.SkipPrevious();
            }
            catch (Exception) { }
        }

        private async void Play_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await _spotifyClient!.Player!.GetCurrentPlayback();
                if (result != null)
                {
                    var success = false;
                    if (result.IsPlaying)
                    {
                        success = await _spotifyClient!.Player!.PausePlayback();
                    }
                    else
                    {
                        success = await _spotifyClient!.Player!.ResumePlayback();
                    }
                }
                else
                {
                    Album.Text = string.Empty;
                    Artist.Text = string.Empty;
                    Track.Text = "No player active";
                }
            }
            catch (Exception) { }
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _spotifyClient?.Player.SkipNext();
            }
            catch (Exception) { }
        }
    }
}