using SingBoxLib.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyCollector.Models;
internal class ProfileInfo
{
    public ProfileItem? Profile { get; set; }
    public int? Delay { get; set; }
    public string? Country { get; set; }
}
