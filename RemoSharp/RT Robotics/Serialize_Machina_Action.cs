using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Machina;
using Newtonsoft.Json;

namespace RemoSharp.RT_Robotics
{
    public class Serialize_Machina_Action : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Serialize_Machina_Action class.
        /// </summary>
        public Serialize_Machina_Action()
          : base("StreamMachinaAction", "Stream_M_Acts",
              "Streams Real-Time Robotics Actions from RobotExMachina to text format",
              "RemoSharp", "RT Robotics")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Actions", "Acts", "Machina Actions", GH_ParamAccess.list);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Action_Stream", "stream", "Machina  Actions List converted to text to be sent.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Machina.Action> actions = new List<Machina.Action>();

            if (!DA.GetDataList(0, actions)) return;

            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            string actsJson = JsonConvert.SerializeObject(actions, jsonSerializerSettings);

            DA.SetData(0, actsJson);
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
                return RemoSharp.Properties.Resources.Steam_Machina.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ACC0AC66-07B1-4724-86CB-319F3E6A959E"); }
        }
    }
}