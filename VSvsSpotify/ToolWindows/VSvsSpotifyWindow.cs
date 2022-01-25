using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace VSvsSpotify
{
    public class VSvsSpotifyWindow : BaseToolWindow<VSvsSpotifyWindow>
    {
        public override string GetTitle(int toolWindowId) => "VSvsSpotifyBackend";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FrameworkElement>(new VSvsSpotifyWindowControl());
        }

        [Guid("7051fb93-60b5-48e8-b3b7-1aa0be2bd23c")]
        internal class Pane : ToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}