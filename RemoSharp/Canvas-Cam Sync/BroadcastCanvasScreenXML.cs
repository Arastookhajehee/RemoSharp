using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;
using System.IO;
using System.Threading;
using GHCustomControls;
using WPFNumericUpDown;
using Newtonsoft.Json;
using System.Linq;
using WebMarkupMin.Core;
using AdvancedStringBuilder;

namespace RemoSharp
{
    public class BroadcastCanvasScreenXML : GHCustomComponent
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;
        PushButton pushButton1;

        /// <summary>
        /// Initializes a new instance of the BroadcastCanvasScreenXML class.
        /// </summary>
        public BroadcastCanvasScreenXML()
          : base("BroadcastCanvasScreenXML", "BrdCstCanvas",
              "Broadcasts the extents of the grasshopper canvas as a Grasshopper document as an XML text representation.",
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

            pManager.AddBooleanParameter("Broadcast", "BrdCst",
                "Creates and broadcasts the current GH_Canvas screenshot in a text format.",
                GH_ParamAccess.item, false);
            pManager.AddTextParameter("Client Coordinates", "Cl_Coords",
                "The coordinates of the a client's Grasshopper canvas. (The visiable and active reagion the Grasshopper canvs).",
                GH_ParamAccess.item, "");
            
            
            
        }
        private void CheckForDirectoryAndFileExistance(string path)
        {
            bool directoryExists = Directory.Exists(Path.GetDirectoryName(path));
            bool fileExists = File.Exists(path);

            if (!directoryExists) Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (!fileExists) File.Create(path);
        }

        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                System.Drawing.PointF pivot = this.Attributes.Pivot;
                System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 350, pivot.Y -109);
                System.Drawing.PointF panelPivot2 = new System.Drawing.PointF(pivot.X - 670, pivot.Y -29);
                System.Drawing.PointF togglePivot = new System.Drawing.PointF(pivot.X - 238 , pivot.Y + 60);
                System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 180, pivot.Y - 61);
                System.Drawing.PointF buttnPivot2 = new System.Drawing.PointF(pivot.X - 500, pivot.Y + 19);
                System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X + 30, pivot.Y - 70);
                System.Drawing.PointF wssPivot2 = new System.Drawing.PointF(pivot.X - 290, pivot.Y +10);
                System.Drawing.PointF wsSendPivot = new System.Drawing.PointF(pivot.X + 180, pivot.Y - 10);
                System.Drawing.PointF wsRecvPivot = new System.Drawing.PointF(pivot.X - 160, pivot.Y + 20);

                // run/notrun toggle
                Grasshopper.Kernel.Special.GH_BooleanToggle toggle = new Grasshopper.Kernel.Special.GH_BooleanToggle();
                toggle.CreateAttributes();
                toggle.Attributes.Pivot = togglePivot;
                toggle.NickName = "RemoSharp";

                // sending wss address panel
                Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.Attributes.Pivot = panelPivot;
                panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 300, 20);
                panel.SetUserText("");
                panel.NickName = "RemoSharp";

                // receiving wss address panel
                Grasshopper.Kernel.Special.GH_Panel panel2 = new Grasshopper.Kernel.Special.GH_Panel();
                panel2.CreateAttributes();
                panel2.Attributes.Pivot = panelPivot2;
                panel2.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 300, 20);
                panel2.SetUserText("");
                panel2.NickName = "RemoSharp";

                // sending wss button
                Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                button.CreateAttributes();
                button.Attributes.Pivot = buttnPivot;
                button.NickName = "RemoSharp";

                // receiving wss button
                Grasshopper.Kernel.Special.GH_ButtonObject button2 = new Grasshopper.Kernel.Special.GH_ButtonObject();
                button2.CreateAttributes();
                button2.Attributes.Pivot = buttnPivot2;
                button2.NickName = "RemoSharp";

                // sending wss
                RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                wss.CreateAttributes();
                wss.Attributes.Pivot = wssPivot;

                // receiving wss
                RemoSharp.WsClientCat.WsClientStart wss2 = new WsClientCat.WsClientStart();
                wss2.CreateAttributes();
                wss2.Attributes.Pivot = wssPivot2;

                // send component
                RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
                wsSend.CreateAttributes();
                wsSend.Attributes.Pivot = wsSendPivot;

                // send component
                RemoSharp.WsClientCat.WsClientRecv wsRecv = new WsClientCat.WsClientRecv();
                wsRecv.CreateAttributes();
                wsRecv.Attributes.Pivot = wsRecvPivot;



                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(toggle, true);
                    this.OnPingDocument().AddObject(button, true);
                    this.OnPingDocument().AddObject(wss, true);
                    this.OnPingDocument().AddObject(wsSend, true);

                    this.OnPingDocument().AddObject(wss2, true);
                    this.OnPingDocument().AddObject(wsRecv, true);
                    this.OnPingDocument().AddObject(panel2, true);
                    this.OnPingDocument().AddObject(button2, true);
                    


                    wss.Params.Input[0].AddSource((IGH_Param)panel);
                    wss2.Params.Input[0].AddSource((IGH_Param)panel2);
                    wss.Params.Input[2].AddSource((IGH_Param)button);
                    wss2.Params.Input[2].AddSource((IGH_Param)button2);
                    wsSend.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                    wsSend.Params.Input[1].AddSource((IGH_Param)this.Params.Output[0]);

                    wsRecv.Params.Input[0].AddSource((IGH_Param)wss2.Params.Output[0]);
                    this.Params.Input[0].AddSource((IGH_Param)toggle);
                    this.Params.Input[1].AddSource((IGH_Param)wsRecv.Params.Output[0]);
                });

            }
        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("CanvasScreenshot", "screenshot", "A text based representation of the current GH_Canvas screenshot",
                GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string savePath = @"C:\temp\RemoSharp\saveTempFile.ghx";
            string openPath = @"C:\temp\RemoSharp\openTempFile.ghx";
            string finalPath = @"C:\temp\RemoSharp\finalTempFile.ghx";

            CheckForDirectoryAndFileExistance(savePath);
            CheckForDirectoryAndFileExistance(openPath);
            CheckForDirectoryAndFileExistance(finalPath);

            bool run = false;
            string coords = "";
            DA.GetData(0, ref run);
            DA.GetData(1, ref coords);

            if (!run || coords == "" || coords == null || coords == "Hello World") return;
            

            Grasshopper.Kernel.GH_DocumentIO saveDoc = new GH_DocumentIO(this.OnPingDocument());
            bool saveDocR = saveDoc.SaveQuiet(savePath);
            System.IO.File.Copy(savePath, openPath, true);
            Grasshopper.Kernel.GH_DocumentIO openDoc = new GH_DocumentIO();
            openDoc.Open(openPath);
            var bounds = new VisibleBounds(coords);
            foreach (var obj in openDoc.Document.Objects.Reverse())
            {
                string type = obj.GetType().ToString();
                var objPivot = obj.Attributes.Pivot;
                bool outside = false;
                bool isFromRemoSharp = type.Contains("RemoSharp");
                bool remosharpNickname = obj.NickName.Contains("RemoSharp");
                if (
                  (int)objPivot.X < bounds.topLeftCornerX ||
                  (int)objPivot.Y < bounds.topLeftCornerY ||
                  (int)objPivot.X > bounds.topLeftCornerX + bounds.visibleAreaWidth ||
                  (int)objPivot.Y > bounds.topLeftCornerY + bounds.visibleAreaHeight) outside = true;
                if (isFromRemoSharp || outside || remosharpNickname)
                {
                    openDoc.Document.RemoveObject(obj, false);
                }
            }
            openDoc.SaveQuiet(savePath);

            string content = "";
            using (StreamReader sr = new StreamReader(savePath))
            {
                content = sr.ReadToEnd();
                WebMarkupMin.Core.XmlMinifier minifier = new XmlMinifier();
                MarkupMinificationResult result = minifier.Minify(content, false);
                content = result.MinifiedContent;

                using (StreamWriter sw = new StreamWriter(finalPath))
                {
                    sw.Write(content);
                }
            }
            if (content.Length < 150000)
            {
                DA.SetData(0, content);
            }
            else
            {
                this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Broadcasting region too large");
            }
        }

        private class VisibleBounds
        {
            public int topLeftCornerX;
            public int topLeftCornerY;
            public int visibleAreaWidth;
            public int visibleAreaHeight;

            public VisibleBounds(string coordinatesCSV)
            {
                string[] csv = coordinatesCSV.Split(',');
                double topLeftCornerX = Convert.ToDouble(csv[0]);
                double topLeftCornerY = Convert.ToDouble(csv[1]);
                double visibleAreaWidth = Convert.ToDouble(csv[2]);
                double visibleAreaHeight = Convert.ToDouble(csv[3]);

                this.topLeftCornerX = (int)topLeftCornerX;
                this.topLeftCornerY = (int)topLeftCornerY;
                this.visibleAreaWidth = (int)visibleAreaWidth;
                this.visibleAreaHeight = (int)visibleAreaHeight;
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
                return RemoSharp.Properties.Resources.BroadcastCanvas.ToBitmap(); ;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("EF3EC033-ACFF-44AA-8749-4655BC50E498"); }
        }
    }
}