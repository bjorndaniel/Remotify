namespace Remotify
{
    [Command(PackageIds.RemotifyCommand)]
    internal sealed class RemotifyToolWindowCommand : BaseCommand<RemotifyToolWindowCommand>
    {
        protected override Task ExecuteAsync(OleMenuCmdEventArgs e) => 
            RemotifyToolWindow.ShowAsync();
    }
}
