using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace Starsector_Mod_Manager
{
    class UpdateAgent
    {
        private static readonly HttpClient UpdateClient;


        // get remote version file, compare to local. Return true if remote has higher version
        public bool checkForUpdate(ModDataRow modData)
        {
            return false;
        }


        // function: get remote mod version file
        // function: compare remote and local versions
        // function: download remote version
        // function: unpack archive

    }
}
