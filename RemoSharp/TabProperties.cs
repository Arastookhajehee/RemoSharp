using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel;

namespace RemoSharp
{
    internal class TabProperties : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            var server = Grasshopper.Instances.ComponentServer;

            server.AddCategoryShortName("RemoSharp", "RS");
            server.AddCategorySymbolName("RemoSharp", 'R');
            server.AddCategoryIcon("RemoSharp", Properties.Resources.RemoSharpIcon);

            return GH_LoadingInstruction.Proceed;
        }

    }
}
