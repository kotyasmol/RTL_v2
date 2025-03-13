using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTL.ReportGenerator
{
    public  class ReportService
    {
        private  string _currentReportFile;

        /// <summary>
        /// Устанавливает новый путь к файлу отчёта при старте тестирования.
        /// </summary>
        public  void InitializeNewReportFile(string reportDirectory)
        {
            if (!Directory.Exists(reportDirectory))
            {
                Directory.CreateDirectory(reportDirectory);
            }

            string fileName = $"TestReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            _currentReportFile = Path.Combine(reportDirectory, fileName);
        }

        /// <summary>
        /// Добавляет строку в текущий файл отчёта.
        /// </summary>
        public  void AppendToReport(string content)
        {
            if (string.IsNullOrEmpty(_currentReportFile))
            {
                throw new InvalidOperationException("Файл отчёта не инициализирован. Вызовите InitializeNewReportFile.");
            }

            try
            {
                File.AppendAllText(_currentReportFile, content + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи в файл отчёта: {ex.Message}");
            }
        }

        public void PrependToReport(string content)
        {
            if (string.IsNullOrEmpty(_currentReportFile))
            {
                throw new InvalidOperationException("Файл отчёта не инициализирован. Вызовите InitializeNewReportFile.");
            }

            try
            {
                // Читаем весь файл в память
                string existingContent = File.Exists(_currentReportFile) ? File.ReadAllText(_currentReportFile) : string.Empty;

                // Добавляем новую строку в начало
                string newContent = content + Environment.NewLine + existingContent;

                // Записываем обновлённое содержимое обратно в файл
                File.WriteAllText(_currentReportFile, newContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи в файл отчёта: {ex.Message}");
            }
        }

    }
}
