﻿using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using StudioSpotify.Model;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace StudioSpotify
{
    public partial class StudioSpotifyToolWindowControl : UserControl
    {

        private bool _hasCode = false;
        private string _code = string.Empty;
        private SpotifyClient _spotifyClient;
        private Timer _timer;
        private StudioSpotifySettings _settings;
        private readonly HttpClient _httpClient;
        private string _placeHolder;
        private Guid _paneGuid = Guid.NewGuid();
        private IVsOutputWindow _outputWindow;

        public StudioSpotifyToolWindowControl()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        protected override async void OnInitialized(EventArgs e)
        {
            var dir = Path.GetDirectoryName(typeof(StudioSpotifyPackage).Assembly.Location);
            var json = Path.Combine(dir, "Resources", "settings.json");
            if (!string.IsNullOrWhiteSpace(json))
            {
                _settings = JsonConvert.DeserializeObject<StudioSpotifySettings>(File.ReadAllText(json));
                if (!string.IsNullOrWhiteSpace(_settings?.AccessToken))
                {
                    BtnConnect.Visibility = Visibility.Collapsed;
                }
                await ActivateSpotifyAsync();

            }
            dir = Path.GetDirectoryName(typeof(StudioSpotifyPackage).Assembly.Location);
            _placeHolder = Path.Combine(dir, "Resources", "logo.png");
            AlbumImage.Source = new BitmapImage(new Uri(_placeHolder));
            base.OnInitialized(e);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            PanelError.Visibility = Visibility.Collapsed;
            WebBrowser.Visibility = Visibility.Visible;
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
                    AlbumImage.Source = new BitmapImage(new Uri(_placeHolder));
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
            catch(APIException e)
            {
                _timer.Stop();
                Track.Text = "In preview mode and invite only.";
                Artist.Text = e.Response?.Body?.ToString() ?? "";
                Album.Text= "The extension is awaiting Spotify approval.";
            }
            catch (Exception e)
            {
                if (e.Message.Contains("expired"))
                {
                    await RefreshTokenAsync();
                }
                
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (_outputWindow == null)
                {
                    _outputWindow = (IVsOutputWindow)Package.GetGlobalService(typeof(SVsOutputWindow));
                    _outputWindow.CreatePane(ref _paneGuid, "Studio Spotify", 1, 1);
                }
                _outputWindow.GetPane(ref _paneGuid, out var outputPane);
                outputPane.Activate();
                outputPane.OutputString(e.ToString());
            }
        }

        private async Task CreateSpotifyClientAsync()
        {
            try
            {
                if (_settings != null && string.IsNullOrEmpty(_settings?.AccessToken))
                {
                    var dto = new StudioSpotifyDTO
                    {
                        Code = _code,
                        Type = 0,
                    };
                    var content = new StringContent(JsonConvert.SerializeObject(dto), System.Text.Encoding.UTF8, "application/json");
                    var result = await _httpClient.PostAsync($"{_settings!.StudioSpotifyBackend}", content);
                    var response = JsonConvert.DeserializeObject<StudioSpotifyDTO>(await result.Content.ReadAsStringAsync());

                    _settings.AccessToken = response?.AccessToken ?? string.Empty;
                    _settings.RefreshToken = response?.RefreshToken ?? string.Empty;
                    _settings.Expires = DateTimeOffset.UtcNow.AddSeconds(response?.ExpiresIn ?? 0);
                    var dir = Path.GetDirectoryName(typeof(StudioSpotifyPackage).Assembly.Location);
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
                    _timer = new Timer(TimeSpan.FromSeconds(5).TotalMilliseconds);
                    _timer.Elapsed += Timer_Elapsed;
                    _timer.Start();
                }
                PanelError.Visibility = Visibility.Collapsed;
                WebBrowser.Visibility = Visibility.Collapsed;
                NowPlaying.Visibility = Visibility.Visible;
            }
            catch (Exception e)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                if(_outputWindow == null)
                {
                    _outputWindow = (IVsOutputWindow)Package.GetGlobalService(typeof(SVsOutputWindow));
                    _outputWindow.CreatePane(ref _paneGuid, "Studio Spotify", 1, 1);
                }
                _outputWindow.GetPane(ref _paneGuid, out var outputPane);
                outputPane.Activate();
                outputPane.OutputString(e.ToString());
                WebBrowser.Visibility = Visibility.Collapsed;
                PanelError.Visibility = Visibility.Visible;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_spotifyClient != null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (_settings!.Expires.HasValue && Math.Abs(_settings.Expires.Value.UtcDateTime.Subtract(DateTimeOffset.UtcNow.UtcDateTime).TotalSeconds) < 120)
                {
                    await RefreshTokenAsync();

                }
                else
                {
                    await GetCurrentlyPlayingAsync();
                }
            }
        }

        private async Task RefreshTokenAsync()
        {
            try
            {
                var dto = new StudioSpotifyDTO
                {
                    Type = 1,
                    AccessToken = _settings!.AccessToken,
                    RefreshToken = _settings!.RefreshToken,
                };
                var content = new StringContent(JsonConvert.SerializeObject(dto));
                var result = await _httpClient.PostAsync($"{_settings!.StudioSpotifyBackend}", content);
                var response = JsonConvert.DeserializeObject<StudioSpotifyDTO>(await result.Content.ReadAsStringAsync());
                _settings.AccessToken = response?.AccessToken ?? string.Empty;
                _settings.Expires = DateTimeOffset.UtcNow.AddSeconds(response?.ExpiresIn ?? 0);
                var dir = Path.GetDirectoryName(typeof(StudioSpotifyPackage).Assembly.Location);
                var json = Path.Combine(dir, "Resources", "settings.json");
                File.WriteAllText(json, JsonConvert.SerializeObject(_settings));
                _spotifyClient = new SpotifyClient(_settings.AccessToken);
            }
            catch (Exception e)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (_outputWindow == null)
                {
                    _outputWindow = (IVsOutputWindow)Package.GetGlobalService(typeof(SVsOutputWindow));
                    _outputWindow.CreatePane(ref _paneGuid, "Studio Spotify", 1, 1);
                }
                _outputWindow.GetPane(ref _paneGuid, out var outputPane);
                outputPane.Activate();
                outputPane.OutputString(e.ToString());
                WebBrowser.Visibility = Visibility.Collapsed;
                PanelError.Visibility = Visibility.Visible;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        private async void WebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {

            if (e.Uri.ToString().Contains("http://localhost:5000") && !_hasCode)
            {
                PanelError.Visibility = Visibility.Collapsed;
                var queryDictionary = HttpUtility.ParseQueryString(e.Uri.Query);
                _code = queryDictionary["code"] ?? string.Empty;
                _hasCode = true;
                await CreateSpotifyClientAsync();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        private async void Previous_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _spotifyClient?.Player.SkipPrevious();
            }
            catch (Exception) { }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
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
                    AlbumImage.Source = new BitmapImage(new Uri(_placeHolder));
                    Album.Text = string.Empty;
                    Artist.Text = string.Empty;
                    Track.Text = "No player active";
                }
            }
            catch (Exception) { }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
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