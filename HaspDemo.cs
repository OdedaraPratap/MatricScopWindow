using Matric_scope;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Matric_scope
{
    public class HaspDemo
    {
        public HaspDemo()
        {
            string[] value = new string[]
            {
                "Success.",
                "Invalid memory address.",
                "Unknown/invalid feature id option.",
                "Memory allocation failed.",
                "Too many open features.",
                "Feature access denied.",
                "Incompatible feature.",
                "HASP not found.",
                "En-/decryption length too short.",
                "Invalid handle.",
                "Invalid file id / memory descriptor.",
                "Driver or support daemon version too old.",
                "Real time support not available.",
                "Generic error from host system call.",
                "Hardware key driver not found.",
                "Unrecognized info format.",
                "Request not supported.",
                "Invalid update object.",
                "Key with requested id was not found.",
                "Update data consistency check failed.",
                "Update not supported by this key.",
                "Update counter mismatch.",
                "Invalid vendor code.",
                "Requested encryption algorithm not supported.",
                "Invalid date / time.",
                "Clock out of power.",
                "Update requested ack., but no area to return it.",
                "Terminal services (remote terminal) detected.",
                "Feature type not implemented.",
                "Unknown algorithm.",
                "Signature check failed.",
                "Feature not found",
                "Trace log not enabled.",
                "Communication error between application and local LM",
                "Vendor code unknown to API library (run apigen to make it known)",
                "Invalid XML spec",
                "Invalid XML scope",
                "Too many keys connected",
                "Too many users",
                "Broken session",
                "Communication error between local and remote LM",
                "The feature is expired",
                "HASP LM version too old",
                "HASP SL secure storage I/O error or USB request error",
                "Update installation not allowed",
                "System time has been tampered",
                "Secure channel communication error",
                "Secure storage contains garbage",
                "Vendor lib cannot be found",
                "Vendor lib cannot be loaded",
                "No feature matching scope found",
                "Virtual machine detected",
                "HASP update incompatible with this hardware; HASP key is locked to other hardware",
                "Login denied because of user restrictions",
                "Update was already installed",
                "Another update must be installed first",
                "Vendor library version too old",
                "Upload error",
                "Invalid XML recipient parameter for hasp_detach",
                "Invalid XML action parameter for hasp_detach",
                "Scope for hasp_detach does not select a unique Product",
                "Invalid Product information"
            };
            this.stringCollection = new StringCollection();
            this.stringCollection.AddRange(value);
            for (int i = this.stringCollection.Count; i < 400; i++)
            {
                this.stringCollection.Insert(i, "");
            }
            this.stringCollection.AddRange(new string[]
            {
                "A required API dynamic library was not found",
                "The found and assigned API dynamic library could not be verified"
            });
            for (int j = this.stringCollection.Count; j < 500; j++)
            {
                this.stringCollection.Insert(j, "");
            }
            this.stringCollection.AddRange(new string[]
            {
                "Calling invalid object.",
                "A parameter is invalid.",
                "Already logged in.",
                "Already logged out."
            });
            for (int k = this.stringCollection.Count; k < 525; k++)
            {
                this.stringCollection.Insert(k, "");
            }
            this.stringCollection.Insert(525, "Unable to excecute/complete the operation.");
            for (int l = this.stringCollection.Count; l < 600; l++)
            {
                this.stringCollection.Insert(l, "");
            }
            this.stringCollection.Insert(600, "No classic memory extension block available.");
            for (int m = this.stringCollection.Count; m < 650; m++)
            {
                this.stringCollection.Insert(m, "");
            }
            this.stringCollection.Insert(650, "Invalid port type.");
            this.stringCollection.Insert(651, "Invalid port.");
            for (int n = this.stringCollection.Count; n < 698; n++)
            {
                this.stringCollection.Insert(n, "");
            }
            this.stringCollection.Insert(698, "Capability is not available.");
            this.stringCollection.Insert(699, "Internal API error.");
            this.Dog = new GrandDog();
        }

        public void DumpBytes(byte[] bytes)
        {
        }

        public string Bytes2String(byte[] bytes)
        {
            string text = "";
            for (int i = 0; i < bytes.Length; i++)
            {
                bool flag = Convert.ToChar(bytes[i]).ToString() != "\0";
                if (flag)
                {
                    string str = text;
                    string str2 = Convert.ToChar(bytes[i]).ToString().Trim();
                    text = str + str2;
                }
            }
            return text;
        }

        public string getids()
        {
            ManagementClass managementClass = new ManagementClass("win32_processor");
            ManagementObjectCollection instances = managementClass.GetInstances();
            string str = "";
            using (ManagementObjectCollection.ManagementObjectEnumerator enumerator = instances.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    ManagementObject managementObject = (ManagementObject)enumerator.Current;
                    str = managementObject.Properties["processorID"].Value.ToString();
                }
            }
            ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
            string str2 = "";
            ManagementObjectSearcher managementObjectSearcher2 = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            foreach (ManagementBaseObject managementBaseObject in managementObjectSearcher2.Get())
            {
                ManagementObject managementObject2 = (ManagementObject)managementBaseObject;
                bool flag = managementObject2.Properties["InterfaceType"].Value.ToString() != "USB";
                if (flag)
                {
                    bool flag2 = Convert.ToInt64(managementObject2.Properties["Size"].Value.ToString()) > 100105249280L;
                    if (flag2)
                    {
                        str2 = managementObject2["SerialNumber"].ToString();
                        break;
                    }
                }
            }
            return str2 + str + "123456789098765432112345678909876543211234567890987654321";
        }

        public unsafe bool LogingDOG()
        {
            char[] array = new char[16];
            string text = "GrandDog";
            uint num;
            char[] productName = new Char[16];
            try
            {
                fixed (uint* pDogHandle = &ulDogHandle)
                {
                    productName = text.ToCharArray(0, text.Length);
                    fixed (byte* pProductName = new byte[16])
                    {
                        for (int i = 0; i < text.Length; i++)
                        {
                            *(pProductName + i) = (byte)(productName[i]);
                        }
                        *(pProductName + text.Length) = 0;

                        num = Dog.OpenDog(OpenDogFlag, pProductName, pDogHandle);
                    }

                }
            }
            finally
            {
                uint* ptr = null;
            }
            bool flag = num == 0U;
            if (flag)
            {
                byte b;
                num = this.Dog.VerifyPassword(this.ulDogHandle, 1, "12345678", &b);
                bool flag2 = num == 0U;
                return flag2;
            }
            return false;
        }


        public unsafe byte[] readDog()
        {
            try
            {
                char[] array = new char[16];
                string text = "GrandDog";
                uint num;
                char[] productName = new Char[16];
                try
                {
                    fixed (uint* pDogHandle = &ulDogHandle)
                    {
                        productName = text.ToCharArray(0, text.Length);
                        fixed (byte* pProductName = new byte[16])
                        {
                            for (int i = 0; i < text.Length; i++)
                            {
                                *(pProductName + i) = (byte)(productName[i]);
                            }
                            *(pProductName + text.Length) = 0;

                            num = Dog.OpenDog(OpenDogFlag, pProductName, pDogHandle);
                        }

                    }
                }
                finally
                {
                    uint* ptr = null;
                }
                bool flag = num == 0U;
                if (flag)
                {
                    byte b;
                    num = this.Dog.VerifyPassword(this.ulDogHandle, 1, "12345678", &b);
                    bool flag2 = num == 0U;
                    if (flag2)
                    {
                        ushort usDirID = 16128;
                        ushort usFileID = 6;
                        uint ulPos = 0U;
                        uint ulLen = 496U;
                        byte[] array3 = new byte[496];
                        try
                        {
                            fixed (byte* ptr3 = &array3[0])
                            {
                                byte* ptr4 = ptr3;
                                num = this.Dog.ReadFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, ptr3);

                                return array3;
                            }
                        }
                        finally
                        {
                            byte* ptr3 = null;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return new byte[496];
        }

        public unsafe int validategrand()
        {
            int result;
            try
            {
                char[] array = new char[16];
                string text = "GrandDog";
                uint num;
                char[] productName = new Char[16];
                try
                {
                    fixed (uint* pDogHandle = &ulDogHandle)
                    {
                        productName = text.ToCharArray(0, text.Length);
                        fixed (byte* pProductName = new byte[16])
                        {
                            for (int i = 0; i < text.Length; i++)
                            {
                                *(pProductName + i) = (byte)(productName[i]);
                            }
                            *(pProductName + text.Length) = 0;

                            num = Dog.OpenDog(OpenDogFlag, pProductName, pDogHandle);
                        }

                    }
                }
                finally
                {
                    uint* ptr = null;
                }
                bool flag = num == 0U;
                if (flag)
                {
                    byte b;
                    num = this.Dog.VerifyPassword(this.ulDogHandle, 1, "12345678", &b);
                    bool flag2 = num == 0U;
                    return 1;
                    if (flag2)
                    {
                        ushort usDirID = 16128;
                        ushort usFileID = 204;
                        uint ulPos = 0U;
                        uint ulLen = 4U;
                        byte[] array3 = new byte[10];
                        try
                        {
                            fixed (byte* ptr3 = &array3[0])
                            {
                                byte* ptr4 = ptr3;
                                num = this.Dog.ReadFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, ptr4);
                                bool flag3 = num == 0U;
                                if (flag3)
                                {
                                    string text2 = this.getids();
                                    bool flag4 = text2.Length < 30;
                                    if (flag4)
                                    {
                                        MessageBox.Show("Invalid Key");
                                    }
                                    char[] array4 = new char[160];
                                    array4 = text2.ToCharArray(0, Math.Min(text2.Length, 159));
                                    bool flag5 = array3[3] < 78 || array3[3] > 80;
                                    if (flag5)
                                    {
                                        array3[1] = 0;
                                        array3[2] = 150;
                                        array3[3] = 78;
                                        num = this.Dog.WriteFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, ptr4);
                                        usFileID = 205;
                                        array3[0] = (byte)array4[3];
                                        array3[1] = (byte)array4[7];
                                        array3[2] = (byte)array4[1];
                                        array3[3] = (byte)array4[9];
                                        num = this.Dog.WriteFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, ptr4);
                                        usFileID = 304;
                                        array3[0] = (byte)array4[24];
                                        array3[1] = (byte)array4[27];
                                        array3[2] = (byte)array4[22];
                                        array3[3] = (byte)array4[28];
                                        num = this.Dog.WriteFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, ptr4);
                                        usFileID = 305;
                                        array3[0] = (byte)array4[12];
                                        array3[1] = (byte)array4[16];
                                        array3[2] = (byte)array4[10];
                                        array3[3] = (byte)array4[18];
                                        num = this.Dog.WriteFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, ptr4);
                                        return 0;
                                    }
                                    bool flag6 = array3[1] < 250;
                                    if (flag6)
                                    {
                                        byte[] array5 = array3;
                                        int num2 = 1;
                                        array5[num2] += 1;
                                    }
                                    else
                                    {
                                        array3[1] = 1;
                                        bool flag7 = array3[2] < 250;
                                        if (flag7)
                                        {
                                            byte[] array6 = array3;
                                            int num3 = 2;
                                            array6[num3] += 1;
                                        }
                                        else
                                        {
                                            byte[] array7 = array3;
                                            int num4 = 3;
                                            array7[num4] += 1;
                                            array3[2] = 1;
                                        }
                                    }
                                    num = this.Dog.WriteFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, ptr4);
                                    int num5 = 0;
                                    usFileID = 401;
                                    num = this.Dog.ReadFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, (byte*)(&num5));
                                    bool flag8 = num == 0U;
                                    if (flag8)
                                    {
                                        this.newserialno = num5;
                                    }
                                    usFileID = 205;
                                    num = this.Dog.ReadFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, ptr4);
                                    bool flag9 = num == 0U;
                                    if (flag9)
                                    {
                                        bool flag10 = (char)array3[0] == array4[3] && (char)array3[1] == array4[7] && (char)array3[2] == array4[1] && (char)array3[3] == array4[9];
                                        if (flag10)
                                        {
                                            usFileID = 305;
                                            num = this.Dog.ReadFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, ptr4);
                                            bool flag11 = num == 0U;
                                            if (flag11)
                                            {
                                                bool flag12 = (char)array3[0] == array4[12] && (char)array3[1] == array4[16] && (char)array3[2] == array4[10] && (char)array3[3] == array4[18];
                                                if (flag12)
                                                {
                                                    usFileID = 304;
                                                    num = this.Dog.ReadFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, ptr4);
                                                    bool flag13 = num == 0U;
                                                    if (flag13)
                                                    {
                                                        bool flag14 = (char)array3[0] == array4[24] && (char)array3[1] == array4[27] && (char)array3[2] == array4[22] && (char)array3[3] == array4[28];
                                                        if (flag14)
                                                        {
                                                            bool flag15 = false;
                                                            usFileID = 312;
                                                            num = this.Dog.ReadFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, ptr4);
                                                            bool flag16 = num == 0U;
                                                            if (flag16)
                                                            {
                                                                flag15 = true;
                                                            }
                                                            usFileID = 311;
                                                            num = this.Dog.ReadFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, ptr4);
                                                            bool flag17 = num == 0U;
                                                            if (flag17)
                                                            {
                                                                return 3000 + (int)array3[0];
                                                            }
                                                            usFileID = 100;
                                                            ulLen = 9U;
                                                            num = this.Dog.ReadFile(this.ulDogHandle, usDirID, usFileID, ulPos, ulLen, ptr4);
                                                            bool flag18 = num == 0U;
                                                            if (flag18)
                                                            {
                                                                bool flag19 = flag15;
                                                                if (flag19)
                                                                {
                                                                    return 1000 + (int)array3[0] + (int)array3[1] + (int)array3[2];
                                                                }
                                                                return (int)(1 + array3[0] + array3[1] + array3[2]);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            byte* ptr3 = null;
                        }
                    }
                }
                result = 0;
            }
            catch (Exception)
            {
                result = 0;
            }
            return result;
        }

        public int AutoValidatePMLC()
        {
            return this.validategrand();
        }

        public int GetStatus()
        {
            return this.AutoValidatePMLC();
        }

        public bool lckflg = true;

        public string scope = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?> <haspscope/> ";

        public const string localScope = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?> <haspscope>    <license_manager hostname =\"localhost\" /> </haspscope>";

        public const string defaultScope = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?> <haspscope/> ";

        public StringCollection stringCollection;

        public int newserialno = 0;

        public GrandDog Dog;

        public uint ulDogHandle;

        public uint OpenDogFlag = 1;
    }
}
