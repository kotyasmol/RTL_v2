using System;
using System.Drawing.Printing;
using System.Management;
using System.Text;
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
                if (string.IsNullOrEmpty(barcode) || string.IsNullOrEmpty(serialNumber))
                    return false;

                ZebraLabel label = new ZebraLabel(30, 10);

                ZebraBlock barcodeBlock = label.addBlock(0, 0, 0, 0, -5);
                barcodeBlock.addBarCodeType(barcode, 4, 8, BarCodeType.Code128);

                ZebraBlock textBlock = label.addBlock(0, 0, 0, 0, 4);
                textBlock.addASCIItext($"{serialNumber}", 3, 1);

                label.PrintLabel(_printerName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IsPrinterOnline()
        {
            try
            {
                var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_Printer WHERE Name = '{_printerName.Replace("\\", "\\\\")}'");

                foreach (ManagementObject printer in searcher.Get())
                {
                    bool workOffline = (bool)printer["WorkOffline"];
                    int status = Convert.ToInt32(printer["PrinterStatus"]);

                    return !workOffline && status == 3; // 3 = Idle/Ready
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsPrinterInstalled()
        {
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                if (printer.Equals(_printerName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
