using System;

namespace GammaPrintService
{
	[Serializable]
	public class Settings
	{
		private string printerName = "Zebra";
		private string pdfPath = "c:\\labels\\label.pdf";
		private string adamIpAddress;
		private uint portPrintSignal = 1;

		public string PrinterName
		{
			get { return printerName; }
			set { printerName = value; }
		}

		public string PdfPath
		{
			get { return pdfPath; }
			set { pdfPath = value; }
		}

		public string AdamIpAddress
		{
			get { return adamIpAddress; }
			set { adamIpAddress = value; }
		}

		public uint PortPrintSignal
		{
			get { return portPrintSignal; }
			set { portPrintSignal = value; }
		}
	}
}
