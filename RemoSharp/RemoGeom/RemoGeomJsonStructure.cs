using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RemoSharp
{
    class RemoGeomJsonStructure
    {
        public string TreePath { get; set; }
        public string Data { get; set; }

        public RemoGeomJsonStructure(string treePath, string data)
        {
            this.TreePath = treePath;
            this.Data = data;
        }

        public static int[] TreePathIntArray(string treePathString)
        {
            string jutIndecies = treePathString.Substring(1, treePathString.Length - 2);
            string[] indeciesStr = jutIndecies.Split(';');

            int[] indecies = new int[indeciesStr.Length];
            for (int i = 0; i < indeciesStr.Length; i++)
            {
                indecies[i] = Convert.ToInt32(indeciesStr[i]);
            }
            return indecies;
        }


    }

    class ImagePartBase64
    {
        public string imagePath;
        public string imageBase64;

        public ImagePartBase64(string imagePath, string imageBase64)
        {
            this.imagePath = imagePath;
            this.imageBase64 = imageBase64;
        }
    }


}
