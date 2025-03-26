using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using FTServiceUtils;

namespace RTL.Services
{
    public class TscPrinterService
    {
        private readonly string _printerName;

        public TscPrinterService(string printerName)
        {
            _printerName = printerName;
        }

        public bool PrintLabel(string text)
        {
            try
            {
                string line1 = "";
                string line2 = "";

                if (text.Contains("\\n"))
                {
                    var lines = text.Split("\\n");
                    if (lines.Length == 2)
                    {
                        line1 = lines[0];
                        line2 = lines[1];
                    }
                }

                if (string.IsNullOrEmpty(line1))
                {
                    if (text.Length > 14)
                    {
                        line1 = text.Substring(0, 14);
                        line2 = text.Substring(14);
                    }
                    else
                        line1 = text;
                }

                // Используем объект ZebraLabel
                ZebraLabel label = new ZebraLabel(25, 12);
                ZebraBlock zb0 = label.addBlock(25, 12, 0);
                zb0.addASCIItext($"{line1}", 3, 0.4);
                if (!string.IsNullOrEmpty(line2))
                    zb0.addASCIItext($"{line2}", 3, 0.4);

                // Отправляем на печать
                label.PrintLabel(_printerName);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка печати: {ex.Message}");
                return false;
            }
        }
    }
}
/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using FTServiceUtils;

namespace RTL.Services
{
    public class TscPrinterService
    {
        private readonly string _printerName;

        public TscPrinterService(string printerName)
        {
            _printerName = printerName;
        }

        public bool PrintLabel(string text, string serialNumber, string imei)
        {
            try
            {
                string command = GenerateTscCommand(text, serialNumber, imei);
                return RawPrinterHelper.Print(_printerName, command);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка печати: {ex.Message}");
                return false;
            }
        }

        private string GenerateTscCommand(string text, string serialNumber, string imei)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SIZE 50 mm, 30 mm");
            sb.AppendLine("GAP 2 mm, 0 mm");
            sb.AppendLine("DIRECTION 1");
            sb.AppendLine("CLS");
            sb.AppendLine($"TEXT 100,100,\"3\",0,1,1,\"{text}\"");
            sb.AppendLine($"TEXT 100,150,\"3\",0,1,1,\"Serial: {serialNumber}\"");
            sb.AppendLine($"TEXT 100,200,\"3\",0,1,1,\"IMEI: {imei}\"");
            sb.AppendLine("PRINT 1");
            return sb.ToString();
        }
    }
}*/