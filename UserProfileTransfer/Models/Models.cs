using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserProfileTransfer.Models
{

    public class UserSecurityProfileList : List<UserSecurityProfile>
    {

    }
    
    public class UserSecurityProfile
    {
        public List<string> emails { get; set; }

        public string businessUnit { get; set; }

        public List<string> roles { get; set; }

        public List<string> teams { get; set; }

        public List<string> queues { get; set; }
    }
}
