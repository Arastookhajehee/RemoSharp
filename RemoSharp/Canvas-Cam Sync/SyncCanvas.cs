using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Threading;
using System.Linq;
using System.IO;
using GHCustomControls;
using WPFNumericUpDown;
using System.Text;

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
            pushButton1 = new PushButton("Set Up Visualizer",
            "Creates The Required WS Client Components To Broadcast Canvas Screen.", "Set Up Visualizer");
            pushButton1.OnValueChanged += PushButton1_OnValueChanged;
            AddCustomControl(pushButton1);

            pManager.AddTextParameter("XML_GH_Stream", "GH_DocXML", "XML text representation of the server's canvas Grasshopper document.",
                GH_ParamAccess.item, "");
        }

        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                //StreamIPSet canvasAddress = new StreamIPSet();
                //canvasAddress.DialougeTitle.Text = "Please Set Your Canvas Content Sync Server Address";
                //canvasAddress.ShowDialog();
                string address = "";

                int shiftX = -20;
                System.Drawing.PointF pivot = this.Attributes.Pivot;
                //System.Drawing.PointF panelPivot = new System.Drawing.PointF(shiftX + pivot.X - 673, pivot.Y - 39);
                System.Drawing.PointF buttnPivot = new System.Drawing.PointF(shiftX + pivot.X - 504, pivot.Y + 9);
                System.Drawing.PointF wssPivot = new System.Drawing.PointF(shiftX + pivot.X - 293, pivot.Y + 0);
                System.Drawing.PointF wsRecvPivot = new System.Drawing.PointF(shiftX + pivot.X - 148, pivot.Y + 10);

                //Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                //panel.CreateAttributes();
                //panel.Attributes.Pivot = panelPivot;
                //panel.SetUserText(address);
                //panel.Name = "RemoSharp Canvas Server";
                //panel.NickName = "RemoSharp Canvas Server";
                //panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 300, 20);

                Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                button.CreateAttributes();
                button.Attributes.Pivot = buttnPivot;
                button.Name = "RemoSharp";
                button.NickName = "RemoSharp";

                RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                wss.CreateAttributes();
                wss.Attributes.Pivot = wssPivot;
                wss.Params.RepairParamAssociations();

                RemoSharp.WsClientCat.WsClientRecv wsRecv = new WsClientCat.WsClientRecv();
                wsRecv.CreateAttributes();
                wsRecv.Attributes.Pivot = wsRecvPivot;
                wsRecv.Params.RepairParamAssociations();

                var addressOutPuts = RemoSharp.RemoCommandTypes.Utilites.CreateServerMakerComponent(this.OnPingDocument(), pivot, -474, -69, true);


                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    //this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(button, true);
                    this.OnPingDocument().AddObject(wss, true);
                    this.OnPingDocument().AddObject(wsRecv, true);

                    wss.Params.Input[2].AddSource((IGH_Param)button);
                    wss.Params.Input[0].AddSource((IGH_Param)addressOutPuts[0]);
                    wsRecv.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                    //this.Params.Input[0].AddSource((IGH_Param)wsRecv.Params.Output[0]);
                });

                AddSyncViewPortCoordinates(pivot, 80);
                currentValue = false;
            }
        }
        private void AddSyncViewPortCoordinates(System.Drawing.PointF pivot, int shiftDown)
        {
            
            string address = "ws://127.0.0.1:18580/RemoSharpCanvasBounds";

            int shiftX = -20;
            System.Drawing.PointF syncViewPivot = new System.Drawing.PointF(shiftX + pivot.X - 7, pivot.Y + shiftDown);
            System.Drawing.PointF panelPivot = new System.Drawing.PointF(shiftX + pivot.X - 673, pivot.Y - 39 + shiftDown);
            System.Drawing.PointF buttnPivot = new System.Drawing.PointF(shiftX + pivot.X - 504, pivot.Y + 9 + shiftDown);
            System.Drawing.PointF wssPivot = new System.Drawing.PointF(shiftX + pivot.X - 293, pivot.Y + 0 + shiftDown);
            System.Drawing.PointF wsRecvPivot = new System.Drawing.PointF(shiftX + pivot.X - 148, pivot.Y + 10 + shiftDown);

            RemoSharp.Canvas_Cam_Sync.SyncGHviewport syncView = new RemoSharp.Canvas_Cam_Sync.SyncGHviewport();
            syncView.CreateAttributes();
            syncView.Attributes.Pivot = syncViewPivot;

            Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
            panel.CreateAttributes();
            panel.Attributes.Pivot = panelPivot;
            panel.SetUserText(address);
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
                this.OnPingDocument().AddObject(syncView, true);
                this.OnPingDocument().AddObject(panel, true);
                this.OnPingDocument().AddObject(button, true);
                this.OnPingDocument().AddObject(wss, true);
                this.OnPingDocument().AddObject(wsRecv, true);

                wss.Params.Input[2].AddSource((IGH_Param)button);
                if (!address.Equals("")) wss.Params.Input[0].AddSource((IGH_Param)panel);
                wsRecv.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                syncView.Params.Input[0].AddSource((IGH_Param)wsRecv.Params.Output[0]);
            });
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

            if (string.IsNullOrEmpty(stream) ||
                stream == " " ||
                stream == "Hello World" ||
                !stream.Substring(0,15).Contains("?xml version")) return;

            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.Write(stream);
            }


            try
            {
                Grasshopper.Kernel.GH_DocumentIO ioDoc = new GH_DocumentIO();
                ioDoc.Open(path);
                var newDoc = ioDoc.Document;

                this.OnPingDocument().MergeDocument(newDoc);

                this.OnPingDocument().ScheduleSolution(150, DeleteAll);
            }
            catch
            {

            }

            //foreach (var obj in newDoc.Objects.Reverse())
            //{
            //    string type = obj.GetType().ToString();
            //    var objPivot = obj.Attributes.Pivot;
            //    bool isFromRemoSharp = type.Contains("RemoSharp");
            //    bool remosharpNickname = obj.NickName.Contains("RemoSharp");
                
            //    if (isFromRemoSharp || remosharpNickname)
            //    {
            //        newDoc.RemoveObject(obj, false);
            //    }
            //}

        }

        private void CheckForDirectoryAndFileExistance(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (!File.Exists(path))
            {
                using (var file = File.Create(path))
                {
                    byte[] byteArray = Encoding.ASCII.GetBytes("First Line");
                    file.Write(byteArray, 0, 0);
                    file.Close();
                }
            }

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
                return RemoSharp.Properties.Resources.SyncCanvas.ToBitmap();
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