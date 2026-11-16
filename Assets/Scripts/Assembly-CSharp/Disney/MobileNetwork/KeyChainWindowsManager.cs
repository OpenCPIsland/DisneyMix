using LitJson;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Disney.MobileNetwork
{
    public class KeyChainWindowsManager : KeyChainManager
    {
        private const string APP_DATA_KEY = "mix.AppData";
        private const string APP_DATA_FILE_NAME = "mix.AppData.dat";

        // This is the fallback key found in the Mix SDK
        private const string FALLBACK_CRYPTO_KEY = "4C906C6AAF5C2CB4B581411A91091A8D";

        private Dictionary<string, string> appData = new Dictionary<string, string>();

        #region Native DLL Imports
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private const string DLL_NAME = "KeyChainWindows";
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        private const string DLL_NAME = "libKeyChainLinux";
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX_ARM || UNITY_EDITOR_OSX_ARM
        private const string DLL_NAME = "libKeyChainOSX";
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX_ARM || UNITY_EDITOR_OSX_ARM
        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int _cryptProtectData(string dataIn, ref int dataOutSize, out IntPtr dataOut);

        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int _cryptUnprotectData(byte[] dataIn, int dataInLength, out IntPtr dataOut);

#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX_ARM || UNITY_EDITOR_OSX_ARM
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _keyChainFree(IntPtr ptr);
#endif

        private static void FreeNativePtr(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return;
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            Marshal.FreeCoTaskMem(ptr);
#else
            _keyChainFree(ptr);
#endif
        }
#endif
        #endregion

        protected override void Init()
        {
            LoadFromDisk();
        }

        public override void PutString(string key, string value)
        {
            appData[key] = value;
            SaveToDisk();
        }

        public override string GetString(string key)
        {
            return appData.TryGetValue(key, out string value) ? value : null;
        }

        public override void RemoveString(string key)
        {
            if (appData.Remove(key))
            {
                SaveToDisk();
            }
        }

        #region Internal Logic
        private void LoadFromDisk()
        {
            string encryptedBase64 = null;
            string path = Path.Combine(Application.persistentDataPath, APP_DATA_FILE_NAME);

            if (File.Exists(path)) encryptedBase64 = File.ReadAllText(path);
            if (string.IsNullOrEmpty(encryptedBase64)) encryptedBase64 = PlayerPrefs.GetString(APP_DATA_KEY, null);

            if (string.IsNullOrEmpty(encryptedBase64)) return;

            try
            {
                string json = null;
                byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                IntPtr outPtr = IntPtr.Zero;
                int success = _cryptUnprotectData(encryptedBytes, encryptedBytes.Length, out outPtr);
                if (success != 0 && outPtr != IntPtr.Zero)
                {
                    try
                    {
                        json = Marshal.PtrToStringAnsi(outPtr);
                    }
                    finally
                    {
                        FreeNativePtr(outPtr);
                    }
                }
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX_ARM || UNITY_EDITOR_OSX_ARM
                IntPtr outPtr = IntPtr.Zero;
                int success = _cryptUnprotectData(encryptedBytes, encryptedBytes.Length, out outPtr);
                if (success != 0 && outPtr != IntPtr.Zero)
                {
                    try
                    {
                        json = Marshal.PtrToStringAnsi(outPtr);
                    }
                    finally
                    {
                        FreeNativePtr(outPtr);
                    }
                }

                if (string.IsNullOrEmpty(json))
                {
                    json = InternalDecrypt(encryptedBase64, FALLBACK_CRYPTO_KEY);
                }
#else
                json = InternalDecrypt(encryptedBase64, FALLBACK_CRYPTO_KEY);
#endif

                if (!string.IsNullOrEmpty(json))
                {
                    appData = JsonMapper.ToObject<Dictionary<string, string>>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"KeyChain: Failed to load data: {e.Message}");
            }
        }

        private void SaveToDisk()
        {
            try
            {
                string json = JsonMapper.ToJson(appData);
                string encryptedBase64 = null;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                IntPtr outPtr = IntPtr.Zero;
                int size = 0;
                int success = _cryptProtectData(json, ref size, out outPtr);
                if (success != 0 && outPtr != IntPtr.Zero)
                {
                    try
                    {
                        byte[] managedArray = new byte[size];
                        Marshal.Copy(outPtr, managedArray, 0, size);
                        encryptedBase64 = Convert.ToBase64String(managedArray);
                    }
                    finally
                    {
                        FreeNativePtr(outPtr);
                    }
                }
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX_ARM || UNITY_EDITOR_OSX_ARM
                IntPtr outPtr = IntPtr.Zero;
                int size = 0;
                int success = _cryptProtectData(json, ref size, out outPtr);
                if (success != 0 && outPtr != IntPtr.Zero && size > 0)
                {
                    try
                    {
                        byte[] managedArray = new byte[size];
                        Marshal.Copy(outPtr, managedArray, 0, size);
                        encryptedBase64 = Convert.ToBase64String(managedArray);
                    }
                    finally
                    {
                        FreeNativePtr(outPtr);
                    }
                }
                else
                {
                    encryptedBase64 = InternalEncrypt(json, FALLBACK_CRYPTO_KEY);
                }
#else
                encryptedBase64 = InternalEncrypt(json, FALLBACK_CRYPTO_KEY);
#endif

                if (!string.IsNullOrEmpty(encryptedBase64))
                {
                    PlayerPrefs.SetString(APP_DATA_KEY, encryptedBase64);
                    PlayerPrefs.Save();
                    File.WriteAllText(Path.Combine(Application.persistentDataPath, APP_DATA_FILE_NAME), encryptedBase64);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"KeyChain: Failed to save data: {e.Message}");
            }
        }
        #endregion

        #region Encryption Fallback
        // Used on mobile platforms and when a Linux/macOS native keychain call fails.
        private string InternalEncrypt(string text, string key)
        {
            Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(key, 32);
            byte[] salt = rfc.Salt;
            byte[] keyBytes = rfc.GetBytes(32);
            byte[] ivBytes = rfc.GetBytes(16);

            using (AesManaged aes = new AesManaged { Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
            using (ICryptoTransform transform = aes.CreateEncryptor(keyBytes, ivBytes))
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, transform, CryptoStreamMode.Write))
                using (StreamWriter sw = new StreamWriter(cs)) sw.Write(text);

                byte[] encrypted = ms.ToArray();
                byte[] result = new byte[salt.Length + encrypted.Length];
                Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
                Buffer.BlockCopy(encrypted, 0, result, salt.Length, encrypted.Length);
                return Convert.ToBase64String(result);
            }
        }

        private string InternalDecrypt(string text, string key)
        {
            byte[] combined = Convert.FromBase64String(text);
            byte[] salt = new byte[32];
            byte[] encrypted = new byte[combined.Length - 32];
            Buffer.BlockCopy(combined, 0, salt, 0, 32);
            Buffer.BlockCopy(combined, 32, encrypted, 0, combined.Length - 32);

            Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(key, salt);
            byte[] keyBytes = rfc.GetBytes(32);
            byte[] ivBytes = rfc.GetBytes(16);

            using (AesManaged aes = new AesManaged { Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
            using (ICryptoTransform transform = aes.CreateDecryptor(keyBytes, ivBytes))
            using (MemoryStream ms = new MemoryStream(encrypted))
            using (CryptoStream cs = new CryptoStream(ms, transform, CryptoStreamMode.Read))
            using (StreamReader sr = new StreamReader(cs)) return sr.ReadToEnd();
        }
        #endregion
    }
}