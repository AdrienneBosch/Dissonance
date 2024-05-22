using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissonance.Settings.Interfaces
{
    public interface IAppSettings
    {
        int Volume { get; set; }
        void Load();
        void Save();
    }
}
