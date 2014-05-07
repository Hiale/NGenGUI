using System;
using System.IO;
using System.Linq;
using System.Security.Principal;

namespace Hiale.NgenGui.Helper
{
    public static class Misc
    {
        public static bool CheckAdmin()
        {
            var windowsIdentity = WindowsIdentity.GetCurrent();
            var localAdminGroupSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            var isLocalAdmin = windowsIdentity.Groups.Select(g => (SecurityIdentifier) g.Translate(typeof (SecurityIdentifier))).Any(s => s == localAdminGroupSid);
            return isLocalAdmin;
        }

        public static DateTime GetLinkerTimestamp(string fileName)
        {
            var buffer = new byte[4];
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read)) //Path to any assembly file
            {
                fileStream.Position = 60; //PE Header Offset
                fileStream.Read(buffer, 0, 4);
                fileStream.Position = BitConverter.ToUInt32(buffer, 0); // COFF Header Offset
                fileStream.Position += 8; //skip "PE\0\0" (4 Bytes), Machine Type (2 Byte), Number Of Sections (2 Bytes)
                fileStream.Read(buffer, 0, 4);
            }
            var timeDateStamp = BitConverter.ToInt32(buffer, 0);
            return TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1) + new TimeSpan(timeDateStamp*TimeSpan.TicksPerSecond)); //Local time
        }
    }
}