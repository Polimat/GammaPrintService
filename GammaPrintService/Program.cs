using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GammaPrintService
{
    class Program
    {
        static void Main(string[] args)
        {
			service = new PrintService();
	        Console.WriteLine("Press ESC to stop");
	        ConsoleKey key;
            //bool[] bDIData;
            //bDIData = new bool[6];
            //bDIData[0] = true;
            //bDIData[1] = false;
            Console.WriteLine("F4 - печать этикетки; F5 - смена состояния на входе толкателя; F6 - смена состояния на входе аппликатора; F7 - сигнал True на выход; F8 - Сигнал false на выход.");
            //Console.WriteLine("0 - " + !bDIData[0]);
            //Console.WriteLine("1 - " + !bDIData[1]);
            do
            {
		        key = Console.ReadKey(true).Key;
		        switch (key)
		        {
                    case ConsoleKey.F4:
                        service.PrintLabel();
                        break;
                    case ConsoleKey.F5:
                        service.ChangeStatePrintSignal();
                        //bDIData[0] = !bDIData[0];
                        //service.OnModbusInputDataReceived(bDIData);
                        break;
                    case ConsoleKey.F6:
                        service.ChangeStateApplicatorReadySignal();
                        //bDIData[1] = !bDIData[1];
                        //service.OnModbusInputDataReceived(bDIData);
                        break;
                    case ConsoleKey.F7:
                        service.SendSignal(true);
                        break;
                    case ConsoleKey.F8:
                        service.SendSignal(false);
                        break;
                    //case ConsoleKey.F9:
                    //    for (int i = 6; i <= 11; i++)
                    //    {
                    //        service.SendSignal(i,true);

                    //    }
                    //    break;
                }
            } while (key != ConsoleKey.Escape);
		}
	    private static PrintService service;
    }
}
