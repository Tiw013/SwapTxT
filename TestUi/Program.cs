using System;
using System.Windows;
using SwapTxT;
using SwapTxT.Models;

class Program
{
    [STAThread]
    static void Main()
    {
        var app = new Application();
        try
        {
            Console.WriteLine("Creating window...");
            var win = new AboutWindow(new AppSettings());
            Console.WriteLine("Window created.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("CRASH_DUMP_START");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("CRASH_DUMP_END");
        }
    }
}
