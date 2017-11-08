using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using System.Drawing.Imaging;

namespace GammaPrintService.Common
{
    public class PdfPrint
    {
        #region dll Wrappers
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetDefaultPrinter(StringBuilder pszBuffer, ref int size);
        #endregion dll Wrappers

        public static bool PrintPdfDocument(string pdfFileName, string printerName = null)
        {
            try
            {
                var mainRasterizer = CreateRasterizer(pdfFileName); // нужен для посчета страниц

                PageStore = new Dictionary<int, MemoryStream>();
                PdfProcessing(1, mainRasterizer.PageCount, pdfFileName);
                while (PageStore.Count < mainRasterizer.PageCount)
                { // ждем завершения рендеринга
                  //Console.WriteLine("{0:000.0}%", ((double)PageStore.Count) / mainRasterizer.PageCount * 100);
                  //Thread.Sleep(100);
                }
                //Console.WriteLine("Start printing");
                PrintPages(PageStore, printerName);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public static Dictionary<int, MemoryStream> PageStore; //хранилище отрендеренных изображений
        private const int Dpi = 200;

        static GhostscriptVersionInfo _lastInstalledVersion = GhostscriptVersionInfo.GetLastInstalledVersion(GhostscriptLicense.GPL | GhostscriptLicense.AFPL, GhostscriptLicense.GPL);

        static GhostscriptRasterizer CreateRasterizer(string file)
        {
            var rasterizer = new GhostscriptRasterizer();
            rasterizer.CustomSwitches.Add("-dNOINTERPOLATE");
            rasterizer.CustomSwitches.Add("-dCOLORSCREEN=0");
            rasterizer.CustomSwitches.Add("-sPAPERSIZE=a4");
            rasterizer.TextAlphaBits = 4;
            rasterizer.GraphicsAlphaBits = 4;

            rasterizer.Open(file, _lastInstalledVersion, true);

            return rasterizer;
        }

        static void PdfProcessing(int StartPage, int EndPage, string SourcefilePath)
        {
            var rasterizer = CreateRasterizer(SourcefilePath);

            for (var pageNumber = StartPage; pageNumber <= EndPage; pageNumber++)
            {
                using (var img = rasterizer.GetPage(Dpi, Dpi, pageNumber))
                {
                    var mem = new MemoryStream();
                    img.Save(mem, ImageFormat.Jpeg);

                    lock (PageStore)
                    {
                        PageStore[pageNumber] = mem;
                    }
                }
            }
        }

        static void PrintPages(Dictionary<int, MemoryStream> pageStore, string printerName = null)
        {
            using (var pd = new PrintDocument())
            {
                StringBuilder dp = new StringBuilder(256);
                int size = dp.Capacity;
                if (string.IsNullOrEmpty(printerName))
                {
                    if (GetDefaultPrinter(dp, ref size))
                    {
                        pd.PrinterSettings.PrinterName = dp.ToString().Trim();
                    }
                }
                else
                {
                    pd.PrinterSettings.PrinterName = printerName;
                }

                pd.PrinterSettings.Duplex = Duplex.Simplex;
                pd.PrintController = new StandardPrintController();

                var index = 0;
                pd.PrintPage += (o, e) => {
                    var pageStream = pageStore[index + 1];
                    var img = System.Drawing.Image.FromStream(pageStream);

                    e.Graphics.DrawImage(img, e.Graphics.VisibleClipBounds);

                    index++;
                    e.HasMorePages = index < pageStore.Count;

                    //Console.WriteLine("Print {0} of {1}; complete {2:000.0}%", index, pageStore.Count, ((double)index) / pageStore.Count * 100);

                };
                pd.Print();
            }
        }


 
    }
}
