using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using GammaPrintService.Common;
using GammaService.Common;

namespace GammaPrintService
{
    public class PrintService
    {
		#region fields

		private const string SettingsFile = "settings.xml";
	    private ModbusDevice device;
	    private string printerName;
	    private string pdfPath;
		/// <summary>
		/// Номер входа на адаме для печати
		/// </summary>
	    private uint inputPrint;

	    private uint inputApplicatorReady;

		/// <summary>
		/// Номер выхода сигнала печати
		/// </summary>
	    private uint outPrint;

	    private bool printInputState;
	    private bool applicatorReady;
	    private bool labelReady;

	    #endregion


		#region Constructors

		public PrintService()
		{
			Settings settings;
		    if (File.Exists(SettingsFile))
		    {
			    try
			    {
				    using (var fs = new FileStream(SettingsFile, FileMode.Open))
				    {
					    var serializer = new XmlSerializer(typeof(Settings));
					    settings = (Settings) serializer.Deserialize(fs);
				    }
			    }
			    catch (Exception)
			    {
				    settings = new Settings();
					SaveSettings(settings);
			    }
		    }
		    else
		    {
			    settings = new Settings();
				SaveSettings(settings);
		    }
		    printerName = settings.PrinterName;
		    pdfPath = settings.PdfPath;
			inputPrint = settings.InPortPrintSignal;
			inputApplicatorReady = settings.PortApplicatorReady;
			outPrint = settings.OutPortPrintSignal;
			device = new ModbusDevice(DeviceType.ADAM6060, settings.AdamIpAddress);
			device.OnDIDataReceived += OnModbusInputDataReceived;
		}

		#endregion

	    #region Properties

	    private bool PrintInputState
	    {
		    get { return printInputState; }
			set {
				if (printInputState == value)
				{
					return;
				}
				if (value && labelReady)
				{
					var outSignals = new Dictionary<int, bool>
					{
						{
							(int) outPrint, true
						}
					};
					device.SendSignal(outSignals);
					labelReady = false;
				}
				printInputState = value;
			}
	    }

	    private bool ApplicatorReady
	    {
		    get { return applicatorReady; }
		    set
		    {
			    if (applicatorReady == value)
			    {
				    return;
			    }
			    if (value && !labelReady)
			    {
				    PrintLabel();
			    }
			    applicatorReady = value;
		    }
	    }

	    #endregion

		#region Private methods

		private void SaveSettings(Settings settings)
	    {
		    try
		    {
			    using (var fs = new FileStream(SettingsFile, FileMode.Create))
			    {
				    var serializer = new XmlSerializer(typeof(Settings));
				    serializer.Serialize(fs, settings);
			    }
		    }
		    catch (Exception)
		    {
			    Console.WriteLine("Не удалось сохранить настройки");
		    }
	    }

	    private void PrintLabel()
	    {
		    labelReady = false;
		    if (!RawPrinterHelper.SendFileToPrinter(pdfPath, printerName))
		    {
			    Console.WriteLine("При печати произошла ошибка");
				return;
		    }
			Thread.Sleep(3000);
		    labelReady = true;
	    }

		private void OnModbusInputDataReceived(bool[] diData) 
	    {
		    if (diData == null || diData.Length < Math.Max(inputPrint, inputApplicatorReady))
		    {
			    return;
		    }
		    PrintInputState = diData[inputPrint - 1];
		    ApplicatorReady = diData[inputApplicatorReady - 1];
	    }

		#endregion

	}
}
