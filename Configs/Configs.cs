using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;

namespace AdminLogger.Configs
{
    public class Settings : ViewModel
    {

        private bool _AdminLoggerOwnLog = true;
        public bool AdminLoggerOwnLog { get => _AdminLoggerOwnLog; set => SetValue(ref _AdminLoggerOwnLog, value); }


    }
}
