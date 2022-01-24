using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RemoSharp
{
    public class ClientCanvasBounds : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ClientCanvasBounds class.
        /// </summary>
        public ClientCanvasBounds()
          : base("Canvas Bounds", "Client_Cvs",
              "Retrieves info from the client's GH_Canvas active bounds and broadcasts it as a string.",
              "RemoSharp", "BroadcastTools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("ShiftX", "X", "Shift the X coordinate of the GH_Canvas", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("ShiftY", "Y", "Shift the Y coordinate of the GH_Canvas", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("HighResolution", "HighRes", "If True, it will map a 1:1 scale GH_Canvas bounds. " +
                "Note that High-Res means that the screen image will take longer to generate and may result in performance and latency issues.",
                GH_ParamAccess.item,
                false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("CanvasBounds", "bounds", "A text based representation of the current GH_Canvas active region bounds",
                GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region Checking for GH_Canvas Zoom
            // Checking for the Zoom Level of GH
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
            #endregion

            this.Message = "Need a Trigger for RT";
            int x = 0;
            int y = 0;
            bool highRes = false;

            DA.GetData(0, ref x);
            DA.GetData(1, ref y);
            DA.GetData(2, ref highRes);


            double scale = 50;
            if (highRes) 
            {
                scale = 100;
            }
            

            var thisCanvas = Grasshopper.Instances.ActiveCanvas;

            // getting the active region of the grasshopper canvas
            var thisCanvasViewPort = thisCanvas.Viewport;
            var visRg = thisCanvasViewPort.VisibleRegion;
            var coords1 = Convert.ToInt32((visRg.X + visRg.Width) * thisCanvasViewPort.Zoom);
            var coords2 = Convert.ToInt32((visRg.Y + visRg.Height) * thisCanvasViewPort.Zoom);

            int visRgX = Convert.ToInt32(visRg.X);
            int visRgY = Convert.ToInt32(visRg.Y);

            visRgY += 0;
            visRgX += 0;

            int xPos = x - 15;
            int yPos = y + 161;

            if (visRgX < 0)
            {
                xPos += -visRgX;
                visRgX = 0;

            }
            if (visRgY < 0)
            {
                yPos += -visRgY;
                visRgY = 0;
            }
            var viewPortCorners = visRgY + "," + coords2 + "," + visRgX + "," + coords1 + "," + thisCanvasViewPort.Zoom;


            viewPortCorners += "," + xPos + "," + yPos + "," + scale;
            // getting where the gh window is and what is its size

            DA.SetData(0, viewPortCorners);
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
                return RemoSharp.Properties.Resources.CanvasBounds.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("da6c5669-3b2c-47aa-9534-b993480adb2b"); }
        }
    }
}