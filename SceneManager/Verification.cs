using System.Management;
using System.Text;
using Rage;

namespace SceneManager
{
    class Verification
    {
        // https://www.codingame.com/playgrounds/11117/simple-encryption-using-c-and-xor-technique
        // Get hardware ID from user
        // encrypt hardware ID
        // show encrypted ID in log
        // decrypt ID for hardware ID
        // Give user their hardware ID for ini
        // Pass hardware ID through encryption for a match
        public static string GetID()
        {
            // Get processor ID
            ManagementObjectCollection mbsList = null;
            ManagementObjectSearcher mbs = new ManagementObjectSearcher("Select * From Win32_processor");
            mbsList = mbs.Get();
            string processorID = "";
            foreach (ManagementObject mo in mbsList)
            {
                processorID = mo["ProcessorID"].ToString();
            }

            // Get HD ID
            /*ManagementObject dsk = new ManagementObject(@"win32_logicaldisk.deviceid=""c:""");
            dsk.Get();
            string hdID = dsk["VolumeSerialNumber"].ToString();*/

            // Get MoBo ID
            ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            ManagementObjectCollection moc = mos.Get();
            string motherBoardID = "";
            foreach (ManagementObject mo in moc)
            {
                motherBoardID = (string)mo["SerialNumber"];
            }

            string uniqueID = processorID + motherBoardID;
            string encrypted = passThrough(uniqueID);
            //Game.LogTrivial($"Processor ID: {processorID}");
            //Game.LogTrivial($"HD ID: {hdID}");
            //Game.LogTrivial($"Motherboard ID: {motherBoardID}");
            //Game.LogTrivial($"{uniqueID}");
            Game.LogTrivial($"{encrypted}"); // Get this value from user's log as their key.  When they put it in the .ini, it will go back through passThrough and match with their uniqueID
            //Game.LogTrivial($"Decrypted: {passThrough(encrypted)}");
            return encrypted;
        }

        public static string passThrough(string id)
        {
            StringBuilder szInputStringBuild = new StringBuilder(id);
            StringBuilder szOutStringBuild = new StringBuilder(id.Length);
            char Textch;
            for (int iCount = 0; iCount < id.Length; iCount++)
            {
                Textch = szInputStringBuild[iCount];
                Textch = (char)(Textch ^ 1);
                szOutStringBuild.Append(Textch);
            }

            return szOutStringBuild.ToString();
        }
    }
}
