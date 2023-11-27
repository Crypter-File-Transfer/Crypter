using System.Collections.Generic;

namespace Crypter.Core.Settings;

public class CorsSettings
{
    public List<string> AllowedOrigins { get; set; }
    public bool AllowWildcardSubdomains { get; set; }
}
