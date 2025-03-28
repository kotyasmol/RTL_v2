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
using FTServiceUtils.Enums;

namespace RTL.Services
{
    public class TscPrinterService
    {
        private readonly string _printerName;

        public TscPrinterService(string printerName)
        {
            _printerName = printerName;
        }



        public bool PrintLabel(string barcode, string serialNumber)
        {
            try
            {
                // Убедись, что данные передаются корректно
                if (string.IsNullOrEmpty(barcode) || string.IsNullOrEmpty(serialNumber))
                {
                    Console.WriteLine("Ошибка: штрихкод или серийный номер пустые.");
                    return false;
                }

                // Создаём ZPL-этикетку 50x30 мм
                ZebraLabel label = new ZebraLabel(30, 10);

                // Добавляем штрихкод (первая строка)
                ZebraBlock barcodeBlock = label.addBlock(0, 0, 0, 0, -5);  //координаты 
                barcodeBlock.addBarCodeType(barcode, 4, 8, BarCodeType.Code128);

                // Добавляем текст для серийного номера (вторая строка)
                ZebraBlock textBlock = label.addBlock(0, 0, 0, 0,4);  // координаты
                textBlock.addASCIItext($"{serialNumber}", 3, 1);




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






        private string GenerateTscCommand(string serialNumber, string imei)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SIZE 50 mm, 30 mm");  // Размер этикетки
            sb.AppendLine("GAP 2 mm, 0 mm");    // Зазор между этикетками
            sb.AppendLine("DIRECTION 1");       // Направление печати
            sb.AppendLine("CLS");               // Очистка буфера

            // Добавляем текст

            sb.AppendLine($"TEXT 100,100,\"3\",0,1,1,\"Serial: {serialNumber}\"");

            // Добавляем штрихкод
            sb.AppendLine($"BARCODE 100,200,\"128\",60,1,0,2,2,\"{serialNumber}\"");

            sb.AppendLine("PRINT 1");  // Отправляем команду на печать

            return sb.ToString();
        }

    }
}
