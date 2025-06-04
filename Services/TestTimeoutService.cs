using RTL.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTL.Services
{
    public class TestTimeoutService : ITestTimeoutService
    {
        private readonly Loggers _logger;

        public TestTimeoutService(Loggers logger)
        {
            _logger = logger;
        }

        public async Task<bool> RunWithTimeoutAsync(
            Func<CancellationToken, Task<bool>> testFunc,
            string testName,
            TimeSpan timeout,
            CancellationToken externalToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, externalToken);

            try
            {
                return await testFunc(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                _logger.LogToUser($"{testName} не завершён за {timeout.TotalSeconds} секунд (таймаут).", Loggers.LogLevel.Error);
                return false;
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser($"{testName} был отменён пользователем.", Loggers.LogLevel.Warning);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время выполнения {testName}: {ex.Message}", Loggers.LogLevel.Error);
                return false;
            }
        }
    }

}
