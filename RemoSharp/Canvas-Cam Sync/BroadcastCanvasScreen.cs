using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using GHCustomControls;
using WPFNumericUpDown;

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
                System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 215, pivot.Y - 126);
                System.Drawing.PointF togglePivot = new System.Drawing.PointF(pivot.X - 251, pivot.Y -53);
                System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 20, pivot.Y - 90);
                System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X + 189, pivot.Y - 99);
                System.Drawing.PointF wsSendPivot = new System.Drawing.PointF(pivot.X + 353, pivot.Y - 2);

                Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.Attributes.Pivot = panelPivot;
                panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 300, 20);

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

                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(toggle, true);
                    this.OnPingDocument().AddObject(button, true);
                    this.OnPingDocument().AddObject(wss, true);
                    this.OnPingDocument().AddObject(wsSend, true);

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
            bool highRes = false;

            DA.GetData(0, ref broadcast);
            DA.GetData(1, ref saveToFile);
            DA.GetData(2, ref path);
            DA.GetData(3, ref shiftX);
            DA.GetData(4, ref shiftY);
            DA.GetData(5, ref highRes);


            if (!broadcast) return;

            // getting the extent of the GH canvas by taking a look at where the components are located
            var extents = FindCanvasExtents();
            var rec = new System.Drawing.Rectangle(shiftX, shiftY, extents[1], extents[3]);

            // getting the active GH canvas
            var thisCanvas = Grasshopper.Instances.ActiveCanvas;

            // settings for the GenerateHiResImage() method
            var ghSet = new Grasshopper.GUI.Canvas.GH_Canvas.GH_ImageSettings(path);
            ghSet.Folder = "Imgages";
            ghSet.Extension = "png";
            ghSet.FileName = "testFile";
            ghSet.BackColour = Color.FromArgb(80, 80, 80);
            if (!highRes)
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

            // understanding how big the final image is going to be
            int xCount = size.Width / 1000 + 1;
            int yCount = size.Height / 1000 + 1;

            // generating a string array that has the temporary images in a correct format
            // the temporary images are saved with a name like 0;0 0;1 0;2 1;0 1;1 1;2
            string tmepRoot = "";
            string[,] strings = new string[xCount, yCount];
            for (int i = 0; i < imagePaths.Count; i++)
            {

                // splitting the name of the files using ';' to add them to the string array
                string[] imagePathName = imagePaths[i].Split(';');
                if (i == 0)
                {
                    string tempFileDir = System.IO.Path.GetDirectoryName(imagePaths[i]);
                    tmepRoot = tempFileDir;
                }
                string xAddress = imagePathName[0].Substring(imagePathName[0].Length - 1);
                string yAddress = imagePathName[1].Substring(0, 1);
                int xIntAdd = Convert.ToInt32(xAddress);
                int yIntAdd = Convert.ToInt32(yAddress);
                strings[xIntAdd, yIntAdd] = imagePaths[i];
            }

            // creating a base64 string to act as the Real Time canvas image stream
            string base64String = "";
            // 1000x1000 pixels is the default temporary image size that the GenerateHiResImage() creates
            int imageDim = 1000;

            // combining the temporary images into one big image
            using (Bitmap result = new Bitmap(xCount * imageDim, yCount * imageDim))
            {

                // unfortunatley the multi-threaded for loop overrides the imagag in the same place
                // resulting in incorrect final images. So I had to revert back to single threaded usual for loops
                //      System.Threading.Tasks.Parallel.For(0, xCount, x => {
                for (int x = 0; x < xCount; x++)
                {
                    //        System.Threading.Tasks.Parallel.For(0, yCount, y => {
                    for (int y = 0; y < yCount; y++)
                    {
                        // we create a Graphics object to combine all the images together.
                        using (Graphics g = Graphics.FromImage(result))
                        {
                            Image image = Image.FromFile(strings[x, y]);
                            // for each iteration one part of the image is added to the rest of it
                            // this seems to be the part that is most time consuming.
                            // unfortunately, we can't multi-thread it either as it would rewrite data on the image
                            // on the same spot
                            g.DrawImage(image, x * imageDim, y * imageDim);

                            if (x == xCount - 1 && y == yCount - 1)
                            {
                                // converting the image to a base64 stirng that can be broadcasted
                                using (var stream = new MemoryStream())
                                {
                                    result.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                                    var bytes = stream.ToArray();
                                    var str = Convert.ToBase64String(bytes);
                                    base64String = str;
                                }
                            }
                        }
                        //          });
                        //        });
                    }
                }

                if (saveToFile) result.Save(path + @"\GH_Canvas.png");
                result.Dispose();
            }

            // after finishing the creation of the final image we have to delete the temporary files
            // otherwise they will slowly accumulate.
            // I tried to delete the files on the main thread, however, it would give the error: "Files are in use"
            // so I made another thread to deal with the files. This way the temporary images would be released by the
            // main thread and we can delete them instantly
            Thread tr = new Thread(DeleteTemporaryFiles);
            tr.Start();

            DA.SetData(0, base64String);
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
            get { return new Guid("e6aea055-343c-4959-b896-27fbece0a246"); }
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