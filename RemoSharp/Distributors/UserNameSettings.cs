using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RemoSharp.Distributors
{
    public class UserNameSettings : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the UserNameSettings class.
        /// </summary>
        public UserNameSettings()
          : base("UserNameSettings", "user",
              "Sets and checks a message's sender username",
              "RemoSharp", "RemoMakers")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Check_Username", "check", "Check if the message is coming from the current canvas", GH_ParamAccess.item);
            pManager.AddTextParameter("Username_ID", "user", "This Canvas's Username (can be both text or number)", GH_ParamAccess.item);
            pManager.AddTextParameter("Message","Msg","Message to be sent or checked",GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("ID_Attached", "id_attached", "the message with username attached to the start", GH_ParamAccess.item);
            pManager.AddTextParameter("ID_Dettached", "id_dettached", "the message without the username attached to it", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool check = false;
            string username = "";
            string message = "";
            if (!DA.GetData(0, ref check)) return;
            if (!DA.GetData(1, ref username)) return;
            if (!DA.GetData(2, ref message)) return;

            string attached = "";
            string detached = "";

            string[] parts;
            if (message.Substring(0, 3).Equals("ID_"))
            {
                parts = message.Split(',');
                if (check) if (parts[0] == "ID_" + username) return;
                for (int i = 1; i < parts.Length; i++)
                {
                    if (i != parts.Length - 1) detached += parts[i] + ",";
                    else detached += parts[i];
                }
            }
            else 
            {
                attached = "ID_" + username + "," + message;
            }

            DA.SetData(0, attached);
            DA.SetData(1, detached);
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
                return RemoSharp.Properties.Resources.ID.ToBitmap(); ;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("FDB13750-EEE2-4EE1-BB6B-704C006EB4C8"); }
        }
    }
}