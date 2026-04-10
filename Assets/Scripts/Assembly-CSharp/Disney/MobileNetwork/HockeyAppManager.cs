using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mix.Assets;
using UnityEngine;

namespace Disney.MobileNetwork
{
    public class HockeyAppManager : MonoBehaviour, IInitializable
    {
        // Removed HOCKEYAPP_BASEURL, HEADER_KEY, and HOCKEYAPP_CRASHESPATH as they are no longer used for sending
        protected const int MAX_CHARS = 199800;
        protected const string LOG_FILE_DIR = "/logs/";

        public string appID = "your-hockey-app-id";
        public string packageID = "your-package-identifier";
        public bool exceptionLogging;
        public bool updateManager;
        public Hashtable loggedExceptions = new Hashtable();

        public virtual void Init()
        {
            base.gameObject.name = "HockeyApp";
            UnityEngine.Object.DontDestroyOnLoad(base.gameObject);

            string text = Application.persistentDataPath + LOG_FILE_DIR;
            try
            {
                if (!Directory.Exists(text))
                {
                    Directory.CreateDirectory(text);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error " + ex.Message + " creating logs dir : " + text);
            }

            // Removed the SendLogs coroutine trigger logic here

            SubscribeToCallbacks();
        }

        public void OnEnable()
        {
            SubscribeToCallbacks();
        }

        public void OnDisable()
        {
            UnsubscribeFromCallbacks();
        }

        private void OnDestroy()
        {
            UnsubscribeFromCallbacks();
        }

        public void SubscribeToCallbacks()
        {
            if (exceptionLogging)
            {
                AppDomain.CurrentDomain.UnhandledException += OnHandleUnresolvedException;
                Application.logMessageReceived += OnHandleLogCallback;
            }
        }

        protected void UnsubscribeFromCallbacks()
        {
            AppDomain.CurrentDomain.UnhandledException -= OnHandleUnresolvedException;
            Application.logMessageReceived -= OnHandleLogCallback;
        }

        public virtual void ForceCrash()
        {
        }

        protected virtual void HandleException(string logString, string stackTrace)
        {
            WriteLogToDisk(logString, stackTrace);
        }

        public void OnHandleLogCallback(string logString, string stackTrace, LogType type)
        {
            if ((type == LogType.Assert || type == LogType.Exception) && exceptionLogging)
            {
                HandleException(logString, stackTrace);
            }
        }

        public void OnHandleUnresolvedException(object sender, UnhandledExceptionEventArgs args)
        {
            if (args != null && args.ExceptionObject != null && args.ExceptionObject.GetType() == typeof(Exception))
            {
                Exception ex = (Exception)args.ExceptionObject;
                HandleException(ex.Source + " - " + ex.Message, ex.StackTrace);
            }
        }

        public void OnHandleUnresolvedException(string logString, string stackTrace, LogType type)
        {
            HandleException(logString, stackTrace);
        }

        protected virtual List<string> GetLogHeaders()
        {
            return new List<string>();
        }

        protected virtual List<string> GetLogFiles()
        {
            List<string> list = new List<string>();
            string path = Application.persistentDataPath + LOG_FILE_DIR;
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                if (directoryInfo.Exists)
                {
                    FileInfo[] files = directoryInfo.GetFiles();
                    foreach (FileInfo fileInfo in files)
                    {
                        if (fileInfo.Extension == ".log")
                        {
                            list.Add(fileInfo.FullName);
                        }
                        else
                        {
                            File.Delete(fileInfo.FullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Debug.isDebugBuild)
                {
                    Debug.Log("Failed to retrieve log files: " + ex);
                }
            }
            return list;
        }

        protected void WriteLogToDisk(string logString, string stackTrace)
        {
            string shaString = AssetManager.GetShaString(logString + stackTrace);
            if (loggedExceptions.ContainsKey(shaString))
            {
                return;
            }
            loggedExceptions.Add(shaString, true);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss_fff");
            StringBuilder formattedLog = new StringBuilder();
            formattedLog.Append("\n").Append(logString.Replace("\n", " ")).Append("\n");

            string[] stackLines = stackTrace.Split('\n');
            foreach (string line in stackLines)
            {
                if (line.Length > 0)
                {
                    formattedLog.Append("  at ").Append(line).Append("\n");
                }
            }

            List<string> logHeaders = GetLogHeaders();
            string fileName = "LogFile_" + timestamp + ".log";
            string filePath = Path.Combine(Application.persistentDataPath + LOG_FILE_DIR, fileName);

            try
            {
                using (StreamWriter streamWriter = new StreamWriter(filePath, true))
                {
                    foreach (string header in logHeaders)
                    {
                        streamWriter.WriteLine(header);
                    }
                    streamWriter.WriteLine(formattedLog.ToString());
                }

                // New log notification for Unity 6 Console
                Debug.Log($"<b>[HockeyApp]</b> Exception logged to disk: <color=yellow>{fileName}</color>\nPath: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"<b>[HockeyApp]</b> Failed to write log to disk: {ex.Message}");
            }
        }
    }
}