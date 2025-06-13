using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ModSystem.Core
{
    /// <summary>
    /// 平台无关的模组加载器
    /// 负责加载模组文件和程序集
    /// </summary>
    public class ModLoader
    {
        private readonly ILogger logger;
        private readonly IPathProvider pathProvider;
        private readonly SecurityManager securityManager;
        private readonly Dictionary<string, LoadedMod> loadedMods;
        // 新增字段
        private readonly Dictionary<string, int> loadVersions;
        private readonly Dictionary<string, string> dllHashes;

        /// <summary>
        /// 创建模组加载器
        /// </summary>
        public ModLoader(ILogger logger, IPathProvider pathProvider, SecurityManager securityManager = null)
        {
            this.logger = logger;
            this.pathProvider = pathProvider;
            this.securityManager = securityManager;
            this.loadedMods = new Dictionary<string, LoadedMod>();
            this.loadVersions = new Dictionary<string, int>();
            this.dllHashes = new Dictionary<string, string>();
        }
        
        /// <summary>
        /// 异步加载模组
        /// </summary>
        public async Task<LoadedMod> LoadModAsync(string modDirectory)
        {
            try
            {
                // 1. 加载清单文件
                var manifestPath = Path.Combine(modDirectory, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    throw new FileNotFoundException("Manifest file not found");
                }
                
                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonConvert.DeserializeObject<ModManifest>(manifestJson);
                
                // 2. 验证安全性
                if (securityManager != null && !securityManager.ValidateMod(modDirectory))
                {
                    throw new SecurityException("Mod failed security validation");
                }
                
                // 3. 加载程序集
                Assembly assembly = null;
                var dllPath = Path.Combine(modDirectory, "Assemblies", $"{manifest.id}.dll");
                if (File.Exists(dllPath))
                {
                    assembly = Assembly.LoadFrom(dllPath);
                }
                
                // 4. 加载资源
                var resources = await LoadResourcesAsync(modDirectory, manifest);
                
                // 5. 创建模组实例
                var loadedMod = new LoadedMod
                {
                    Manifest = manifest,
                    Assembly = assembly,
                    Resources = resources,
                    RootPath = modDirectory
                };
                
                // 6. 实例化主模组行为类
                if (!string.IsNullOrEmpty(manifest.main_class) && assembly != null)
                {
                    var mainType = assembly.GetType(manifest.main_class);
                    if (mainType != null && typeof(IModBehaviour).IsAssignableFrom(mainType))
                    {
                        var behaviour = Activator.CreateInstance(mainType) as IModBehaviour;
                        loadedMod.Behaviours.Add(behaviour);
                    }
                }
                
                // 7. 加载额外的行为类
                if (manifest.behaviours != null && assembly != null)
                {
                    foreach (var behaviourClass in manifest.behaviours)
                    {
                        var behaviourType = assembly.GetType(behaviourClass);
                        if (behaviourType != null && typeof(IModBehaviour).IsAssignableFrom(behaviourType))
                        {
                            var behaviour = Activator.CreateInstance(behaviourType) as IModBehaviour;
                            loadedMod.Behaviours.Add(behaviour);
                        }
                    }
                }
                
                loadedMods[manifest.id] = loadedMod;
                logger.Log($"Mod loaded: {manifest.name} v{manifest.version}");
                
                return loadedMod;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load mod from {modDirectory}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 加载模组资源
        /// </summary>
        private async Task<ModResources> LoadResourcesAsync(string modDirectory, ModManifest manifest)
        {
            var resources = new ModResources();
            
            // 加载对象定义
            var objectsDir = Path.Combine(modDirectory, "Objects");
            if (Directory.Exists(objectsDir))
            {
                foreach (var objectFile in Directory.GetFiles(objectsDir, "*.json"))
                {
                    try
                    {
                        var objectJson = await File.ReadAllTextAsync(objectFile);
                        var objectDef = JsonConvert.DeserializeObject<ObjectDefinition>(objectJson);
                        resources.ObjectDefinitions[Path.GetFileName(objectFile)] = objectDef;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to load object definition {objectFile}: {ex.Message}");
                    }
                }
            }
            
            // 加载配置文件
            var configDir = Path.Combine(modDirectory, "Config");
            if (Directory.Exists(configDir))
            {
                foreach (var configFile in Directory.GetFiles(configDir, "*.json"))
                {
                    try
                    {
                        var configData = await File.ReadAllTextAsync(configFile);
                        resources.Configs[Path.GetFileName(configFile)] = configData;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to load config {configFile}: {ex.Message}");
                    }
                }
            }
            
            // 记录资源路径
            resources.ModelPaths = GetResourcePaths(modDirectory, "Models", "*.gltf", "*.glb");
            resources.TexturePaths = GetResourcePaths(modDirectory, "Resources/Textures", "*.png", "*.jpg");
            resources.AudioPaths = GetResourcePaths(modDirectory, "Resources/Audio", "*.wav", "*.mp3");
            
            return resources;
        }
        
        /// <summary>
        /// 获取资源路径
        /// </summary>
        private Dictionary<string, string> GetResourcePaths(string baseDir, string subDir, params string[] patterns)
        {
            var paths = new Dictionary<string, string>();
            var dir = Path.Combine(baseDir, subDir);
            
            if (Directory.Exists(dir))
            {
                foreach (var pattern in patterns)
                {
                    foreach (var file in Directory.GetFiles(dir, pattern))
                    {
                        paths[Path.GetFileName(file)] = file;
                    }
                }
            }
            
            return paths;
        }
        
        /// <summary>
        /// 卸载模组
        /// </summary>
        public void UnloadMod(string modId)
        {
            if (loadedMods.TryGetValue(modId, out var mod))
            {
                // 销毁所有行为
                foreach (var behaviour in mod.Behaviours)
                {
                    try
                    {
                        behaviour.OnDestroy();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error destroying behaviour: {ex.Message}");
                    }
                }
                
                // 清理临时文件
                if (mod.IsTemporary && Directory.Exists(mod.RootPath))
                {
                    try
                    {
                        Directory.Delete(mod.RootPath, true);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to delete temporary files: {ex.Message}");
                    }
                }
                
                loadedMods.Remove(modId);
                logger.Log($"Mod {modId} unloaded");
            }
        }
        
        /// <summary>
        /// 获取已加载的模组
        /// </summary>
        public LoadedMod GetLoadedMod(string modId)
        {
            return loadedMods.TryGetValue(modId, out var mod) ? mod : null;
        }


        /// <summary>
        /// 支持热重载的加载方法
        /// </summary>
        public async Task<LoadedMod> LoadModWithHotReloadAsync(string modDirectory)
        {
            try
            {
                // 1. 加载清单文件
                var manifestPath = Path.Combine(modDirectory, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    throw new FileNotFoundException("Manifest file not found");
                }

                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonConvert.DeserializeObject<ModManifest>(manifestJson);

                // 2. 验证安全性
                if (securityManager != null && !securityManager.ValidateMod(modDirectory))
                {
                    throw new SecurityException("Mod failed security validation");
                }

                // 3. 加载程序集（支持热重载）
                Assembly assembly = null;
                var dllPath = Path.Combine(modDirectory, $"{manifest.id}.dll");

                // 也尝试标准路径
                if (!File.Exists(dllPath))
                {
                    dllPath = Path.Combine(modDirectory, "Assemblies", $"{manifest.id}.dll");
                }

                if (File.Exists(dllPath))
                {
                    var dllBytes = await File.ReadAllBytesAsync(dllPath);

                    // 计算DLL哈希
                    var currentHash = ComputeHash(dllBytes);
                    bool hasChanged = !dllHashes.ContainsKey(manifest.id) || dllHashes[manifest.id] != currentHash;

                    if (hasChanged)
                    {
                        logger.Log($"[ModLoader] DLL changed for {manifest.id}, loading new version");
                        dllHashes[manifest.id] = currentHash;
                    }

                    // 尝试加载PDB文件
                    var pdbPath = Path.ChangeExtension(dllPath, ".pdb");
                    byte[] pdbBytes = null;
                    if (File.Exists(pdbPath))
                    {
                        pdbBytes = await File.ReadAllBytesAsync(pdbPath);
                    }

                    // 使用byte[]加载以支持热重载
                    if (pdbBytes != null)
                    {
                        assembly = Assembly.Load(dllBytes, pdbBytes);
                    }
                    else
                    {
                        assembly = Assembly.Load(dllBytes);
                    }

                    // 更新加载版本
                    if (!loadVersions.ContainsKey(manifest.id))
                        loadVersions[manifest.id] = 0;
                    loadVersions[manifest.id]++;

                    logger.Log($"[ModLoader] Loaded {manifest.id} v{manifest.version} (Load #{loadVersions[manifest.id]})");
                }

                // 4. 加载资源
                var resources = await LoadResourcesAsync(modDirectory, manifest);

                // 5. 创建模组实例
                var loadedMod = new LoadedMod
                {
                    Manifest = manifest,
                    Assembly = assembly,
                    Resources = resources,
                    RootPath = modDirectory,
                    LoadVersion = loadVersions.ContainsKey(manifest.id) ? loadVersions[manifest.id] : 0
                };

                // 6. 实例化模组行为（支持IMod和IModBehaviour）
                if (!string.IsNullOrEmpty(manifest.main_class) && assembly != null)
                {
                    var mainType = assembly.GetType(manifest.main_class);

                    if (mainType == null)
                    {
                        // 尝试不同的命名空间组合
                        var possibleNames = new[]
                        {
                            manifest.main_class,
                            $"ExampleMods.{manifest.main_class}",
                            $"ModSystem.{manifest.main_class}",
                            manifest.main_class.Contains('.') ? manifest.main_class.Split('.').Last() : null
                        }.Where(n => n != null).Distinct();

                        foreach (var name in possibleNames)
                        {
                            mainType = assembly.GetType(name);
                            if (mainType != null)
                            {
                                logger.Log($"[ModLoader] Found type with alternate name: {name}");
                                break;
                            }
                        }
                    }

                    if (mainType != null)
                    {
                        // 支持IMod接口（新的简化接口）
                        if (typeof(IMod).IsAssignableFrom(mainType))
                        {
                            var mod = Activator.CreateInstance(mainType) as IMod;
                            loadedMod.ModInstance = mod;
                            logger.Log($"[ModLoader] Created IMod instance: {mainType.FullName}");
                        }
                        // 支持IModBehaviour接口（原有接口）
                        else if (typeof(IModBehaviour).IsAssignableFrom(mainType))
                        {
                            var behaviour = Activator.CreateInstance(mainType) as IModBehaviour;
                            loadedMod.Behaviours.Add(behaviour);
                            logger.Log($"[ModLoader] Created IModBehaviour instance: {mainType.FullName}");
                        }
                        else
                        {
                            logger.LogError($"[ModLoader] Type {mainType.FullName} does not implement IMod or IModBehaviour");
                        }
                    }
                    else
                    {
                        logger.LogError($"[ModLoader] Main class not found: {manifest.main_class}");
                    }
                }

                // 7. 加载额外的行为类
                if (manifest.behaviours != null && assembly != null)
                {
                    foreach (var behaviourClass in manifest.behaviours)
                    {
                        var behaviourType = assembly.GetType(behaviourClass);
                        if (behaviourType != null && typeof(IModBehaviour).IsAssignableFrom(behaviourType))
                        {
                            var behaviour = Activator.CreateInstance(behaviourType) as IModBehaviour;
                            loadedMod.Behaviours.Add(behaviour);
                        }
                    }
                }

                loadedMods[manifest.id] = loadedMod;
                logger.Log($"Mod loaded: {manifest.name} v{manifest.version}");

                return loadedMod;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load mod from {modDirectory}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 计算文件哈希
        /// </summary>
        private string ComputeHash(byte[] data)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

    }

} 