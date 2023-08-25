using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

using Rhino.Geometry;
using Rhino.Commands;
using Grasshopper.Kernel.Parameters;
using System.Linq;
using RemoSharp.RemoCommandTypes;

namespace RemoSharp.Utilities
{


    /// <summary>
    /// 
    /// </summary>
    public class ListItemAccess : GH_Component, IGH_VariableParameterComponent
    {


        private string m_dataTest = "";



        public string DataTest
        {
            get { return m_dataTest; }
            set
            {
                m_dataTest = value;
                Message = m_dataTest;

            }
        }



        /// <summary>
        /// Initializes a new instance of the ProgramAgent_GH class.
        /// </summary>
        public ListItemAccess()
          : base("ListItemAccess", "item",
             "Description",
              "RemoSharp", "Utils")
        {
            DataTest = "";
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

            pManager.AddGenericParameter("List", "list", "list", GH_ParamAccess.list);
            pManager.AddIntegerParameter("index", "i", " ", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("0", "0", "", GH_ParamAccess.item);


        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> objs = new List<object>();
            int index = -1;
            DA.GetDataList(0, objs);
            DA.GetData(1, ref index);

            // Here you loop throw all your inputs and collect data.
            for (int i = 0; i < this.Params.Output.Count; i++)
            {

                int paramIndex = Convert.ToInt32(this.Params.Output[i].NickName);

                if (paramIndex < 0) paramIndex = paramIndex + objs.Count;

                if (paramIndex > objs.Count - 1) DA.SetData(i, null);
                else
                {
                    DA.SetData(i, objs[paramIndex]);
                }

                

            }




        }



        #region VARIABLE COMPONENT INTERFACE IMPLEMENTATION
        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {

            // Only insert parameters on input side. This can be changed if you like/need
            // side== GH_ParameterSide.Output
            if (side == GH_ParameterSide.Output)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            // Only allowed to remove parameters if there are more than 2
            // from the input side
            if (side == GH_ParameterSide.Output && Params.Output.Count > 2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {

            // Has to return a parameter object!
            Param_GenericObject param = new Param_GenericObject();



            string nickName = "";

            if (index == 0) 
            {
                int firstIndex = Convert.ToInt32(this.Params.Output[0].NickName);
                nickName = (firstIndex - 1).ToString();
            }
            else
            {
                int firstIndex = Convert.ToInt32(this.Params.Output[index - 1].NickName);
                nickName = (firstIndex + 1).ToString();
            }

            param.Name = nickName;
            param.NickName = nickName;
            param.Description = "A Data input";
            param.Optional = true;

            try
            {
                var client = this.OnPingDocument().Objects.Where(obj => obj.GetType().ToString().Equals("RemoSharp.RemoSetupClient")).ToList();
                if (client.Count == 1)
                {
                    RemoSharp.RemoSetupClient clientComp = (RemoSharp.RemoSetupClient)client[0];
                    RemoNullCommand nullCommand = new RemoNullCommand("an Output Registered");
                    string commandJson = RemoCommand.SerializeToJson(nullCommand);
                    clientComp.client.Send(commandJson);
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Error:\n" + e.Data, "Output Register Sync Failed!");
            }
            

            return param;
        }


        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            //This function will be called when a parameter is about to be removed. 
            //You do not need to do anything, but this would be a good time to remove 
            //any event handlers that might be attached to the parameter in question.

            try
            {
                var client = this.OnPingDocument().Objects.Where(obj => obj.GetType().ToString().Equals("RemoSharp.RemoSetupClient")).ToList();
                if (client.Count == 1)
                {
                    RemoSharp.RemoSetupClient clientComp = (RemoSharp.RemoSetupClient)client[0];
                    RemoNullCommand nullCommand = new RemoNullCommand("an Output Removed");
                    string commandJson = RemoCommand.SerializeToJson(nullCommand);
                    clientComp.client.Send(commandJson);
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Error:\n" + e.Data, "Output Register Sync Failed!");
            }

            return true;
        }

        public void VariableParameterMaintenance()
        {
            //This method will be called when a closely related set of variable parameter operations completes. 
            //This would be a good time to ensure all Nicknames and parameter properties are correct. This method will also be 
            //called upon IO operations such as Open, Paste, Undo and Redo.


            //throw new NotImplementedException();


        }


        #endregion


        #region COMPONENT GUI

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);

            var sub = Menu_AppendItem(menu, "Menu name");



            Menu_AppendItem(sub.DropDown, "Item in menu name", _ItemClicked, true, m_dataTest == "");


        }




        //This is the method that handles the event

        private void _ItemClicked(object sender, EventArgs e)
        {
            // Do your stuff here 
            DataTest = "You changed me";
            Params.OnParametersChanged();
            ExpireSolution(true);
        }



        #endregion


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5396a78d-7852-4335-bd3b-bf119ace0121"); }
        }
    }
}
