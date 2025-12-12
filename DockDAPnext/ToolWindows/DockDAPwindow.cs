using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DockDAPnext
{
    public class DockDAPwindow : BaseToolWindow<DockDAPwindow>
    {
        public override string GetTitle(int toolWindowId) => "DockDAPwindow";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FrameworkElement>(new DockDAPwindowControl());
        }

        [Guid("c3052301-4ed9-4edd-b49d-6cc3f9c173aa")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}
