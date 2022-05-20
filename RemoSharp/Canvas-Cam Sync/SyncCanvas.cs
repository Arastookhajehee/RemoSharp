using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Threading;
using System.Linq;
using System.IO;
using GHCustomControls;
using WPFNumericUpDown;

namespace RemoSharp
{
    public class SyncCanvas : GHCustomComponent
    {
        PushButton pushButton1;
        /// <summary>
        /// Initializes a new instance of the SyncCanvas class.
        /// </summary>
        public SyncCanvas()
          : base("SyncCanvas", "SyncCvs",
              "WARNING!!, Syncs the components on a canvas based on an XML stream." + Environment.NewLine +
              "WARNING!!, Use only for the visualization client Canvas." + Environment.NewLine +
              ".......!! It will delete anything that is not in the stream!" + Environment.NewLine +
              ".......!! The action is not undoable!",
              "RemoSharp", "BroadcastTools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pushButton1 = new PushButton("WS_Client",
            "Creates The Required WS Client Components To Broadcast Canvas Screen.", "WS_Client");
            pushButton1.OnValueChanged += PushButton1_OnValueChanged;
            AddCustomControl(pushButton1);

            pManager.AddTextParameter("XML_Stream", "XMLstrm", "XML text representation of the server's canvas Grasshopper document.",
                GH_ParamAccess.item, "");
        }

        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                System.Drawing.PointF pivot = this.Attributes.Pivot;
                System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 700, pivot.Y - 23);
                System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 504, pivot.Y + 13);
                System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X - 293, pivot.Y + 4);
                System.Drawing.PointF wsRecvPivot = new System.Drawing.PointF(pivot.X - 135, pivot.Y + 14);

                Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.Attributes.Pivot = panelPivot;
                panel.SetUserText("");
                panel.Name = "RemoSharp";
                panel.NickName = "RemoSharp";
                panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 300, 20);

                Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                button.CreateAttributes();
                button.Attributes.Pivot = buttnPivot;
                button.Name = "RemoSharp";
                button.NickName = "RemoSharp";

                RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                wss.CreateAttributes();
                wss.Attributes.Pivot = wssPivot;

                RemoSharp.WsClientCat.WsClientRecv wsRecv = new WsClientCat.WsClientRecv();
                wsRecv.CreateAttributes();
                wsRecv.Attributes.Pivot = wsRecvPivot;

                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(button, true);
                    this.OnPingDocument().AddObject(wss, true);
                    this.OnPingDocument().AddObject(wsRecv, true);

                    wss.Params.Input[2].AddSource((IGH_Param)button);
                    wss.Params.Input[0].AddSource((IGH_Param)panel);
                    wsRecv.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                    this.Params.Input[0].AddSource((IGH_Param)wsRecv.Params.Output[0]);
                });

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
            //https://stackoverflow.com/questions/674479/how-do-i-get-the-directory-from-a-files-full-path
            string path = @"C:\temp\RemoSharp\ReceiveStream.ghx";
            CheckForDirectoryAndFileExistance(path);

            string stream = "";
            DA.GetData(0,ref stream);

            if (stream == "" ||
                stream == null ||
                stream == " " ||
                stream == "Hello World") return;

            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.Write(stream);
            }

            Grasshopper.Kernel.GH_DocumentIO ioDoc = new GH_DocumentIO();
            ioDoc.Open(path);
            var newDoc = ioDoc.Document;
            this.OnPingDocument().MergeDocument(newDoc);

            this.OnPingDocument().ScheduleSolution(150, DeleteAll);
        }

        private void CheckForDirectoryAndFileExistance(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (!File.Exists(path)) File.Create(path);
        }

        public void DeleteAll(GH_Document doc)
        {
            foreach (var obj in this.OnPingDocument().Objects.Reverse())
            {
                string type = obj.GetType().ToString();

                if (!type.Contains("RemoSharp"))
                {
                    if (!obj.NickName.Contains("RemoSharp"))
                    {
                        if (!obj.Name.Contains("RemoSharp"))
                        {
                            this.OnPingDocument().RemoveObject(obj, false);
                        }
                    }
                }
                
            }
            this.ExpireSolution(false);
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("4DEEC454-DC09-4ADE-B8D4-B28CEAC057F1"); }
        }
    }
}