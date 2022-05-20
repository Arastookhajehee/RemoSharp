using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using GHCustomControls;
using WPFNumericUpDown;
using Newtonsoft.Json;

namespace RemoSharp
{
    public class BroadcastCanvasScreen : GHCustomComponent
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;
        PushButton pushButton1;

        /// <summary>
        /// Initializes a new instance of the BroadcastCanvasScreen class.
        /// </summary>
        public BroadcastCanvasScreen()
          : base("BroadcastCanvasScreen", "BrdCstCanvas",
              "Broadcasts the extents of the grasshopper canvas as a screenshot.",
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
            pManager.AddBooleanParameter("SaveToFile", "Save",
                "Saves the canvas screenshot to a local file on the defined path.",
                GH_ParamAccess.item, false);
            pManager.AddTextParameter("Path", "Path", "Local path for the screenshot to be saved in.",
                GH_ParamAccess.item,
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            pManager.AddIntegerParameter("OriginShiftX", "ShiftX", "Shifts the origin point of the image for calibration purposes.",
                GH_ParamAccess.item,
                100);
            pManager.AddIntegerParameter("OriginShiftY", "ShiftY", "Shifts the origin point of the image for calibration purposes.",
                GH_ParamAccess.item,
                100);
            pManager.AddBooleanParameter("LowResolution", "LowRes", "If True, it will generates a low res and light image. " +
                "Note that Low-Res means that the screen image will take less time to generate but will be less legible.",
                GH_ParamAccess.item,
                false);
            pManager.AddBooleanParameter("HighResolution", "HighRes", "If True, it will generates a 1:1 scale image of the canvas. " +
                "Note that High-Res means that the screen image will take longer to generate and may result in performance and latency issues.",
                GH_ParamAccess.item,
                false);
        }

        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                System.Drawing.PointF pivot = this.Attributes.Pivot;
                System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 215, pivot.Y - 126-10);
                System.Drawing.PointF togglePivot = new System.Drawing.PointF(pivot.X - 251, pivot.Y -60 - 10);
                System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 20, pivot.Y - 90 - 10);
                System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X + 189, pivot.Y - 99 - 10);
                System.Drawing.PointF wsSendPivot = new System.Drawing.PointF(pivot.X + 353, pivot.Y - 4 - 10);
                System.Drawing.PointF triggerPivot = new System.Drawing.PointF(pivot.X - 251, pivot.Y + 80);

                Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.Attributes.Pivot = panelPivot;
                panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 300, 20);
                panel.SetUserText("");

                Grasshopper.Kernel.Special.GH_BooleanToggle toggle = new Grasshopper.Kernel.Special.GH_BooleanToggle();
                toggle.CreateAttributes();
                toggle.Attributes.Pivot = togglePivot;

                Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                button.CreateAttributes();
                button.Attributes.Pivot = buttnPivot;

                RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                wss.CreateAttributes();
                wss.Attributes.Pivot = wssPivot;

                RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
                wsSend.CreateAttributes();
                wsSend.Attributes.Pivot = wsSendPivot;

                var guid = this.InstanceGuid;
                Grasshopper.Kernel.Special.GH_Timer trigger = new Grasshopper.Kernel.Special.GH_Timer();
                trigger.CreateAttributes();
                trigger.Attributes.Pivot = triggerPivot;
                trigger.Interval = 100;

                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(toggle, true);
                    this.OnPingDocument().AddObject(button, true);
                    this.OnPingDocument().AddObject(wss, true);
                    this.OnPingDocument().AddObject(wsSend, true);
                    this.OnPingDocument().AddObject(trigger, true);

                    trigger.AddTarget(guid);
                    wss.Params.Input[0].AddSource((IGH_Param)panel);
                    wss.Params.Input[2].AddSource((IGH_Param)button);
                    wsSend.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                    wsSend.Params.Input[1].AddSource((IGH_Param)this.Params.Output[0]);
                    this.Params.Input[0].AddSource((IGH_Param)toggle);
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
            this.Message = "Need a Trigger for RT";

            Component = this;
            GrasshopperDocument = this.OnPingDocument();

            bool broadcast = false;
            bool saveToFile = false;
            string path = "";
            int shiftX = 0;
            int shiftY = 0;
            bool lowRes = false;
            bool highRes = false;

            DA.GetData(0, ref broadcast);
            DA.GetData(1, ref saveToFile);
            DA.GetData(2, ref path);
            DA.GetData(3, ref shiftX);
            DA.GetData(4, ref shiftY);
            DA.GetData(5, ref lowRes);
            DA.GetData(6, ref highRes);


            if (!broadcast) return;

            // getting the extent of the GH canvas by taking a look at where the components are located
            var extents = FindCanvasExtents();
            var rec = new System.Drawing.Rectangle(shiftX, shiftY, extents[1], extents[3]);

            // getting the active GH canvas
            var thisCanvas = Grasshopper.Instances.ActiveCanvas;

            // settings for the GenerateHiResImage() method
            var ghSet = new Grasshopper.GUI.Canvas.GH_Canvas.GH_ImageSettings(
              Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
              );
            ghSet.Folder = "Imgages";
            ghSet.Extension = "png";
            ghSet.FileName = "testFile";
            ghSet.BackColour = Color.FromArgb(200, 200, 200);
            if (lowRes)
            {
                ghSet.Zoom = 0.15f;
            }
            else if (!highRes)
            {
                ghSet.Zoom = 0.5f;
            }
            else
            {
                ghSet.Zoom = 1;
            }

            // generating temporary files for the screenshot
            Size size;
            // GenerateHiResImage() returns the path of the temporary png images in string format
            var imagePaths = thisCanvas.GenerateHiResImage(rec, ghSet, out size);
            tempFileDirs.AddRange(imagePaths);


            string[] imagesStrings = new string[imagePaths.Count];
            System.Threading.Tasks.Parallel.For(0, imagePaths.Count, i =>
            {
                //    for (int i = 0; i < imagePaths.Count; i++) {

                string imagePath = imagePaths[i];
                string imagePartBase64 = ImageToBase64Converter(imagePath);
                ImagePartBase64 imagePart = new ImagePartBase64(imagePath, imagePartBase64);
                string singleJsonString = JsonConvert.SerializeObject(imagePart);
                imagesStrings[i] = singleJsonString;
                //    }
            });

            Thread tr = new Thread(DeleteTemporaryFiles);
            tr.Start();

            string entireImageJson = JsonConvert.SerializeObject(imagesStrings);

            DA.SetData(0, entireImageJson);
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
                return RemoSharp.Properties.Resources.BroadcastCanvas.ToBitmap();
            }
        }
        
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("e6aea055-343c-4959-b896-27fbece0a246"); }
        }

        string ImageToBase64Converter(string filePath)
        {
            // https://stackoverflow.com/questions/21325661/convert-an-image-selected-by-path-to-base64-string
            byte[] imageArray = System.IO.File.ReadAllBytes(filePath);
            string base64ImageRepresentation = Convert.ToBase64String(imageArray);
            return base64ImageRepresentation;
        }

        int[] FindCanvasExtents()
        {
            int[] extents = new int[4];

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            var thisGH_Objs = this.GrasshopperDocument.Objects;
            for (int i = 0; i < thisGH_Objs.Count; i++)
            {
                var tempObj = thisGH_Objs[i];
                int xDim = Convert.ToInt32(tempObj.Attributes.Pivot.X);
                int yDim = Convert.ToInt32(tempObj.Attributes.Pivot.Y);

                if (xDim < minX) minX = xDim;
                if (yDim < minY) minY = yDim;
                if (xDim > maxX) maxX = xDim;
                if (yDim > maxY) maxY = yDim;
            }

            extents[0] = minX;
            extents[1] = maxX;
            extents[2] = minY;
            extents[3] = maxY;
            return extents;
        }

        public List<string> tempFileDirs = new List<string>();
        private void DeleteTemporaryFiles()
        {
            string root = tempFileDirs[tempFileDirs.Count - 1];
            var directory = Path.GetDirectoryName(root);
            // If directory does not exist, don't even try
            while (Directory.Exists(directory))
            {
                if (Directory.Exists(directory))
                {
                    try
                    {
                        Directory.Delete(directory, true);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}