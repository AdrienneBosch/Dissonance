using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissonance.AppSettings.Interfaces
{
    public interface IUserSettings
    {
        int Volume { get; set; }
        void Load();
        void Save();
    }
}
