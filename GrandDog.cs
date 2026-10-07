using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Matric_scope
{
    public class GrandDog
    {
        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_OpenDog(uint ulFlag, byte* pszProductName, uint* pDogHandle);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_GetDogInfo(uint DogHandle, byte* pHardwareInfo, uint* pulLen);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_GetProductCurrentNo(uint DogHandle, uint* pulProductCurrentNo);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_VerifyPassword(uint DogHandle, byte bPasswordType, string szPassword, byte* pbDegree);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public static extern uint rc_ChangePassword(uint DogHandle, byte bPasswordType, string szPassword);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_SetKey(uint DogHandle, byte bKeyType, byte* pucIn, uint ulLen);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_EncryptData(uint DogHandle, byte* pucIn, uint ulInLen, byte* pucOut, uint* pulOutLen);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_DecryptData(uint DogHandle, byte* pucIn, uint ulInLen, byte* pucOut, uint* pulOutLen);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_SignData(uint DogHandle, byte* pucIn, uint ulInLen, byte* pucOut, uint* pulOutLen);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_ConvertData(uint DogHandle, byte* pucIn, uint ulInLen, uint* pulResult);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public static extern uint rc_CheckDog(uint DogHandle);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_GetRandom(uint DogHandle, byte* pucOut, uint ulInLen);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public static extern uint rc_CreateDir(uint DogHandle, ushort usDirID, uint ulDirSize);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public static extern uint rc_CreateFile(uint DogHandle, ushort usDirID, ushort usFileID, byte bFiletype, uint ulFileSize);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public static extern uint rc_DeleteDir(uint DogHandle, ushort usDirID);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public static extern uint rc_DeleteFile(uint DogHandle, ushort usDirID, ushort usFileID);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public static extern uint rc_DefragFileSystem(uint DogHandle, ushort usDirID);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_ReadFile(uint DogHandle, ushort usDirID, ushort usFileID, uint ulPos, uint ulLen, byte* pucOut);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_WriteFile(uint DogHandle, ushort usDirID, ushort usFileID, uint ulPos, uint ulLen, byte* pucIn);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public static extern uint rc_VisitLicenseFile(uint DogHandle, ushort usDirID, ushort usFileID, uint ulReserved);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_ExecuteFile(uint DogHandle, ushort usDirID, ushort usFileID, byte* pucIn, uint ulInlen, byte* pucOut, uint* pulOutlen);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_GetUpgradeRequestString(uint DogHandle, byte* pucBuf, uint* pulLen);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_Upgrade(uint DogHandle, byte* pucUpgrade, uint ulLen);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public static extern uint rc_CloseDog(uint DogHandle);

        [DllImport("RCGrandDogW32.dll", CharSet = CharSet.Ansi)]
        public unsafe static extern uint rc_GetLicenseInfo(uint DogHandle, ushort usDirID, ushort usFileID, ushort* pusLimit, uint* pulCount, uint* pulRuntime, ushort* pusBeginYear, byte* pbBeginMonth, byte* pbBeginDay, byte* pbBeginHour, byte* pbBeginMinute, byte* pbBeginSecond, ushort* pusEndYear, byte* pbEndMonth, byte* pbEndDay, byte* pbEndHour, byte* pbEndMinute, byte* pbEndSecond);

        public unsafe uint OpenDog(uint ulFlag, byte* pszProductName, uint* pDogHandle)
        {
            return GrandDog.rc_OpenDog(ulFlag, pszProductName, pDogHandle);
        }

        public uint CloseDog(uint DogHandle)
        {
            return GrandDog.rc_CloseDog(DogHandle);
        }

        public unsafe uint GetDogInfo(uint DogHandle, byte* pHardwareInfo, uint* pulLen)
        {
            return GrandDog.rc_GetDogInfo(DogHandle, pHardwareInfo, pulLen);
        }

        public unsafe uint GetProductCurrentNo(uint DogHandle, uint* pulProductCurrentNo)
        {
            return GrandDog.rc_GetProductCurrentNo(DogHandle, pulProductCurrentNo);
        }

        public unsafe uint VerifyPassword(uint DogHandle, byte bPasswordType, string szPassword, byte* pbDegree)
        {
            return GrandDog.rc_VerifyPassword(DogHandle, bPasswordType, szPassword, pbDegree);
        }

        public uint ChangePassword(uint DogHandle, byte bPasswordType, string szPassword)
        {
            return GrandDog.rc_ChangePassword(DogHandle, bPasswordType, szPassword);
        }

        public unsafe uint SetKey(uint DogHandle, byte bKeyType, byte* pucIn, uint ulLen)
        {
            return GrandDog.rc_SetKey(DogHandle, bKeyType, pucIn, ulLen);
        }

        public unsafe uint EncryptData(uint DogHandle, byte* pucIn, uint ulInLen, byte* pucOut, uint* pulOutLen)
        {
            return GrandDog.rc_EncryptData(DogHandle, pucIn, ulInLen, pucOut, pulOutLen);
        }

        public unsafe uint DecryptData(uint DogHandle, byte* pucIn, uint ulInLen, byte* pucOut, uint* pulOutLen)
        {
            return GrandDog.rc_DecryptData(DogHandle, pucIn, ulInLen, pucOut, pulOutLen);
        }

        public unsafe uint SignData(uint DogHandle, byte* pucIn, uint ulInLen, byte* pucOut, uint* pulOutLen)
        {
            return GrandDog.rc_SignData(DogHandle, pucIn, ulInLen, pucOut, pulOutLen);
        }

        public unsafe uint ConvertData(uint DogHandle, byte* pucIn, uint ulInLen, uint* pulResult)
        {
            return GrandDog.rc_ConvertData(DogHandle, pucIn, ulInLen, pulResult);
        }

        public uint CheckDog(uint DogHandle)
        {
            return GrandDog.rc_CheckDog(DogHandle);
        }

        public unsafe uint GetRandom(uint DogHandle, byte* pucOut, uint ulInLen)
        {
            return GrandDog.rc_GetRandom(DogHandle, pucOut, ulInLen);
        }

        public uint CreateDir(uint DogHandle, ushort usDirID, uint ulDirSize)
        {
            return GrandDog.rc_CreateDir(DogHandle, usDirID, ulDirSize);
        }

        public uint CreateFile(uint DogHandle, ushort usDirID, ushort usFileID, byte bFiletype, uint ulFileSize)
        {
            return GrandDog.rc_CreateFile(DogHandle, usDirID, usFileID, bFiletype, ulFileSize);
        }

        public uint DeleteDir(uint DogHandle, ushort usDirID)
        {
            return GrandDog.rc_DeleteDir(DogHandle, usDirID);
        }

        public uint DeleteFile(uint DogHandle, ushort usDirID, ushort usFileID)
        {
            return GrandDog.rc_DeleteFile(DogHandle, usDirID, usFileID);
        }

        public uint DefragFileSystem(uint DogHandle, ushort usDirID)
        {
            return GrandDog.rc_DefragFileSystem(DogHandle, usDirID);
        }

        public unsafe uint ReadFile(uint DogHandle, ushort usDirID, ushort usFileID, uint ulPos, uint ulLen, byte* pucOut)
        {
            return GrandDog.rc_ReadFile(DogHandle, usDirID, usFileID, ulPos, ulLen, pucOut);
        }

        public unsafe uint WriteFile(uint DogHandle, ushort usDirID, ushort usFileID, uint ulPos, uint ulLen, byte* pucIn)
        {
            return GrandDog.rc_WriteFile(DogHandle, usDirID, usFileID, ulPos, ulLen, pucIn);
        }

        public uint VisitLicenseFile(uint DogHandle, ushort usDirID, ushort usFileID, uint ulReserved)
        {
            return GrandDog.rc_VisitLicenseFile(DogHandle, usDirID, usFileID, ulReserved);
        }

        public unsafe uint ExecuteFile(uint DogHandle, ushort usDirID, ushort usFileID, byte* pucIn, uint ulInlen, byte* pucOut, uint* pulOutlen)
        {
            return GrandDog.rc_ExecuteFile(DogHandle, usDirID, usFileID, pucIn, ulInlen, pucOut, pulOutlen);
        }

        public unsafe uint GetUpgradeRequestString(uint DogHandle, byte* pucBuf, uint* pulLen)
        {
            return GrandDog.rc_GetUpgradeRequestString(DogHandle, pucBuf, pulLen);
        }

        public unsafe uint Upgrade(uint DogHandle, byte* pucUpgrade, uint pulLen)
        {
            return GrandDog.rc_Upgrade(DogHandle, pucUpgrade, pulLen);
        }

        public unsafe uint GetLicenseInfo(uint DogHandle, ushort usDirID, ushort usFileID, ushort* pusLimit, uint* pulCount, uint* pulRuntime, ushort* pusBeginYear, byte* pbBeginMonth, byte* pbBeginDay, byte* pbBeginHour, byte* pbBeginMinute, byte* pbBeginSecond, ushort* pusEndYear, byte* pbEndMonth, byte* pbEndDay, byte* pbEndHour, byte* pbEndMinute, byte* pbEndSecond)
        {
            return GrandDog.rc_GetLicenseInfo(DogHandle, usDirID, usFileID, pusLimit, pulCount, pulRuntime, pusBeginYear, pbBeginMonth, pbBeginDay, pbBeginHour, pbBeginMinute, pbBeginSecond, pusEndYear, pbEndMonth, pbEndDay, pbEndHour, pbEndMinute, pbEndSecond);
        }
    }
}
