using System.ServiceProcess;

namespace OCR_Windows_Service_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceBase.Run(new CreateJsonService());
        }
    }
}
