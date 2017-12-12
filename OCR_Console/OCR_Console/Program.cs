using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Net.Mime;
using Newtonsoft.Json;
using tessnet2;

namespace OCR_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateJson();
            CleanUpFiles();
        }

        private static void CreateJson()
        {
            var files = new DirectoryInfo(@"C:\WindowsServiceInput\").GetFiles();
            foreach (var file in files)
            {
                if (!File.Exists(@"C:\WindowsServiceOutput\" + file.Name))
                {
                    var ocr = new Tesseract();
                    var image = Image.FromFile(file.FullName);
                    ocr.Init(@"..\..\Content\tessdata", "eng", false);
                    var result = ocr.DoOCR((Bitmap)image, Rectangle.Empty);
                    List<string> data = new List<string>();
                    foreach (Word word in result)
                        data.Add(word.Text);

                    string json = JsonConvert.SerializeObject(data.ToArray());
                    System.IO.File.WriteAllText(
                        @"C:\WindowsServiceOutput\" + Path.GetFileNameWithoutExtension(file.Name) + ".json", json);
                }
            }
        }

        private static void CleanUpFiles()
        {
            var files = new DirectoryInfo(@"C:\WindowsServiceInput\");

            foreach (FileInfo file in files.GetFiles())
            {
                file.Delete();
            }
        }
    }
}
