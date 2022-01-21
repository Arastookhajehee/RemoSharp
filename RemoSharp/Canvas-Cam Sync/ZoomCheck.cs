using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace RemoSharp
{
    public class ZoomCheck : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ZoomCheck class.
        /// </summary>
        public ZoomCheck()
          : base("ZoomCheck", "Zoom",
              "Checks if the zoom level of the canvas is set to 1.",
              "RemoSharp", "BroadcastTools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Info", "Info", "This component makes sure the user's canvas zoom level is correct.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            var textList = new List<string>();
            var GH_Objects = this.OnPingDocument().Objects;
            for (int i = 0; i < GH_Objects.Count; i++)
            {
                try
                {
                    IGH_Component comp = (IGH_Component)GH_Objects[i];
                    string componentType = comp.GetType().ToString();
                    if (componentType.Equals("RemoSharp.RemoCompSource"))
                    {
                        string zoomOutMessage = "Zoom Out Please";
                        string zoomInMessage = "Zoom in Please";
                        var zoomLevel = Grasshopper.Instances.ActiveCanvas.Viewport.Zoom;
                        if (zoomLevel > 1)
                        {
                            comp.Message = zoomOutMessage;
                            comp.ClearRuntimeMessages();
                            comp.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, zoomOutMessage);

                        }
                        else if (zoomLevel < 1)
                        {
                            comp.ClearRuntimeMessages();
                            comp.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, zoomInMessage);
                            comp.Message = zoomInMessage;
                        }
                        else
                        {
                            comp.ClearRuntimeMessages();
                            comp.Message = "";
                        }
                    }
                    textList.Add(componentType);
                }
                catch { }
            }
            DA.SetData(0, "This component makes sure the user's canvas zoom level is correct.");
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
                return RemoSharp.Properties.Resources._1_1.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("c2eef1ba-6f08-4ea9-a131-372849f4fd81"); }
        }
    }
}