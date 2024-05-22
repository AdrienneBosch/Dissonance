using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissonance.Settings.Interfaces
{
    public interface ISettingsManager
    {
        void LoadSettings();
        void SaveUserSettings();
    }
}
