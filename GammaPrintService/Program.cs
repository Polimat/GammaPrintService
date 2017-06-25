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
	        do
	        {
		        key = Console.ReadKey(true).Key;
		        switch (key)
		        {
		        }
	        } while (key != ConsoleKey.Escape);
		}

	    private static PrintService service;
    }
}
