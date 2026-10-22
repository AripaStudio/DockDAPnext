using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DockDAPnext.Service
{
    public static class CurrentDubFileConfigAp
    {
        public static DubConfigAp CurrentDubConfigAp { get; set; }

        // The project picked in the selector: while set, every lookup uses its dub.json.
        public static SolutionDubProject SelectedProject { get; set; }
    }
}
