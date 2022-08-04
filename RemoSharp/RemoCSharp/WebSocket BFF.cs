using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using GHCustomControls;
using WPFNumericUpDown;

namespace RemoSharp
{
    public class WebSocket_BFF : GHCustomComponent
    {
        bool alreadyConnected = false;
        System.Guid triggerGuid = new System.Guid();
        int counter = 0;
        PushButton pushButton1;

        /// <summary>
        /// Initializes a new instance of the WebSocket_BFF class.
        /// </summary>
        public WebSocket_BFF()
          : base("WebSocket_BFF", "WS_BFF",
              "Tries to keep a connection live with a WebSocket Server (for example glitch.com servers)",
              "RemoSharp", "Com_Tools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pushButton1 = new PushButton("Set Up",
                    "Creates The Required WS Client Components To Broadcast Canvas Screen.", "Set Up");
            pushButton1.OnValueChanged += PushButton1_OnValueChanged;
            AddCustomControl(pushButton1);

            pManager.AddBooleanParameter("Reset_Button", "Rst_Button", "The same button that is connected to the 'Reset' input of the WebSocket Client Start Component.", GH_ParamAccess.item,false);
            pManager.AddBooleanParameter("Keep_Alive", "keepAlive", "True: Keep Reconnecting, False: No Automatic Reconnection", GH_ParamAccess.item, false);
        }

        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                System.Drawing.PointF pivot = this.Attributes.Pivot;
                int shiftX = 0;
                int shiftY = -72;
                System.Drawing.PointF togglePivot = new System.Drawing.PointF(shiftX + pivot.X - 243, shiftY + pivot.Y + 33 + 38);

                // Creating a toggle
                Grasshopper.Kernel.Special.GH_BooleanToggle toggle = new Grasshopper.Kernel.Special.GH_BooleanToggle();
                toggle.CreateAttributes();
                toggle.Attributes.Pivot = togglePivot;
                toggle.NickName = "RemoSharp";

                Grasshopper.Kernel.Special.GH_Timer gH_Timer = null;

                bool WSBFF_Trigger_Found = false;
                var ghObjs = this.OnPingDocument().Objects;
                foreach (var obj in ghObjs)
                {
                    // if the NickName of the component was "RemoSharp WSBFF" then its the one we want
                    string nickName = "RemoSharp WSBFF";
                    if (obj.NickName.Equals(nickName)) 
                    {
                        gH_Timer = (Grasshopper.Kernel.Special.GH_Timer)obj;
                        WSBFF_Trigger_Found = true;
                        break;
                    }
                }

                if (!WSBFF_Trigger_Found)
                {
                    // creating the trigger
                    System.Drawing.PointF triggerPivot = new System.Drawing.PointF(shiftX + pivot.X - 243, shiftY + pivot.Y + 66 + 78);
                    gH_Timer = new Grasshopper.Kernel.Special.GH_Timer();
                    gH_Timer.CreateAttributes();
                    gH_Timer.Attributes.Pivot = triggerPivot;
                    gH_Timer.Interval = 1000;
                    gH_Timer.NickName = "RemoSharp WSBFF";
                }

                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    this.OnPingDocument().AddObject(toggle, true);
                    this.OnPingDocument().AddObject(gH_Timer, true);

                    this.Params.Input[1].AddSource((IGH_Param)toggle);
                    gH_Timer.AddTarget(this.InstanceGuid);
                });

                triggerGuid = gH_Timer.InstanceGuid;
                alreadyConnected = true;
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // instruction
            this.Message = "Trigger Required";

            bool keepAlive = false;
            DA.GetData(1,ref keepAlive);
            if (!keepAlive) return;

            // getting all the components in the canvas to find the "RemoSharp WSBFF" trigger component
            var ghObjs = this.OnPingDocument().Objects;
            foreach (var obj in ghObjs)
            {
                if (alreadyConnected) break;
                // if the NickName of the component was "RemoSharp WSBFF" then its the one we want
                string nickName = "RemoSharp WSBFF";
                if (obj.NickName.Equals(nickName))
                {
                    // checking if the trigger is already connected or not
                    Grasshopper.Kernel.Special.GH_Timer bffTRG = (Grasshopper.Kernel.Special.GH_Timer)obj;

                    foreach (System.Guid guid in bffTRG.Targets)
                    {
                        // going through the guids in the trigger target list to see if this comp's instance guid is in it
                        if (guid.ToString().Equals(this.InstanceGuid.ToString()))
                        {
                            alreadyConnected = true;
                            triggerGuid = bffTRG.InstanceGuid;
                            break;
                        }

                    }
                    // connect if not yet connected
                    if (!alreadyConnected)
                    {
                        bffTRG.AddTarget(this.InstanceGuid);
                        alreadyConnected = true;
                        triggerGuid = bffTRG.InstanceGuid;
                    }
                }
            }

            //getting the interval of the trigger
            int trgInterval = 999;
            int resetTimeout = 15000;
            if (alreadyConnected)
            {
                Grasshopper.Kernel.Special.GH_Timer trg = (Grasshopper.Kernel.Special.GH_Timer)this.OnPingDocument().FindObject(triggerGuid, false);
                trgInterval = trg.Interval;
            }
            bool resetTimeOut = trgInterval * counter > resetTimeout;

            Grasshopper.Kernel.Special.GH_ButtonObject button = null;

            try
            {
                // finding the button that is connected to this component
                button = (Grasshopper.Kernel.Special.GH_ButtonObject)this.Params.Input[0].Sources[0];

            }
            catch
            {

                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error,"Please Connect a Button the \"Reset Button\" input");
            }

            // finding the wsc component from the reciepients of the button component
            GH_Component wsc = null;
            for (int i = 0; i < button.Recipients.Count; i++)
            {
                var wscGuid = button.Recipients[i].Attributes.Parent.InstanceGuid;
                GH_Component recipient = (GH_Component)this.OnPingDocument().FindObject(wscGuid, false);
                if (recipient.GetType().ToString().Equals("RemoSharp.WsClientCat.WsClientStart")) wsc = recipient;
            }

            try
            {
                // pushing the reset button if the connection is lost
                if (wsc.Message.Equals("Close") && keepAlive && resetTimeOut)
                {
                    this.OnPingDocument().ScheduleSolution(0, PushButton);
                    // the counter prevents multiple reset buttons being pushed
                    // preventing GH's freezing and potential crash
                    counter = 0;
                }
            }
            catch
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Make sure the \"Reset Button\" connected this this component is also connected" + Environment.NewLine +
                                                                     " to a \"WebSocket Client Start\" component too!" + Environment.NewLine +
                                                                     "Also, make sure the \"WebSocket Client Start\" component's URL input is not blank");
            }

            counter++;
        }



        // a function to push the reset button
        void PushButton(GH_Document doc)
        {
            Grasshopper.Kernel.Special.GH_ButtonObject button = (Grasshopper.Kernel.Special.GH_ButtonObject)this.Params.Input[0].Sources[0];
            button.ButtonDown = true;
            button.ExpireSolution(true);
            button.ButtonDown = false;
            button.ExpireSolution(true);

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
                return RemoSharp.Properties.Resources.WS_BFF.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("070d3911-ed8a-44bf-9f3c-64efe68f7ff0"); }
        }
    }
}