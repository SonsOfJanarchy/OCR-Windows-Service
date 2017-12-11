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
            aTimer = new System.Timers.Timer(10000);
            aTimer.Elapsed += new ElapsedEventHandler(Actions);
            Console.WriteLine("Press the Enter key to exit the program.");
            Console.ReadLine();
            //Thread thread = new Thread(Actions);
            //thread.Start();
        }

        private static void Actions(object source, ElapsedEventArgs e)
        {
            var files = new DirectoryInfo(@"C:\WindowsServiceInput\")
                .GetFiles();

            //var wacther = new FileSystemWatcher();

            foreach (var file in files)
            {
                var ocr = new Tesseract();
                var image = Image.FromFile(file.FullName);
                ocr.Init(@"..\..\Content\tessdata", "eng", false);
                var result = ocr.DoOCR((Bitmap)image, Rectangle.Empty);
                List<string> data = new List<string>();
                foreach (Word word in result)
                    data.Add(word.Text);

                string json = JsonConvert.SerializeObject(data.ToArray());
                System.IO.File.WriteAllText(@"C:\WindowsServiceOutput\" + Path.GetFileNameWithoutExtension(file.Name) + ".json", json);
            }
        }
    }
}