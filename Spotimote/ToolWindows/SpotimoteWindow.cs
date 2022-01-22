using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Spotimote
{
    public class SpotimoteWindow : BaseToolWindow<SpotimoteWindow>
    {
        public override string GetTitle(int toolWindowId) => "Spotimote";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FrameworkElement>(new SpotimoteWindowControl());
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