
using JetBrains.Annotations;

namespace Crowdin.Net.Models
{
    [PublicAPI]
    public enum NetworkPolicy
    {
        All,
        OnlyWiFi,
        OnlyCellular,
        Forbidden
    }
}