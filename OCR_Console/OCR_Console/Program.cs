using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using Dynamsoft.Barcode;
using Newtonsoft.Json;
using PV_Doc_Template;
using tessnet2;

namespace OCR_Console
{
    class Program
    {
        private static System.IO.FileSystemWatcher m_BarCodeWatcher = new System.IO.FileSystemWatcher(@"C:\BarCodeServiceInput\");
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, eArgs) => {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };
            //CreateJson();
            
            m_BarCodeWatcher.EnableRaisingEvents = true;
            m_BarCodeWatcher.Created += (o, eventArgs) => ReadBarCode(o, eventArgs);

            _quitEvent.WaitOne();
        }

        private static void ReadBarCode(Object sender, FileSystemEventArgs e)
        {
            System.Threading.Thread.Sleep(2000);
            var reader = new BarcodeReader();
            string dbrLicenseKeys = "t0068MgAAAE/4ptMgtvYh/3/aHYqyZSg9d1L+JaBCahbvfx+h4pmOrJUH1XLf+UG9o+yhRBil3A3DocqUs7B7uUkWBm8BdD0=";
            reader.LicenseKeys = dbrLicenseKeys;
            ReaderOptions option = new ReaderOptions();
            option.BarcodeFormats = BarcodeFormat.PDF417;
            reader.ReaderOptions = option;
            var files = new DirectoryInfo(@"C:\BarCodeServiceInput\").GetFiles();

            foreach (var file in files)
            {
                if (!File.Exists(@"C:\WindowsServiceOutput\" + file.Name))
                {
                    BarcodeResult[] result = reader.DecodeFile(file.FullName);
                    if (result == null)
                    {
                        file.Delete();
                        return;
                    }
                    var data = result.FirstOrDefault(x => x.BarcodeFormat == BarcodeFormat.PDF417).BarcodeText;
                    IdentificationReturnModel model = new IdentificationReturnModel();

                    var strings = data.Split(new string[] {"\n"}, StringSplitOptions.None);
                    foreach (var str in strings)
                    {
                        if (str.StartsWith("DBB"))
                        {
                            var bday = str.Replace("DBB", "");
                            bday = bday.Insert(4, "/");
                            bday = bday.Insert(7, "/");
                            model.dateofBirth = Convert.ToDateTime(bday);
                        }
                        else if (str.Contains("DAA"))
                        {
                            var split = str.Split(',');
                            if (split.Any())
                            {
                                var beginIndex = split[0].IndexOf("DAA", StringComparison.Ordinal) + 3;
                                model.lastName = split[0].Substring(beginIndex, (split[0].Length - beginIndex));
                            }

                            if (split.Count() > 1)
                            {
                                model.firstName = split[1];
                            }
                            if (split.Count() > 2)
                            {
                                model.middleName = split[2];
                            }
                        }
                        else if (str.StartsWith("DAG"))
                        {
                            model.address1 = str.Replace("DAG", "");
                        }
                        else if (str.StartsWith("DAI"))
                        {
                            model.city = str.Replace("DAI", "");
                        }
                        else if (str.StartsWith("DAJ"))
                        {
                            model.state = str.Replace("DAJ", "");
                        }
                        else if (str.StartsWith("DAK"))
                        {
                            var zip = str.Replace("DAK", "");
                            zip = zip.TrimEnd();
                            zip = zip.Insert(5, "-");
                            model.zip = zip;
                        }
                        else if (str.StartsWith("DBC"))
                        {
                            model.sex = str.Replace("DBC", "");
                        }
                    }

                    var json = JsonConvert.SerializeObject(model);
                    System.IO.File.WriteAllText(
                        @"C:\WindowsServiceOutput\" + Path.GetFileNameWithoutExtension(file.Name) + ".json", json);
                }
                file.Delete();
            }
        }

        private static void ReadBarCodeInfo(BarcodeResult[] result)
        {
            
        }

        private static void CreateJson()
        {
            var dataItems = new OCRRawDataModel();
            var files = new DirectoryInfo(@"C:\OcrServiceInput\").GetFiles();
            foreach (var file in files)
            {
                if (!File.Exists(@"C:\WindowsServiceOutput\" + file.Name))
                {
                    var ocr = new Tesseract();
                    var image = Image.FromFile(file.FullName);
                    var anotherimage = (Bitmap) image;
                    var getanotherimage = Resize(anotherimage, (5000), (5000), false);
                    var bit = (Bitmap) getanotherimage;
                    bit.SetResolution(300,300);

                    var blackAndWhite = BlackAndWhite(bit, new Rectangle(0,0, getanotherimage.Width, getanotherimage.Height));
                    //getanotherimage.SetResolution(1000, 1000);
                    //blackAndWhite.Save(@"C:\WindowsServiceOutput\BlackAndWhite.bmp");
                    ocr.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-.,/");
                    ocr.Init(@"..\..\Content\tessdata", "eng", false);
                    var result = ocr.DoOCR(blackAndWhite, Rectangle.Empty);
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
                    var json = JsonConvert.SerializeObject(dataItems);
                    System.IO.File.WriteAllText(@"C:\WindowsServiceOutput\" + Path.GetFileNameWithoutExtension(file.Name) + ".json", json);

                    image.Dispose();
                }
                file.Delete();
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
