using System.Net.NetworkInformation;

namespace Crowdin.Net.Infrastructure
{
    internal static class SystemHelpers
    {
        internal static bool IsNetworkConnected()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }


        //static bool IsWiFiConnection()
        //{
        //    var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        //    foreach (NetworkInterface networkInterface in interfaces)
        //    {
        //        if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
        //            networkInterface.OperationalStatus == OperationalStatus.Up)
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}



    }
}