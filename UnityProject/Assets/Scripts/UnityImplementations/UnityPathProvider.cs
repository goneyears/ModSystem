// ModSystem.Unity/UnityImplementations/UnityPathProvider.cs
using UnityEngine;
using System.IO;
using ModSystem.Core;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity路径提供器实现
    /// 提供Unity环境下的各种路径访问
    /// </summary>
    public class UnityPathProvider : IPathProvider
    {
        #region Constants
        private const string MODS_FOLDER = "Mods";
        private const string CONFIG_FOLDER = "ModConfigs";
        private const string TEMP_FOLDER = "ModTemp";
        private const string CACHE_FOLDER = "ModCache";
        #endregion

        #region IPathProvider Implementation

        private readonly string modsPath;
        private readonly string configPath;
        
        public UnityPathProvider(string modsPath = "Mods", string configPath = "ModConfigs")
        {
            this.modsPath = modsPath;
            this.configPath = configPath;
        }

         public string GetModsPath()
        {
            #if UNITY_EDITOR
            return System.IO.Path.Combine(Application.dataPath, modsPath);
            #else
            return System.IO.Path.Combine(Application.persistentDataPath, modsPath);
            #endif
        }
        
        public string GetConfigPath()
        {
            return System.IO.Path.Combine(Application.streamingAssetsPath, configPath);
        }
        
        /// <summary>
        /// 获取临时文件路径
        /// </summary>
        public string GetTempPath()
        {
            string tempPath = Path.Combine(Application.temporaryCachePath, TEMP_FOLDER);
            EnsureDirectoryExists(tempPath);
            return tempPath;
        }
        
        /// <summary>
        /// 获取持久化数据路径
        /// </summary>
        public string GetPersistentDataPath()
        {
            return Application.persistentDataPath;
        }
        #endregion

        #region Extended Path Methods
        /// <summary>
        /// 获取缓存路径
        /// </summary>
        public string GetCachePath()
        {
            string cachePath = Path.Combine(Application.temporaryCachePath, CACHE_FOLDER);
            EnsureDirectoryExists(cachePath);
            return cachePath;
        }
        
        /// <summary>
        /// 获取模组包路径
        /// </summary>
        public string GetModPackagesPath()
        {
            return Path.Combine(Application.streamingAssetsPath, "ModPackages");
        }
        
        /// <summary>
        /// 获取用户模组路径（用于运行时下载的模组）
        /// </summary>
        public string GetUserModsPath()
        {
            string userModsPath = Path.Combine(Application.persistentDataPath, "UserMods");
            EnsureDirectoryExists(userModsPath);
            return userModsPath;
        }
        
        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        public string GetLogsPath()
        {
            string logsPath = Path.Combine(Application.persistentDataPath, "Logs");
            EnsureDirectoryExists(logsPath);
            return logsPath;
        }
        
        /// <summary>
        /// 获取截图路径
        /// </summary>
        public string GetScreenshotsPath()
        {
            string screenshotsPath = Path.Combine(Application.persistentDataPath, "Screenshots");
            EnsureDirectoryExists(screenshotsPath);
            return screenshotsPath;
        }
        #endregion

        #region Platform-Specific Paths
        /// <summary>
        /// 获取平台特定的数据路径
        /// </summary>
        public string GetPlatformDataPath()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), 
                        Application.companyName, Application.productName);
                    
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), 
                        "Library", "Application Support", Application.identifier);
                    
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), 
                        ".config", Application.identifier);
                    
                default:
                    return Application.persistentDataPath;
            }
        }
        
        /// <summary>
        /// 获取安装路径
        /// </summary>
        public string GetInstallationPath()
        {
            return Application.dataPath;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 确保目录存在
        /// </summary>
        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        /// <summary>
        /// 获取相对于StreamingAssets的路径
        /// </summary>
        public string GetStreamingAssetsRelativePath(string fullPath)
        {
            if (fullPath.StartsWith(Application.streamingAssetsPath))
            {
                return fullPath.Substring(Application.streamingAssetsPath.Length + 1);
            }
            return fullPath;
        }
        
        /// <summary>
        /// 清理路径（处理不同平台的路径分隔符）
        /// </summary>
        public string CleanPath(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar)
                      .Replace('/', Path.DirectorySeparatorChar);
        }
        #endregion

        #region Path Validation
        /// <summary>
        /// 验证路径是否安全
        /// </summary>
        public bool IsPathSafe(string path)
        {
            try
            {
                // 获取完整路径
                string fullPath = Path.GetFullPath(path);
                
                // 检查是否在允许的目录内
                return fullPath.StartsWith(Application.streamingAssetsPath) ||
                       fullPath.StartsWith(Application.persistentDataPath) ||
                       fullPath.StartsWith(Application.temporaryCachePath);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 验证文件扩展名
        /// </summary>
        public bool IsValidModFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".modpack" || extension == ".zip";
        }
        #endregion
    }
} 