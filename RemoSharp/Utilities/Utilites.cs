using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Parameters.Hints;
using Grasshopper.Kernel.Special;
using ScriptComponents;
using System.Net.NetworkInformation;

namespace RemoSharp.RemoCommandTypes
{
    public class Utilites
    {

        //public static IGH_Param CreateServerMakerComponent(GH_Document document, System.Drawing.PointF pivot, int shiftX, int shiftY, bool IncludeAddressIP) 
        //{

        //    //var networkList = RemoSharp.RemoCommandTypes.Utilites.GetNetworkListDataFromPC();

        //    //int addressIndex = 0;
        //    //for (int i = 0; i < networkList.Count; i++)
        //    //{
        //    //    if (networkList[i].name.ToUpper().Contains("WIFI") || networkList[i].name.ToUpper().Contains("WI-FI"))
        //    //    {
        //    //        addressIndex = i;
        //    //    }
        //    //}
        //    //string[] ipNumbers = networkList[addressIndex].IP.Split('.');
        //    //string IP_01 = ipNumbers[0];
        //    //string IP_02 = ipNumbers[1];
        //    //string IP_03 = "";
        //    //string IP_04 = "";

        //    //if (IncludeAddressIP)
        //    //{
        //    //    IP_03 = ipNumbers[2];
        //    //    IP_04 = ipNumbers[3];
        //    //}


        //    //var serverAddressPivot = new System.Drawing.Point((int)pivot.X + 0 + shiftX, (int)pivot.Y - 0 + shiftY);

        //    int shiftInputsY = -48;
        //    //var togglePivot = new System.Drawing.Point((int)pivot.X - 246 + shiftX, (int)pivot.Y -145 + shiftY + shiftInputsY);
        //    var serverSamplesPivot = new System.Drawing.Point((int)pivot.X - 258 + shiftX, (int)pivot.Y -93 + shiftY + shiftInputsY);
        //    //var ip01PanelPivot = new System.Drawing.Point((int)pivot.X - 200 + shiftX, (int)pivot.Y + -10 + shiftY + shiftInputsY);
        //    //var ip02PanelPivot = new System.Drawing.Point((int)pivot.X - 200 + shiftX, (int)pivot.Y + 30 + shiftY + shiftInputsY);
        //    //var ip03PanelPivot = new System.Drawing.Point((int)pivot.X - 270 + shiftX, (int)pivot.Y + 70 + shiftY + shiftInputsY);
        //    //var ip04PanelPivot = new System.Drawing.Point((int)pivot.X - 270 + shiftX, (int)pivot.Y + 110 + shiftY + shiftInputsY);

        //    //RemoSharp.RemoCSharp.ServerAddress serverAddress = new RemoCSharp.ServerAddress();
        //    //serverAddress.CreateAttributes();
        //    //serverAddress.Attributes.Pivot = serverAddressPivot;
        //    //serverAddress.Params.RepairParamAssociations();
        //    //serverAddress.NickName = "RemoSetup";

        //    //Grasshopper.Kernel.Special.GH_BooleanToggle toggle = new GH_BooleanToggle();
        //    //toggle.CreateAttributes();
        //    //toggle.Attributes.Pivot = togglePivot;
        //    //toggle.Value = true;
        //    //toggle.ExpireSolution(true);
        //    //toggle.NickName = "RemoSetup";

        //    //RemoSharp.WS_Server_Samples wS_Server_Samples = new RemoSharp.WS_Server_Samples();
        //    //wS_Server_Samples.CreateAttributes();
        //    //wS_Server_Samples.Attributes.Pivot = serverSamplesPivot;
        //    //wS_Server_Samples.Params.RepairParamAssociations();
        //    //wS_Server_Samples.NickName = "RemoSetup";


        //    //GH_Panel ipPanel01 = new GH_Panel();
        //    //ipPanel01.CreateAttributes();
        //    //ipPanel01.Attributes.Pivot = ip01PanelPivot;
        //    //ipPanel01.Attributes.Bounds = new System.Drawing.RectangleF(ip01PanelPivot.X, ip01PanelPivot.Y, 80, 20);
        //    //ipPanel01.NickName = "RemoSetup";
        //    //ipPanel01.SetUserText(IP_01);

