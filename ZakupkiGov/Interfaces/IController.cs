
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZakupkiGov.Enums;

namespace StoreParser.Interfaces
{
    internal interface IController
    {
        void Execute();
        Task ExecuteAsync();
        void Stop();
        ProcessStates GetState();
        int GetTotal();
    }
}
