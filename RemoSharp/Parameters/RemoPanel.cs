using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Special;
namespace RemoSharp
{
    public class RemoPanel : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;
        /// <summary>
        /// Initializes a new instance of the RemoPanel class.
        /// </summary>
        public RemoPanel()
          : base("RemoPanel", "RemoPnl",
              "Remote writing into a panel",
              "RemoSharp", "RemoParams")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Panel", "Pnl", "Writing into a panel on the main remote GH_Canvas", GH_ParamAccess.list, new List<string> { "" });
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("PanelCommand", "PnlCmd", "Command to trigger writing into a panel on the main remote GH_Canvas", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Component = this;
            GrasshopperDocument = this.OnPingDocument();
            List<string> stringList = new List<string>();
            
            DA.GetDataList<string>(0, stringList);

            GH_Panel panel = (GH_Panel)this.Params.Input[0].Sources[0];
            bool multiLine = panel.Properties.Multiline;
            bool drawIndicies = panel.Properties.DrawIndices;
            bool drawPaths = panel.Properties.DrawPaths;
            bool wrap = panel.Properties.Wrap;
            GH_Panel.Alignment alignment = panel.Properties.Alignment;
            float panelSizeX = panel.Attributes.Bounds.Width;
            float panelSizeY = panel.Attributes.Bounds.Height;

            string panelString = "";
            if (stringList.Count == 1) { panelString = stringList[0]; }
            else
            {
                for (int i = 0; i < stringList.Count; i ++)
                {
                    if (i < stringList.Count - 1) panelString += stringList[i] + Environment.NewLine;
                    else panelString += stringList[i];
                }
            }
            int coordX = Convert.ToInt32(this.Component.Attributes.Pivot.X) -20;
            int coordY = Convert.ToInt32(this.Component.Attributes.Pivot.Y) - 40;
            //  command Index -->     4                  5                   6               7               8                          9                  10
            string panelOptions = multiLine + "," + drawIndicies + "," + drawPaths + "," + wrap + "," + alignment.ToString() + "," + panelSizeX + "," + panelSizeY;

            string outputData = "RemoParam,"
                + coordX + ","
                + coordY + ","
                + "WriteToPanel," + panelOptions + "," + panelString;
            DA.SetData(0, outputData);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return RemoSharp.Properties.Resources.RemoPanel.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3b36f8e1-7f27-435b-b519-8b762a2dc36d"); }
        }
    }
}