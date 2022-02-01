using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Remotify
{
    public class RemotifyToolWindow : BaseToolWindow<RemotifyToolWindow>
    {
        public override string GetTitle(int toolWindowId) => "Remotify";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FrameworkElement>(new RemotifyToolWindowControl());
        }

        [Guid("a27f9ba1-14d4-4a04-a670-bcc815e41376")]
        internal class Pane : ToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}