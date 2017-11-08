using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using GammaPrintService.Common;
//using GammaService.Common;

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
		/// Номер входа на адаме для печати (ГУ выдвинута на позицию)
		/// </summary>
	    private uint inputPrint;
        /// <summary>
        /// Номер входа на адаме для сигнала о позиции вппликатора
        /// </summary>
        private uint inputApplicatorReady;
		/// <summary>
		/// Номер выхода сигнала печати
		/// </summary>
	    private uint outPrint;

        ///// <summary>
        ///// Период опроса адама в мс
        ///// </summary>
        //private uint modbusDeviceTickTimeInMs;
        ///// <summary>
        ///// Пауза в мс между сигналом true на печать и сменой на false
        ///// </summary>
        //private int sendSignalPauseInMs;
        /// <summary>
        /// Длина паузы после печати этикетки
        /// </summary>
        private int lengthPauseAfterPrintLabelInMs;

        private bool printInputState = false;
	    private bool applicatorReady = true;
	    private bool labelReady = true;

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
            //modbusDeviceTickTimeInMs = settings.ModbusDeviceTickTimeInMs;
            //sendSignalPauseInMs = (int)settings.SendSignalPauseInMs;
            lengthPauseAfterPrintLabelInMs = (int)settings.LengthPauseAfterPrintLabelInMs;
            device = new ModbusDevice(DeviceType.ADAM6060, settings.AdamIpAddress, (int)settings.ModbusDeviceTickTimeInMs, (int)settings.SendSignalPauseInMs);
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
                    Console.WriteLine("Ярлык приклеен");
                    Console.WriteLine("labelReady: false <-" + labelReady);
                    labelReady = false;
				}
                Console.WriteLine("printInputState: " + value + "<-" + printInputState);
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
                Console.WriteLine("applicatorReady: " + value + "<-" + applicatorReady);
                applicatorReady = value;
		    }
	    }

        #endregion

        #region Private methods

        public void SendSignal(bool signal)
        {
            try
            {
                var outSignals = new Dictionary<int, bool>
                    {
                        {
                            (int) outPrint, signal
                        }
                    };
                device.SendSignal(outSignals);
                Console.WriteLine("Выход "+ outPrint + " Состояние "+signal);
            }
            catch (Exception)
            {
                Console.WriteLine("Не удалось отправить сигнал");
            }
        }

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

	    public void PrintLabel()
	    {
		    labelReady = false;
            //if (!PrintImage.SendImageToPrinter(pdfPath, printerName))
            if (!PdfPrint.PrintPdfDocument(pdfPath, printerName))
            {
			    Console.WriteLine("При печати произошла ошибка");
				return;
		    }
            else
            {
                Console.WriteLine("Печать произведена успешно");
            }
            Thread.Sleep(lengthPauseAfterPrintLabelInMs);
		    labelReady = true;
	    }

        public void ChangeStatePrintSignal()
        {
            bool[] bDIData;
            bDIData = new bool[6];
            bDIData[inputPrint - 1] = PrintInputState;
            bDIData[inputApplicatorReady - 1] = !ApplicatorReady;
            OnModbusInputDataReceived(bDIData) ;
        }

        public void ChangeStateApplicatorReadySignal()
        {
            bool[] bDIData;
            bDIData = new bool[6];
            bDIData[inputPrint - 1] = !PrintInputState;
            bDIData[inputApplicatorReady - 1] = ApplicatorReady;
            OnModbusInputDataReceived(bDIData);
        }

        public void OnModbusInputDataReceived(bool[] diData) 
	    {
		    if (diData == null || diData.Length < Math.Max(inputPrint, inputApplicatorReady))
		    {
			    return;
		    }
            Console.WriteLine((inputPrint-1).ToString()+": " + !diData[inputPrint - 1] + "  " + (inputApplicatorReady - 1).ToString() + ": " + !diData[inputApplicatorReady - 1] + "  "+ DateTime.Now);
            //for (int i = 0; i <= 5; i++)
            //{
            //    Console.WriteLine(i + " " + diData[i].ToString() + " " + DateTime.Now);
            //    using (StreamWriter sw = new StreamWriter(@"d:\cprojects\adam.txt", true, System.Text.Encoding.Default))
            //    {
            //        sw.WriteLine(i + " " + diData[i].ToString() + " "+DateTime.Now);
            //        sw.Close();
            //    }
            //}

            //if (PrintInputState != !diData[inputPrint - 1] || ApplicatorReady != !diData[inputApplicatorReady - 1])
            //{
            //    Console.WriteLine("0: " + !diData[inputPrint - 1]+"<-"+ PrintInputState+"/"+ inputPrint);
            //    Console.WriteLine("1: " + !diData[inputApplicatorReady - 1] + "<-" + ApplicatorReady+"/"+ inputApplicatorReady);
            //}
            PrintInputState = !diData[inputPrint - 1];
            ApplicatorReady = !diData[inputApplicatorReady - 1];
        }

        #endregion

    }
}
