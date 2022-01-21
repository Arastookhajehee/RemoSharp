using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Linq;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Parameters.Hints;
using Grasshopper.Kernel.Special;
using ScriptComponents;

namespace RemoSharp
{
    public class EavesDrop : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        /// <summary>
        /// Initializes a new instance of the EavesDrop class.
        /// </summary>
        public EavesDrop()
          : base("EavesDrop", "EavesDrop",
              "Exctracts scripting code from the closest C# script component on the canvas.",
              "RemoSharp", "RemoSharpScript")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("InputTrigger", "Input", "Updates this component with every input data change. This input is not inlcuded in any calculation and the sole purpose of it is to trigger the component when the source changes.", GH_ParamAccess.item,"");
            pManager.AddBooleanParameter("Manual_Update", "Update", "Seeks C# components on the canvas.", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("UsingCode", "UsingCode", "Exports the library reference 'using' code section of the source data C# component.", GH_ParamAccess.item);
            pManager.AddTextParameter("Script", "Script", "Exports the main function script body of the source C# component.", GH_ParamAccess.item);
            pManager.AddTextParameter("AdditionalCode", "AdditionalCode", "Exports the custom additional code section of the source C# component.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Component = this;
            GrasshopperDocument = this.OnPingDocument();

            string update = "";
            bool manual = false;
            DA.GetData(0, ref update);
            DA.GetData(1, ref manual);

            string Using_Code = "";
            string Script_Code = "";
            string Additional_Code = "";

            double minDistance = double.MaxValue;
            int index = -1;
            for (int i = 0; i < GrasshopperDocument.ObjectCount; i++)
            {
                try
                {
                    ScriptComponents.Component_CSNET_Script component = (Component_CSNET_Script) GrasshopperDocument.Objects[i];

                    component.Message = "";


                    var pivot = component.Attributes.Pivot;
                    double distanceToThisComponent = DistanceToCsharpComponent(component);
                    if (distanceToThisComponent < minDistance && distanceToThisComponent != 0)
                    {
                        minDistance = distanceToThisComponent;
                        index = i;
                    }
                }
                catch
                {
                }
            }
            if (index == -1)
            {
                string message = "Create a C# component on the canvas and bring this component close to it";
                Using_Code = message;
                Script_Code = message;
                Additional_Code = message;
            }
            else
            {
                if (manual) {
                    string message = "Looking for the closest C# component to listen to.";
                    Using_Code = message;
                    Script_Code = message;
                    Additional_Code = message;
                }
                Component_CSNET_Script scriptComponent = (Component_CSNET_Script) GrasshopperDocument.Objects[index];
                scriptComponent.Message = "Being Read!";
                Using_Code = scriptComponent.ScriptSource.UsingCode;
                Script_Code = scriptComponent.ScriptSource.ScriptCode;
                Additional_Code = scriptComponent.ScriptSource.AdditionalCode;
            }

            DA.SetData(0, Using_Code);
            DA.SetData(1, Script_Code);
            DA.SetData(2, Additional_Code);
        }

        public double DistanceToCsharpComponent(Component_CSNET_Script component)
        {
            double thisX = this.Component.Attributes.Pivot.X;
            double thisY = this.Component.Attributes.Pivot.Y;
            double otherX = component.Attributes.Pivot.X;
            double otherY = component.Attributes.Pivot.Y;

            double distance = Math.Sqrt((thisX - otherX) * (thisX - otherX) + (thisY - otherY) * (thisY - otherY));
            return distance;
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
                return RemoSharp.Properties.Resources.EavesDropIcon.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("47d299f1-f4aa-47b7-a1ce-2bb3e4e07593"); }
        }
    }
}