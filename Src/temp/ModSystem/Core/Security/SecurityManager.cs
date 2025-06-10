using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using System.Linq;

namespace ModSystem.Core
{
    /// <summary>
    /// 安全管理器
    /// 负责模组的安全验证和权限控制
    /// </summary>
    public class SecurityManager
    {
        private readonly SecurityConfig config;
        private readonly ILogger logger;
        private readonly HashSet<string> whitelistedPaths;
        private readonly HashSet<string> blacklistedAPIs;
        
        /// <summary>
        /// 创建安全管理器
        /// </summary>
        public SecurityManager(SecurityConfig config, ILogger logger)
        {
            this.config = config;
            this.logger = logger;
            this.whitelistedPaths = new HashSet<string>();
            this.blacklistedAPIs = InitializeBlacklistedAPIs();
        }
        
        /// <summary>
        /// 验证模组
        /// </summary>
        public bool ValidateMod(string modPath)
        {
            try
            {
                // 1. 检查路径安全性
                if (!IsPathSafe(modPath))
                {
                    logger.LogError($"Mod path is not safe: {modPath}");
                    return false;
                }
                
                // 2. 验证数字签名（如果启用）
                if (config.RequireSignedMods)
                {
                    if (!VerifySignature(modPath))
                    {
                        logger.LogError($"Mod signature verification failed: {modPath}");
                        return false;
                    }
                }
                
                // 3. 扫描恶意代码
                if (!ScanForMaliciousCode(modPath))
                {
                    logger.LogError($"Mod contains suspicious code: {modPath}");
                    return false;
                }
                
                // 4. 验证权限
                if (!ValidatePermissions(modPath))
                {
                    logger.LogError($"Mod requests unauthorized permissions: {modPath}");
                    return false;
                }
                
                logger.Log($"Mod validation passed: {modPath}");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Security validation error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查路径安全性
        /// </summary>
        private bool IsPathSafe(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                var allowedPaths = config.AllowedModPaths ?? new List<string> { config.ModDirectory };
                
                // 检查是否在允许的路径内
                bool isInAllowedPath = false;
                foreach (var allowedPath in allowedPaths)
                {
                    var fullAllowedPath = Path.GetFullPath(allowedPath);
                    if (fullPath.StartsWith(fullAllowedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        isInAllowedPath = true;
                        break;
                    }
                }
                
                if (!isInAllowedPath)
                {
                    logger.LogError($"Path {fullPath} is not in allowed directories");
                    return false;
                }
                
                // 检查路径遍历攻击
                if (path.Contains("..") || path.Contains("~"))
                {
                    logger.LogError("Path contains traversal characters");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Path validation error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 验证数字签名
        /// </summary>
        private bool VerifySignature(string modPath)
        {
            var signaturePath = Path.Combine(modPath, "signature.sig");
            if (!File.Exists(signaturePath))
            {
                logger.LogWarning($"Signature file not found: {signaturePath}");
                return false;
            }
            
            try
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    // 加载公钥
                    if (!File.Exists(config.PublicKeyPath))
                    {
                        logger.LogError("Public key file not found");
                        return false;
                    }
                    
                    var publicKey = File.ReadAllText(config.PublicKeyPath);
                    rsa.FromXmlString(publicKey);
                    
                    // 计算清单文件哈希
                    var manifestPath = Path.Combine(modPath, "manifest.json");
                    if (!File.Exists(manifestPath))
                    {
                        logger.LogError("Manifest file not found for signature verification");
                        return false;
                    }
                    
                    var manifestData = File.ReadAllBytes(manifestPath);
                    using (var sha256 = SHA256.Create())
                    {
                        var hash = sha256.ComputeHash(manifestData);
                        var signature = File.ReadAllBytes(signaturePath);
                        
                        return rsa.VerifyHash(hash, CryptoConfig.MapNameToOID("SHA256"), signature);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Signature verification error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 扫描恶意代码
        /// </summary>
        private bool ScanForMaliciousCode(string modPath)
        {
            var dllFiles = Directory.GetFiles(modPath, "*.dll", SearchOption.AllDirectories);
            
            foreach (var dll in dllFiles)
            {
                if (!ScanAssembly(dll))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 扫描程序集
        /// </summary>
        private bool ScanAssembly(string assemblyPath)
        {
            try
            {
                // 使用ReflectionOnlyLoad避免执行代码
                var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
                var assembly = Assembly.ReflectionOnlyLoad(assemblyName.FullName);
                
                foreach (var type in assembly.GetTypes())
                {
                    // 检查危险的基类
                    if (IsDangerousType(type))
                    {
                        logger.LogError($"Dangerous type detected: {type.FullName}");
                        return false;
                    }
                    
                    // 检查方法调用
                    foreach (var method in type.GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic | 
                        BindingFlags.Instance | BindingFlags.Static))
                    {
                        if (IsDangerousMethod(method))
                        {
                            logger.LogError($"Dangerous method detected: {method.Name} in {type.FullName}");
                            return false;
                        }
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Assembly scan error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查是否为危险类型
        /// </summary>
        private bool IsDangerousType(Type type)
        {
            var dangerousTypes = new[]
            {
                "System.Diagnostics.Process",
                "System.IO.FileSystemWatcher",
                "System.Net.WebClient",
                "System.Net.Http.HttpClient",
                "Microsoft.Win32.Registry"
            };
            
            return dangerousTypes.Any(dt => 
                type.FullName == dt || 
                (type.BaseType != null && type.BaseType.FullName == dt));
        }
        
        /// <summary>
        /// 检查是否为危险方法
        /// </summary>
        private bool IsDangerousMethod(MethodInfo method)
        {
            var dangerousPatterns = new[]
            {
                "Process.Start",
                "File.Delete",
                "Directory.Delete",
                "Registry.",
                "Assembly.Load",
                "AppDomain.CreateDomain",
                "Marshal.GetDelegateForFunctionPointer"
            };
            
            var methodFullName = $"{method.DeclaringType?.Name}.{method.Name}";
            
            return dangerousPatterns.Any(pattern => 
                methodFullName.Contains(pattern));
        }
        
        /// <summary>
        /// 验证权限
        /// </summary>
        private bool ValidatePermissions(string modPath)
        {
            var manifestPath = Path.Combine(modPath, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                logger.LogError("Manifest file not found for permission validation");
                return false;
            }
            
            try
            {
                var manifestJson = File.ReadAllText(manifestPath);
                var manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<ModManifest>(manifestJson);
                
                if (manifest.permissions == null || manifest.permissions.Length == 0)
                {
                    return true; // 没有请求特殊权限
                }
                
                // 检查每个请求的权限
                foreach (var permission in manifest.permissions)
                {
                    if (!config.AllowedPermissions.Contains(permission))
                    {
                        logger.LogError($"Unauthorized permission requested: {permission}");
                        return false;
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Permission validation error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 初始化黑名单API
        /// </summary>
        private HashSet<string> InitializeBlacklistedAPIs()
        {
            return new HashSet<string>
            {
                "System.IO.File.Delete",
                "System.IO.Directory.Delete",
                "System.Diagnostics.Process.Start",
                "System.Net.WebClient",
                "System.Net.Http.HttpClient",
                "System.Reflection.Assembly.Load",
                "System.Reflection.Assembly.LoadFrom",
                "System.Reflection.Assembly.LoadFile",
                "System.AppDomain.CreateDomain",
                "System.Runtime.InteropServices.Marshal",
                "Microsoft.Win32.Registry",
                "System.Security.Cryptography",
                "System.Threading.Thread.Abort",
                "System.Environment.Exit"
            };
        }
        
        /// <summary>
        /// 创建安全上下文
        /// </summary>
        public SecurityContext CreateContext(string modId, string[] requestedPermissions)
        {
            var grantedPermissions = new HashSet<string>();
            
            // 检查模组是否在信任列表中
            if (config.TrustedMods?.Contains(modId) == true)
            {
                // 信任的模组获得所有请求的权限
                grantedPermissions = new HashSet<string>(requestedPermissions ?? new string[0]);
            }
            else if (config.ModPermissions?.TryGetValue(modId, out var allowedPermissions) == true)
            {
                // 只授予配置中允许的权限
                foreach (var permission in requestedPermissions ?? new string[0])
                {
                    if (allowedPermissions.Contains(permission))
                    {
                        grantedPermissions.Add(permission);
                    }
                }
            }
            else
            {
                // 使用默认权限集
                foreach (var permission in requestedPermissions ?? new string[0])
                {
                    if (config.DefaultPermissions?.Contains(permission) == true)
                    {
                        grantedPermissions.Add(permission);
                    }
                }
            }
            
            var resourceLimits = config.ModResourceLimits?.GetValueOrDefault(modId) ?? 
                                config.ModResourceLimits?.GetValueOrDefault("default") ?? 
                                new ResourceLimits();
            
            return new SecurityContext
            {
                ModId = modId,
                Permissions = grantedPermissions,
                ResourceLimits = resourceLimits
            };
        }
    }
} 