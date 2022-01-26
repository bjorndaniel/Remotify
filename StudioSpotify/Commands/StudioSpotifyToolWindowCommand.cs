namespace StudioSpotify
{
    [Command(PackageIds.StudioSpotifyCommand)]
    internal sealed class StudioSpotifyToolWindowCommand : BaseCommand<StudioSpotifyToolWindowCommand>
    {
        protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            return StudioSpotifyToolWindow.ShowAsync();
        }
    }
}
