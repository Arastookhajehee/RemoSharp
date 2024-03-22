using GHCustomControls;
using Grasshopper;
using Grasshopper.Kernel;
using RemoSharp.RemoCommandTypes;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoSharp
{
    internal class AssemblyPriority : GH_AssemblyPriority
    {

        // With help from @Victor_Lin
        //https://github.com/lin-ycv/Melanoplus/blob/ca8d861a7b8d3d0ecf6080bdf5860b339aad4de6/AssemblyPriority.cs#L62
        public override GH_LoadingInstruction PriorityLoad()
        {
            //Instances.CanvasCreated += Instances_CanvasCreated;

            System.Windows.Forms.MessageBox.Show("hi", "hi", MessageBoxButtons.OK);

            return GH_LoadingInstruction.Proceed;
        }

        //private void Instances_CanvasCreated(Grasshopper.GUI.Canvas.GH_Canvas canvas)
        //{
        //    Instances.CanvasCreated -= Instances_CanvasCreated;

        //    ToolStripItemCollection items = ((ToolStrip)(Instances.DocumentEditor).Controls[0].Controls[1]).Items;
        //    items.Add(new ToolStripButton("SyncComps", (Image) Properties.Resources.SyncCanvas.ToBitmap(), onClick: (s,e) => SyncComponents_OnValueChanged(s,e))
        //    {
        //        AutoSize = true,
        //        DisplayStyle = ToolStripItemDisplayStyle.Image,
        //        ImageAlign = ContentAlignment.MiddleCenter,
        //        ImageScaling = ToolStripItemImageScaling.SizeToFit,
        //        Margin = new Padding(0, 0, 0, 0),
        //        Name = "SyncComps",
        //        Size = new Size(28, 28),
        //        ToolTipText = "Syncronize the Selected Components.",
        //    });

        //}

        //private void SyncComponents_OnValueChanged(object sender, EventArgs e)
        //{
        //    List<Guid> guids = new List<Guid>();
        //    List<string> xmls = new List<string>();
        //    List<string> docXmls = new List<string>();

        //    GH_Document thisDoc = Grasshopper.Instances.ActiveCanvas.Document;
        //    RemoSetupClient setupComp = (RemoSetupClient) thisDoc.Objects.Where(obj => obj is RemoSetupClient).FirstOrDefault();
        //    if (setupComp != null) return;

        //    var selection = thisDoc.SelectedObjects();

        //    foreach (var item in selection)
        //    {
        //        Guid itemGuid = item.InstanceGuid;

        //        string componentXML = RemoCommand.SerializeToXML(item);
        //        string componentDocXML = RemoCommand.SerizlizeToSinglecomponentDocXML(item);

        //        guids.Add(itemGuid);
        //        xmls.Add(componentXML);
        //        docXmls.Add(componentDocXML);
        //    }

        //    RemoCompSync remoCompSync = new RemoCompSync(setupComp.username, guids, xmls, docXmls);
        //    string cmdJson = RemoCommand.SerializeToJson(remoCompSync);

        //    try
        //    {
        //        int commandRepeat = 5;
        //        for (int i = 0; i < commandRepeat; i++)
        //        {
        //            setupComp.client.Send(cmdJson);
        //        }
        //    }
        //    catch
        //    {
        //        System.Windows.Forms.MessageBox.Show("RemoSharp Is Not Setup Properly!", "Connection Error", MessageBoxButtons.OK);
        //    }
            
        //}
    
    }
}
