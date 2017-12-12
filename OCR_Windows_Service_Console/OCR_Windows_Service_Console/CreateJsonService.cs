using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using Newtonsoft.Json;
using tessnet2;

namespace OCR_Windows_Service_Console
{
    public class CreateJsonService : ServiceBase
    {
        private static System.Timers.Timer aTimer;

        public CreateJsonService()
        {
            //aTimer = new System.Timers.Timer(10000);
            //aTimer.Elapsed += new ElapsedEventHandler(Actions);
            //Console.WriteLine("Press the Enter key to exit the program.");
            //Console.ReadLine();
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
                    var result = ocr.DoOCR((Bitmap) image, Rectangle.Empty);
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