using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Net.Configuration;
using System.Net.Mime;
using Newtonsoft.Json;
using PV_Doc_Template;
using tessnet2;

namespace OCR_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateJson();
        }

        private static void CreateJson()
        {
            var dataItems = new OCRRawDataModel();
            var files = new DirectoryInfo(@"C:\WindowsServiceInput\").GetFiles();
            foreach (var file in files)
            {
                if (!File.Exists(@"C:\WindowsServiceOutput\" + file.Name))
                {
                    var ocr = new Tesseract();
                    var image = Image.FromFile(file.FullName);
                    ocr.Init(@"..\..\Content\tessdata", "eng", false);
                    var result = ocr.DoOCR((Bitmap) image, Rectangle.Empty);
                    dataItems.DataList = new List<OCRRawDataModel.RawDataItem>();

                    foreach (Word word in result)
                    {
                        var item = new OCRRawDataModel.RawDataItem();
                        item.Value = word.Text;
                        item.Confidence = (int)word.Confidence;
                        item.LineIndex = word.LineIndex;
                        dataItems.DataList.Add(item);
                    }
                    
                    var mapper = new IdentificationCardMapper();
                    var json = mapper.MapDriversLicenseData(dataItems);
                    //System.IO.File.WriteAllText(@"C:\WindowsServiceOutput\" + Path.GetFileNameWithoutExtension(file.Name) + ".json", json);

                    image.Dispose();
                }
                file.Delete();
            }
            
        }
    }
}
