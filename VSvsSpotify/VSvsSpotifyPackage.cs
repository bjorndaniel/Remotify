global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using System.Threading;

namespace VSvsSpotify
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideToolWindow(typeof(VSvsSpotifyWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.SolutionExplorer)]
    [ProvideToolWindowVisibility(typeof(VSvsSpotifyWindow.Pane), VSConstants.UICONTEXT.SolutionExists_string)]
    [ProvideToolWindowVisibility(typeof(VSvsSpotifyWindow.Pane), VSConstants.UICONTEXT.NoSolution_string)]
    [ProvideMenuResource("Menus.ctmenu", 1)]        
    [Guid(PackageGuids.VSvsSpotifyString)]
    public sealed class VSvsSpotifyPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.RegisterCommandsAsync();
            this.RegisterToolWindows();
        }
    }
}