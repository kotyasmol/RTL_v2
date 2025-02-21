using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTL.Models
{
    public class ReportModel
    {
        // Информация о стенде
        public static int StandType { get; set; }
        public static int StandSerialNumber { get; set; }

        // Stage 1 K5 Test
        public static StageK5TestReport Stage1K5 { get; set; } = new StageK5TestReport();

        // Stage 2 K5 Test
        public static StageK5TestReport Stage2K5 { get; set; } = new StageK5TestReport();

        // Stage 3 K5 Test
        public static StageK5TestReport Stage3K5 { get; set; } = new StageK5TestReport();

        // Stage 4 K5 Test
        public static StageK5TestReport Stage4K5 { get; set; } = new StageK5TestReport();

        // VCC Test
        public static VCCTestReport VCC { get; set; } = new VCCTestReport();

        // Linux Tests
        public static DutTestReport Dut { get; set; } = new DutTestReport();

    }

    // Классы отчетов для удобной группировки
    public class StageK5TestReport
    {
        public bool ResultK5 { get; set; }
        public int V52Report { get; set; }
        public int V55Report { get; set; }
        public int VOUTReport { get; set; }
        public int V2048Report { get; set; }
        public int V12Report { get; set; }
    }

    public class VCCTestReport
    {
        public int V33Report { get; set; }
        public int V15Report { get; set; }
        public int V11Report { get; set; }
        public int CR2032Report { get; set; }
        public int CpuCR2032Report { get; set; }
    }

    public class DutTestReport
    {
        public int StatusTamperCPU { get; set; } // 2319
        public int StatusTamperLED { get; set; }
        public double SensorTemperature { get; set; }
    }
}