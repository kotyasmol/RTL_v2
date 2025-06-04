using RTL.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTL.Services
{
    public interface IMcuProgrammerService
    {
        Task<bool> FlashMcuAsync(string batPath, string binPath, CancellationToken cancellationToken);
    }
}
