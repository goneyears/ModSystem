using System;
using System.IO;
using Newtonsoft.Json;
using ModSystem.Core.Interfaces;

namespace ModSystem.Core.Configuration
{
    /// <summary>
    /// 简单的配置加载器 - 负责从JSON文件加载配置
    /// </summary>
    public static class ConfigLoader
    {
        /// <summary>
        /// 加载模组配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="modId">模组ID</param>
        /// <param name="configPath">配置文件路径</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>配置对象，如果加载失败返回默认实例</returns>
        public static T LoadConfig<T>(string modId, string configPath, ILogger logger) where T : new()
        {
            var configFile = Path.Combine(configPath, $"{modId}.json");

            try
            {
                if (File.Exists(configFile))
                {
                    var json = File.ReadAllText(configFile);
                    var config = JsonConvert.DeserializeObject<T>(json);

                    if (config != null)
                    {
                        logger?.Log($"Loaded config for {modId} from {configFile}");
                        return config;
                    }
                }
                else
                {
                    logger?.Log($"Config file not found for {modId}, using defaults");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to load config for {modId}: {ex.Message}");
            }

            // 返回默认实例
            return new T();
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public static bool SaveConfig<T>(string modId, string configPath, T config, ILogger logger)
        {
            var configFile = Path.Combine(configPath, $"{modId}.json");

            try
            {
                // 确保目录存在
                Directory.CreateDirectory(configPath);

                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configFile, json);

                logger?.Log($"Saved config for {modId} to {configFile}");
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to save config for {modId}: {ex.Message}");
                return false;
            }
        }
    }
}