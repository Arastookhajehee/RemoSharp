using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using ScriptComponents;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

namespace RemoSharp
{
    public class Output_Grip_List : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        /// <summary>
        /// Initializes a new instance of the Output_Grip class.
        /// </summary>
        public Output_Grip_List()
          : base("Output_Grip_List", "out_gr_list",
              "Place near the generated C# component and set the output index. It Grips the output of the created C# component.",
              "RemoSharp", "Output_Grip")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("RUN", "RUN", "Have the component Running.", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Output_Index", "Index", "The index of the output of the generated C# component.", GH_ParamAccess.item, 1);
            pManager.AddGenericParameter("Output_Data_L", "List", "Automatically grips the data output of the closest C# component based on the output index", GH_ParamAccess.list);

            pManager[2].Optional = true;
            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            
            pManager.AddGenericParameter("component_Output_List", "List_Out_Data", "Automatically outputs the data coming from the 'Output_Data' input", GH_ParamAccess.list);
            
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Component = this;
            GrasshopperDocument = this.OnPingDocument();

            bool RUN = false;
            int Output_Index = 1;
            List<System.Object> dataList = new List<object>();

            DA.GetData(0, ref RUN);
            DA.GetData(1, ref Output_Index);
            DA.GetDataList(2, dataList);



            if (!RUN)
            {
                this.Component.Params.Input[2].Sources.Clear();
                return;
            }

            double minDistance = double.MaxValue;
            for (int i = 0; i < this.GrasshopperDocument.ObjectCount; i++)
            {
                try
                {
                    ScriptComponents.Component_CSNET_Script component = (Component_CSNET_Script)this.GrasshopperDocument.Objects[i];

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
                DA.SetData(0, message);
            }
            else
            {


                inputIndex = Output_Index;

                var doc = this.GrasshopperDocument;
                doc.ScheduleSolution(0, ScheduleCallback);

                DA.SetDataList(0, dataList);

            }
        }

        public int inputIndex = 1;
        public int index = -1;
        public double DistanceToCsharpComponent(Component_CSNET_Script component)
        {
            double thisX = this.Component.Attributes.Pivot.X;
            double thisY = this.Component.Attributes.Pivot.Y;
            double otherX = component.Attributes.Pivot.X;
            double otherY = component.Attributes.Pivot.Y;

            double distance = Math.Sqrt((thisX - otherX) * (thisX - otherX) + (thisY - otherY) * (thisY - otherY));
            return distance;
        }

        public double DistanceToGHComponent(Grasshopper.Kernel.Parameters.Param_Geometry component)
        {
            double thisX = this.Component.Attributes.Pivot.X;
            double thisY = this.Component.Attributes.Pivot.Y;
            double otherX = component.Attributes.Pivot.X;
            double otherY = component.Attributes.Pivot.Y;

            double distance = Math.Sqrt((thisX - otherX) * (thisX - otherX) + (thisY - otherY) * (thisY - otherY));
            return distance;
        }

        public void ScheduleCallback(GH_Document doc)
        {

            ScriptComponents.Component_CSNET_Script component = (Component_CSNET_Script)this.GrasshopperDocument.Objects[index];

            int compInputIndex = inputIndex;

            var source = component.Params.Output[compInputIndex];
            var sourceID = (IGH_Param)source;

            if (component.Message != "Being Read!")
            {
                this.Component.Params.Input[2].AddSource(sourceID);
            }

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
                return RemoSharp.Properties.Resources.Output_Icons_List.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("a766248a-5e6a-4aca-a0de-256a4a24464a"); }
        }
    }
}