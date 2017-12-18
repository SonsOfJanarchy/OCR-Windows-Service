using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Ports;
using System.Net.Configuration;
using System.Net.Mime;
using Dynamsoft.Barcode;
using Newtonsoft.Json;
using PV_Doc_Template;
using tessnet2;

namespace OCR_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            //CreateJson();
            ReadBarCode();
        }

        private static void ReadBarCode()
        {
            var reader = new BarcodeReader();
            string dbrLicenseKeys = "t0068MgAAAE/4ptMgtvYh/3/aHYqyZSg9d1L+JaBCahbvfx+h4pmOrJUH1XLf+UG9o+yhRBil3A3DocqUs7B7uUkWBm8BdD0=";
            string dntLicenseKeys = "t0068MgAAALldOkIqmvuL1dexYI8Jhxh+fkxAhf+naIiM0IGi1dLzDdLMcnAsG0GdcxrH9vwSx4/amTOZC6vGeEml9Vm3lLE=";
            reader.LicenseKeys = dbrLicenseKeys;
            ReaderOptions option = new ReaderOptions();
            option.BarcodeFormats = BarcodeFormat.PDF417;
            //option.MaxBarcodesToReadPerPage = 100;
            //option.BarcodeColorMode = BarcodeColorMode.BCM_DarkAndLight;
            reader.ReaderOptions = option;
            var files = new DirectoryInfo(@"C:\WindowsServiceInput\").GetFiles();
            foreach (var file in files)
            {
                var image = Image.FromFile(file.FullName);
                var anotherimage = (Bitmap)image;
                BarcodeResult[] result = reader.DecodeFile(file.FullName);
                var data = result.FirstOrDefault(x => x.BarcodeFormat == BarcodeFormat.PDF417).BarcodeText;

                IdentificationReturnModel model = new IdentificationReturnModel();

                var strings = data.Split();

                //var barcodetext = data.BarcodeText.Count(y => y == 'n');
                //var json = JsonConvert.SerializeObject(result);
            }
            
        }

        private static void ReadBarCodeInfo(BarcodeResult[] result)
        {
            
        }

        private static void CreateJson()
        {
            //using (Bitmap bitmap = (Bitmap)Image.FromFile("file.jpg"))
            //{
            //    using (Bitmap newBitmap = new Bitmap(bitmap))
            //    {
            //        newBitmap.SetResolution(300, 300);
            //        newBitmap.Save("file300.jpg", ImageFormat.Jpeg);
            //    }
            //}

            var dataItems = new OCRRawDataModel();
            var files = new DirectoryInfo(@"C:\WindowsServiceInput\").GetFiles();
            foreach (var file in files)
            {
                if (!File.Exists(@"C:\WindowsServiceOutput\" + file.Name))
                {
                    var ocr = new Tesseract();
                    var image = Image.FromFile(file.FullName);
                    var anotherimage = (Bitmap) image;
                    var getanotherimage = Resize(anotherimage, (3000), (3000), false);
                    var bit = (Bitmap) getanotherimage;
                    bit.SetResolution(300,300);

                    //var blackAndWhite = BlackAndWhite(getanotherimage, new Rectangle(0,0, getanotherimage.Width, getanotherimage.Height));
                    //getanotherimage.SetResolution(1000, 1000);
                    //getanotherimage.Save(@"C:\WindowsServiceOutput\BlackAndWhite.bmp");
                    ocr.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-.,/");
                    ocr.Init(@"..\..\Content\tessdata", "eng", false);
                    var result = ocr.DoOCR(bit, Rectangle.Empty);
                    dataItems.DataList = new List<OCRRawDataModel.RawDataItem>();

                    foreach (Word word in result)
                    {
                        var item = new OCRRawDataModel.RawDataItem();
                        item.Value = word.Text;
                        item.Confidence = (int)word.Confidence;
                        item.LineIndex = word.LineIndex;
                        dataItems.DataList.Add(item);
                    }
                    var anotherjson = JsonConvert.SerializeObject(dataItems);
                    
                    var mapper = new IdentificationCardMapper();
                    //var mappedObjects = mapper.MapDriversLicenseData(dataItems);
                    var json = JsonConvert.SerializeObject(dataItems);
                    //string json2 = JsonConvert.SerializeObject(data.ToArray());
                    System.IO.File.WriteAllText(@"C:\WindowsServiceOutput\" + Path.GetFileNameWithoutExtension(file.Name) + ".json", json);

                    image.Dispose();
                }
                //file.Delete();
            }
            
        }

        private int PerceivedBrightness(Color c)
        {
            return (int)Math.Sqrt(
                c.R * c.R * .299 +
                c.G * c.G * .587 +
                c.B * c.B * .114);
        }

        private static Bitmap BlackAndWhite(Bitmap image, Rectangle rectangle)
        {
            Bitmap blackAndWhite = new System.Drawing.Bitmap(image.Width, image.Height);

            // make an exact copy of the bitmap provided
            using (Graphics graphics = System.Drawing.Graphics.FromImage(blackAndWhite))
                graphics.DrawImage(image, new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                    new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

            // for every pixel in the rectangle region
            for (Int32 xx = rectangle.X; xx < rectangle.X + rectangle.Width && xx < image.Width; xx++)
            {
                for (Int32 yy = rectangle.Y; yy < rectangle.Y + rectangle.Height && yy < image.Height; yy++)
                {
                    // average the red, green and blue of the pixel to get a gray value
                    Color pixel = blackAndWhite.GetPixel(xx, yy);
                    Int32 sum = (pixel.R + pixel.G + pixel.B);

                    if (sum <= 400)
                    {
                        blackAndWhite.SetPixel(xx, yy, Color.FromArgb(0, 0, 0));
                    }
                    else
                    {
                        blackAndWhite.SetPixel(xx, yy, Color.FromArgb(255, 255, 255));
                    }       
                            
                    
                }
            }

            return blackAndWhite;
        }

        private static Bitmap ResizeBitmap(Bitmap sourceBMP, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
                g.DrawImage(sourceBMP, 0, 0, width, height);
            return result;
        }

        public static Image Resize(Image image, int newWidth, int maxHeight, bool onlyResizeIfWider)
        {
            if (onlyResizeIfWider && image.Width <= newWidth) newWidth = image.Width;

            var newHeight = image.Height * newWidth / image.Width;
            if (newHeight > maxHeight)
            {
                // Resize with height instead  
                newWidth = image.Width * maxHeight / image.Height;
                newHeight = maxHeight;
            }

            var res = new Bitmap(newWidth, newHeight);

            using (var graphic = Graphics.FromImage(res))
            {
                graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphic.SmoothingMode = SmoothingMode.HighQuality;
                graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphic.CompositingQuality = CompositingQuality.HighQuality;
                graphic.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return res;
        }
    }
}
