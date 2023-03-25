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

        private bool _AntCheat = false;
        public bool AntCheat { get => _AntCheat; set => SetValue(ref _AntCheat, value); }

        private bool _JoinValidation = false;
        public bool JoinValidation { get => _JoinValidation; set => SetValue(ref _JoinValidation, value); }

        private bool _EnableAutoBan = true;
        public bool EnableAutoBan { get => _EnableAutoBan; set => SetValue(ref _EnableAutoBan, value); }

    }
}
