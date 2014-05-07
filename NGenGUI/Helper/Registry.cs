using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Hiale.NgenGui.Helper
{
    public class Registry
    {
        #region RegSAM enum

        public enum RegSAM
        {
            QueryValue = 0x0001,
            SetValue = 0x0002,
            CreateSubKey = 0x0004,
            EnumerateSubKeys = 0x0008,
            Notify = 0x0010,
            CreateLink = 0x0020,
            Wow6432Key = 0x0200,
            Wow6464Key = 0x0100,
            Wow64Res = 0x0300,
            Read = 0x00020019,
            Write = 0x00020006,
            Execute = 0x00020019,
            AllAccess = 0x000f003f
        }

        #endregion

        #region Nested type: RegHive

        public static class RegHive
        {
            public static UIntPtr HKEY_LOCAL_MACHINE = new UIntPtr(0x80000002u);
            public static UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001u);
        }

        #endregion

        #region Nested type: RegistryWow6432

        private const string Advapi32 = "Advapi32.dll";

        public static class RegistryWow6432
        {
            [DllImport(Advapi32)]
            private static extern uint RegOpenKeyEx(UIntPtr hKey, string lpSubKey, uint ulOptions, int samDesired, out int phkResult);

            [DllImport(Advapi32)]
            private static extern uint RegCloseKey(int hKey);

            [DllImport(Advapi32, EntryPoint = "RegQueryValueEx")]
            public static extern int RegQueryValueEx(int hKey, string lpValueName, int lpReserved, ref uint lpType, StringBuilder lpData, ref uint lpcbData);

            public static string GetRegKey64(UIntPtr hive, String key, String property)
            {
                return GetRegKey(hive, key, RegSAM.Wow6464Key, property);
            }

            public static string GetRegKey32(UIntPtr hive, String key, String proprty)
            {
                return GetRegKey(hive, key, RegSAM.Wow6432Key, proprty);
            }

            public static string GetRegKey(UIntPtr hive, String key, RegSAM in32Or64Key, String property)
            {
                var hkey = 0;
                try
                {
                    var lResult = RegOpenKeyEx(hive, key, 0, (int) RegSAM.QueryValue | (int) in32Or64Key, out hkey);
                    if (0 != lResult)
                        return null;
                    uint lpType = 0;
                    uint lpcbData = 1024;
                    var buffer = new StringBuilder(1024);
                    RegQueryValueEx(hkey, property, 0, ref lpType, buffer, ref lpcbData);
                    return buffer.ToString();
                }
                finally
                {
                    if (0 != hkey)
                        RegCloseKey(hkey);
                }
            }
        }

        #endregion
    }
}