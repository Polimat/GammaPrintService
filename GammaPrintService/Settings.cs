using System;

namespace GammaPrintService
{
	[Serializable]
	public class Settings
	{
		public string PrinterName { get; set; } = "Zebra";

		public string PdfPath { get; set; } = "c:\\labels\\label.pdf";

		public string AdamIpAddress { get; set; } = "192.168.0.1";

		public uint InPortPrintSignal { get; set; } = 1;

		public uint PortApplicatorReady { get; set; } = 2;

		public uint OutPortPrintSignal { get; set; } = 1;

        public uint ModbusDeviceTickTimeInMs { get; set; } = 100;

        public uint SendSignalPauseInMs { get; set; } = 200;

        public uint LengthPauseAfterPrintLabelInMs { get; set; } = 2000;
    }
}
