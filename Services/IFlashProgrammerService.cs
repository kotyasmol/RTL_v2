using RTL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTL.Services
{
    public interface IFlashProgrammerService
    {
        Task<bool> StartProgrammingAsync(FlashProgrammingContext context, CancellationToken cancellationToken);
    }
}