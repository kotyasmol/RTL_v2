using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTL.Services
{
    public interface ITestTimeoutService
    {
        Task<bool> RunWithTimeoutAsync(
            Func<CancellationToken, Task<bool>> testFunc,
            string testName,
            TimeSpan timeout,
            CancellationToken externalToken);
    }

}
