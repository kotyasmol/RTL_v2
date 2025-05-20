using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTL.Models
{
    public class FlashProgrammingContext
    {
        public string FlashProgramPath { get; set; } // EXE
        public string ProjectFilePath { get; set; }  // MPJ
        public string InstructionPath { get; set; }  // PDF
        public bool IsFirstRun { get; set; }         // Показывать ли инструкцию
        public int FlashDelaySeconds { get; set; } = 180; // Задержка на прошивку
        public bool AutoMode { get; set; }      // Автоматическая или ручная прошивка
    }
}