        //    //GH_Panel ipPanel02 = new GH_Panel();
        //    //ipPanel02.CreateAttributes();
        //    //ipPanel02.Attributes.Pivot = ip02PanelPivot;
        //    //ipPanel02.Attributes.Bounds = new System.Drawing.RectangleF(ip02PanelPivot.X, ip02PanelPivot.Y, 80, 20);
        //    //ipPanel02.NickName = "RemoSetup";
        //    //ipPanel02.SetUserText(IP_02);


        //    //GH_Panel ipPanel03 = new GH_Panel();
        //    //ipPanel03.CreateAttributes();
        //    //ipPanel03.Attributes.Pivot = ip03PanelPivot;
        //    //ipPanel03.Attributes.Bounds = new System.Drawing.RectangleF(ip03PanelPivot.X, ip03PanelPivot.Y, 150, 20);
        //    //ipPanel03.NickName = "RemoSetup";
        //    //ipPanel03.SetUserText(IP_03);
        //    //ipPanel03.Properties.Colour = System.Drawing.Color.FromArgb(165, 0, 0, 110);

        //    //GH_Panel ipPanel04 = new GH_Panel();
        //    //ipPanel04.CreateAttributes();
        //    //ipPanel04.Attributes.Pivot = ip04PanelPivot;
        //    //ipPanel04.Attributes.Bounds = new System.Drawing.RectangleF(ip04PanelPivot.X, ip04PanelPivot.Y, 150, 20);
        //    //ipPanel04.NickName = "RemoSetup";
        //    //ipPanel04.SetUserText(IP_04);
        //    //ipPanel04.Properties.Colour = System.Drawing.Color.FromArgb(165, 0, 0, 110);


        //    document.ScheduleSolution(0, (GH_Document doc) =>
        //     {
        //         //document.AddObject(serverAddress, true);
        //         //document.AddObject(toggle, true);
        //         document.AddObject(wS_Server_Samples, true);
        //         //document.AddObject(ipPanel01, true);
        //         //document.AddObject(ipPanel02, true);
        //         //document.AddObject(ipPanel03, true);
        //         //document.AddObject(ipPanel04, true);

        //         //serverAddress.Params.Input[0].AddSource(toggle);
        //         //serverAddress.Params.Input[1].AddSource(wS_Server_Samples.Params.Output[0]);
        //         //serverAddress.Params.Input[2].AddSource(ipPanel01);
        //         //serverAddress.Params.Input[3].AddSource(ipPanel02);
        //         //serverAddress.Params.Input[4].AddSource(ipPanel03);
        //         //serverAddress.Params.Input[5].AddSource(ipPanel04);
        //     });

        //    //IGH_Param[] outPutArray = 
        //    //    { 
        //    //    serverAddress.Params.Output[0],
        //    //    serverAddress.Params.Output[1],
        //    //    serverAddress.Params.Output[2],
        //    //    serverAddress.Params.Output[3],
        //    //    serverAddress.Params.Output[4]
        //    //    };

        //    return wS_Server_Samples.Params.Output[0];
        //}

        public static List<NetworkConfig> GetNetworkListDataFromPC()
        {
            List<NetworkConfig> networkConfigs = new List<NetworkConfig>();
            networkConfigs.Add(new NetworkConfig(0, "Local Server", "127.0.0.1"));
            int index = 1;
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {

                    string wifi_name = ni.Name;
                    string ipAddress = "";
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            ipAddress = ip.Address.ToString();

                        }
                    }
                    NetworkConfig networkConfig = new NetworkConfig(index, wifi_name, ipAddress);
                    networkConfigs.Add(networkConfig);
                    index++;
                }
            }

            return networkConfigs;
        }

        public class NetworkConfig
        {
            public int index;
            public string name;
            public string IP;

            public NetworkConfig(int index, string name, string IP)
            {
                this.index = index;
                this.name = name;
                this.IP = IP;

            }

            public override string ToString()
            {
                return $"{this.index}  {this.name}  {this.IP}";
            }
        }

        }
}
