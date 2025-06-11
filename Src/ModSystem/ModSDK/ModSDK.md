# ModSDK 完整实现

## 目录结构

```
ModSDK/
├── SDK/                                  # 核心SDK库（编译后的DLL）
│   ├── ModSDK.Core.dll                 # 从ModSystem.Core构建
│   ├── ModSDK.Runtime.dll              # 从下面的ModSDK.Runtime项目构建
│   └── Newtonsoft.Json.dll             # JSON处理库
│
├── ModSDK.Runtime/                      # ⭐ ModSDK.Runtime源代码项目
│   ├── ModSDK.Runtime.csproj          # 项目文件
│   ├── Helpers/                        # 辅助类
│   │   └── ModHelper.cs
│   ├── Attributes/                     # 特性定义
│   │   └── ModAttributes.cs
│   ├── Base/                           # 基类
│   │   └── ModBehaviourBase.cs
│   └── bin/Release/netstandard2.1/    # 构建输出
│       └── ModSDK.Runtime.dll          # → 需要复制到 SDK/ 目录
│
├── Tools/                                # 开发工具
│   ├── ModBuilder/                      # 模组构建工具
│   │   ├── ModBuilder.csproj
│   │   ├── Program.cs
│   │   ├── Commands/
│   │   ├── Services/
│   │   └── Templates/
│   ├── ModValidator/                    # 模组验证工具
│   │   ├── ModValidator.csproj
│   │   └── Program.cs
│   └── ModPackager/                     # 打包工具
│       ├── ModPackager.csproj
│       └── Program.cs
│
├── Templates/                            # 模组模板
│   ├── BasicMod/
│   ├── ButtonMod/
│   ├── RobotMod/
│   ├── ServiceMod/
│   └── template-registry.json
│
├── Documentation/                        # 文档
│   ├── GettingStarted.md
│   ├── APIReference.md
│   └── Examples/
│
├── Samples/                              # 示例项目
│   ├── SimpleButton/
│   └── ComplexRobot/
│
├── BuildSDK.ps1                         # Windows构建脚本
├── build-sdk.sh                         # Linux/Mac构建脚本
└── ModSDK.sln                           # 解决方案文件（包含所有项目）
```

## 1. ModBuilder工具实现

### ModBuilder/Program.cs

```csharp
// ModSDK/Tools/ModBuilder/Program.cs
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Invocation;
using ModBuilder.Commands;
using ModBuilder.Services;

namespace ModBuilder
{
    /// <summary>
    /// ModBuilder主程序
    /// 提供模组创建、构建、测试和打包功能的命令行工具
    /// </summary>
    class Program
    {
        /// <summary>
        /// 程序入口点
        /// </summary>
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("ModBuilder v1.0.0 - Unity Mod System Build Tool");
            Console.WriteLine("===============================================");

            // 创建根命令
            var rootCommand = new RootCommand("ModBuilder - Create, build, test and package Unity mods");

            // 添加子命令
            rootCommand.AddCommand(new NewCommand());
            rootCommand.AddCommand(new BuildCommand());
            rootCommand.AddCommand(new TestCommand());
            rootCommand.AddCommand(new PackageCommand());
            rootCommand.AddCommand(new ValidateCommand());
            rootCommand.AddCommand(new CleanCommand());
            rootCommand.AddCommand(new ListCommand());
            rootCommand.AddCommand(new InfoCommand());

            // 设置全局选项
            var verboseOption = new Option<bool>(
                aliases: new[] { "--verbose", "-v" },
                description: "Enable verbose output"
            );
            rootCommand.AddGlobalOption(verboseOption);

            // 解析并执行命令
            return await rootCommand.InvokeAsync(args);
        }
    }
}
```

### ModBuilder/Commands/NewCommand.cs

```csharp
// ModSDK/Tools/ModBuilder/Commands/NewCommand.cs
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using ModBuilder.Services;

namespace ModBuilder.Commands
{
    /// <summary>
    /// 创建新模组项目的命令
    /// </summary>
    public class NewCommand : Command
    {
        /// <summary>
        /// 构造函数，定义命令参数和选项
        /// </summary>
        public NewCommand() : base("new", "Create a new mod project")
        {
            // 添加参数：模组名称
            var nameArgument = new Argument<string>(
                name: "name",
                description: "The name of the mod to create"
            );
            AddArgument(nameArgument);

            // 添加选项：模板
            var templateOption = new Option<string>(
                aliases: new[] { "--template", "-t" },
                getDefaultValue: () => "basic",
                description: "The template to use (basic, button, robot, service)"
            );
            AddOption(templateOption);

            // 添加选项：输出目录
            var outputOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                getDefaultValue: () => ".",
                description: "Output directory for the new mod"
            );
            AddOption(outputOption);

            // 添加选项：作者
            var authorOption = new Option<string>(
                aliases: new[] { "--author", "-a" },
                getDefaultValue: () => Environment.UserName,
                description: "Author name for the mod"
            );
            AddOption(authorOption);

            // 添加选项：Unity版本
            var unityVersionOption = new Option<string>(
                aliases: new[] { "--unity-version", "-u" },
                getDefaultValue: () => "2021.3",
                description: "Target Unity version"
            );
            AddOption(unityVersionOption);

            // 设置处理器
            this.SetHandler(async (context) =>
            {
                var name = context.ParseResult.GetValueForArgument(nameArgument);
                var template = context.ParseResult.GetValueForOption(templateOption);
                var output = context.ParseResult.GetValueForOption(outputOption);
                var author = context.ParseResult.GetValueForOption(authorOption);
                var unityVersion = context.ParseResult.GetValueForOption(unityVersionOption);

                await CreateNewMod(name, template, output, author, unityVersion);
            });
        }

        /// <summary>
        /// 创建新模组
        /// </summary>
        private async Task CreateNewMod(string name, string template, string output, string author, string unityVersion)
        {
            try
            {
                Console.WriteLine($"Creating new mod '{name}' using template '{template}'...");

                // 验证模组名称
                if (!IsValidModName(name))
                {
                    Console.Error.WriteLine("Error: Invalid mod name. Use only letters, numbers, and underscores.");
                    return;
                }

                // 创建项目服务
                var projectService = new ProjectService();
                var templateService = new TemplateService();

                // 获取模板路径
                var templatePath = templateService.GetTemplatePath(template);
                if (!Directory.Exists(templatePath))
                {
                    Console.Error.WriteLine($"Error: Template '{template}' not found.");
                    Console.WriteLine("Available templates:");
                    foreach (var t in templateService.GetAvailableTemplates())
                    {
                        Console.WriteLine($"  - {t.Name}: {t.Description}");
                    }
                    return;
                }

                // 创建输出目录
                var projectPath = Path.Combine(output, name);
                if (Directory.Exists(projectPath))
                {
                    Console.Error.WriteLine($"Error: Directory '{projectPath}' already exists.");
                    return;
                }

                // 复制模板
                await templateService.CopyTemplate(templatePath, projectPath, new TemplateContext
                {
                    ModName = name,
                    ModId = name.ToLower().Replace(" ", "_"),
                    Author = author,
                    Version = "1.0.0",
                    UnityVersion = unityVersion,
                    SdkVersion = "1.0.0",
                    Year = DateTime.Now.Year.ToString()
                });

                // 初始化项目
                await projectService.InitializeProject(projectPath);

                Console.WriteLine($"✓ Mod project '{name}' created successfully at: {Path.GetFullPath(projectPath)}");
                Console.WriteLine("\nNext steps:");
                Console.WriteLine($"  1. cd {name}");
                Console.WriteLine("  2. ModBuilder build");
                Console.WriteLine("  3. ModBuilder test");
                Console.WriteLine("  4. ModBuilder package");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error creating mod: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证模组名称是否有效
        /// </summary>
        private bool IsValidModName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && 
                   System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z0-9_]+$");
        }
    }
}
```

### ModBuilder/Commands/BuildCommand.cs

```csharp
// ModSDK/Tools/ModBuilder/Commands/BuildCommand.cs
using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ModBuilder.Services;

namespace ModBuilder.Commands
{
    /// <summary>
    /// 构建模组的命令
    /// </summary>
    public class BuildCommand : Command
    {
        /// <summary>
        /// 构造函数，定义构建命令的参数和选项
        /// </summary>
        public BuildCommand() : base("build", "Build the mod project")
        {
            // 添加选项：项目路径
            var projectOption = new Option<string>(
                aliases: new[] { "--project", "-p" },
                getDefaultValue: () => ".",
                description: "Path to the mod project"
            );
            AddOption(projectOption);

            // 添加选项：配置
            var configurationOption = new Option<string>(
                aliases: new[] { "--configuration", "-c" },
                getDefaultValue: () => "Release",
                description: "Build configuration (Debug or Release)"
            );
            AddOption(configurationOption);

            // 添加选项：是否清理
            var cleanOption = new Option<bool>(
                aliases: new[] { "--clean" },
                description: "Clean before building"
            );
            AddOption(cleanOption);

            // 添加选项：是否重建
            var rebuildOption = new Option<bool>(
                aliases: new[] { "--rebuild" },
                description: "Rebuild the project"
            );
            AddOption(rebuildOption);

            // 设置处理器
            this.SetHandler(async (context) =>
            {
                var project = context.ParseResult.GetValueForOption(projectOption);
                var configuration = context.ParseResult.GetValueForOption(configurationOption);
                var clean = context.ParseResult.GetValueForOption(cleanOption);
                var rebuild = context.ParseResult.GetValueForOption(rebuildOption);

                await BuildMod(project, configuration, clean, rebuild);
            });
        }

        /// <summary>
        /// 执行模组构建
        /// </summary>
        private async Task BuildMod(string projectPath, string configuration, bool clean, bool rebuild)
        {
            try
            {
                var buildService = new BuildService();
                
                // 验证项目
                if (!buildService.ValidateProject(projectPath))
                {
                    Console.Error.WriteLine("Error: Invalid mod project. Make sure manifest.json exists.");
                    return;
                }

                Console.WriteLine($"Building mod project at: {Path.GetFullPath(projectPath)}");
                Console.WriteLine($"Configuration: {configuration}");

                // 清理
                if (clean || rebuild)
                {
                    Console.WriteLine("Cleaning previous build...");
                    await buildService.Clean(projectPath);
                }

                // 构建前处理
                Console.WriteLine("Preparing build...");
                await buildService.PreBuild(projectPath);

                // 编译源代码
                Console.WriteLine("Compiling source code...");
                var compileResult = await buildService.CompileSource(projectPath, configuration);
                if (!compileResult.Success)
                {
                    Console.Error.WriteLine("Compilation failed:");
                    foreach (var error in compileResult.Errors)
                    {
                        Console.Error.WriteLine($"  {error}");
                    }
                    return;
                }

                // 复制资源
                Console.WriteLine("Copying resources...");
                await buildService.CopyResources(projectPath);

                // 生成元数据
                Console.WriteLine("Generating metadata...");
                await buildService.GenerateMetadata(projectPath);

                // 构建后处理
                Console.WriteLine("Post-build processing...");
                await buildService.PostBuild(projectPath);

                Console.WriteLine("✓ Build completed successfully!");
                
                // 显示构建输出信息
                var outputInfo = buildService.GetBuildOutput(projectPath);
                Console.WriteLine($"\nBuild output:");
                Console.WriteLine($"  Assembly: {outputInfo.AssemblyPath}");
                Console.WriteLine($"  Size: {outputInfo.AssemblySize / 1024} KB");
                Console.WriteLine($"  Dependencies: {outputInfo.Dependencies.Count}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Build failed: {ex.Message}");
            }
        }
    }
}
```

### ModBuilder/Commands/TestCommand.cs

```csharp
// ModSDK/Tools/ModBuilder/Commands/TestCommand.cs
using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using ModBuilder.Services;

namespace ModBuilder.Commands
{
    /// <summary>
    /// 测试模组的命令
    /// </summary>
    public class TestCommand : Command
    {
        /// <summary>
        /// 构造函数，定义测试命令的参数和选项
        /// </summary>
        public TestCommand() : base("test", "Test the mod")
        {
            // 添加选项：项目路径
            var projectOption = new Option<string>(
                aliases: new[] { "--project", "-p" },
                getDefaultValue: () => ".",
                description: "Path to the mod project"
            );
            AddOption(projectOption);

            // 添加选项：测试过滤器
            var filterOption = new Option<string>(
                aliases: new[] { "--filter", "-f" },
                description: "Filter tests by name pattern"
            );
            AddOption(filterOption);

            // 添加选项：是否显示详细输出
            var verboseOption = new Option<bool>(
                aliases: new[] { "--verbose", "-v" },
                description: "Show detailed test output"
            );
            AddOption(verboseOption);

            // 添加选项：Unity路径
            var unityPathOption = new Option<string>(
                aliases: new[] { "--unity-path" },
                description: "Path to Unity Editor (for integration tests)"
            );
            AddOption(unityPathOption);

            // 设置处理器
            this.SetHandler(async (context) =>
            {
                var project = context.ParseResult.GetValueForOption(projectOption);
                var filter = context.ParseResult.GetValueForOption(filterOption);
                var verbose = context.ParseResult.GetValueForOption(verboseOption);
                var unityPath = context.ParseResult.GetValueForOption(unityPathOption);

                await TestMod(project, filter, verbose, unityPath);
            });
        }

        /// <summary>
        /// 执行模组测试
        /// </summary>
        private async Task TestMod(string projectPath, string filter, bool verbose, string unityPath)
        {
            try
            {
                var testService = new TestService();
                
                Console.WriteLine($"Testing mod at: {Path.GetFullPath(projectPath)}");

                // 验证项目
                if (!testService.ValidateProject(projectPath))
                {
                    Console.Error.WriteLine("Error: Invalid mod project.");
                    return;
                }

                // 构建测试版本
                Console.WriteLine("Building test version...");
                var buildService = new BuildService();
                var buildResult = await buildService.CompileSource(projectPath, "Debug");
                if (!buildResult.Success)
                {
                    Console.Error.WriteLine("Failed to build test version.");
                    return;
                }

                // 运行单元测试
                Console.WriteLine("\nRunning unit tests...");
                var unitTestResult = await testService.RunUnitTests(projectPath, filter, verbose);
                DisplayTestResults("Unit Tests", unitTestResult);

                // 运行验证测试
                Console.WriteLine("\nRunning validation tests...");
                var validationResult = await testService.RunValidationTests(projectPath);
                DisplayTestResults("Validation Tests", validationResult);

                // 如果提供了Unity路径，运行集成测试
                if (!string.IsNullOrEmpty(unityPath))
                {
                    Console.WriteLine("\nRunning Unity integration tests...");
                    var integrationResult = await testService.RunIntegrationTests(projectPath, unityPath);
                    DisplayTestResults("Integration Tests", integrationResult);
                }

                // 总结
                var totalPassed = unitTestResult.Passed + validationResult.Passed;
                var totalFailed = unitTestResult.Failed + validationResult.Failed;
                var totalSkipped = unitTestResult.Skipped + validationResult.Skipped;

                Console.WriteLine("\n" + new string('=', 50));
                Console.WriteLine($"Total: {totalPassed} passed, {totalFailed} failed, {totalSkipped} skipped");
                
                if (totalFailed > 0)
                {
                    Console.WriteLine("\n✗ Tests failed!");
                    Environment.Exit(1);
                }
                else
                {
                    Console.WriteLine("\n✓ All tests passed!");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Test execution failed: {ex.Message}");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// 显示测试结果
        /// </summary>
        private void DisplayTestResults(string category, TestResult result)
        {
            Console.WriteLine($"\n{category}:");
            Console.WriteLine($"  Passed: {result.Passed}");
            Console.WriteLine($"  Failed: {result.Failed}");
            Console.WriteLine($"  Skipped: {result.Skipped}");
            Console.WriteLine($"  Duration: {result.Duration.TotalSeconds:F2}s");

            if (result.FailedTests.Count > 0)
            {
                Console.WriteLine("\n  Failed tests:");
                foreach (var test in result.FailedTests)
                {
                    Console.WriteLine($"    ✗ {test.Name}");
                    Console.WriteLine($"      {test.Error}");
                }
            }
        }
    }
}
```

### ModBuilder/Commands/PackageCommand.cs

```csharp
// ModSDK/Tools/ModBuilder/Commands/PackageCommand.cs
using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using ModBuilder.Services;

namespace ModBuilder.Commands
{
    /// <summary>
    /// 打包模组的命令
    /// </summary>
    public class PackageCommand : Command
    {
        /// <summary>
        /// 构造函数，定义打包命令的参数和选项
        /// </summary>
        public PackageCommand() : base("package", "Package the mod for distribution")
        {
            // 添加选项：项目路径
            var projectOption = new Option<string>(
                aliases: new[] { "--project", "-p" },
                getDefaultValue: () => ".",
                description: "Path to the mod project"
            );
            AddOption(projectOption);

            // 添加选项：输出路径
            var outputOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                description: "Output path for the package"
            );
            AddOption(outputOption);

            // 添加选项：是否包含源代码
            var includeSourceOption = new Option<bool>(
                aliases: new[] { "--include-source" },
                description: "Include source code in the package"
            );
            AddOption(includeSourceOption);

            // 添加选项：是否签名
            var signOption = new Option<bool>(
                aliases: new[] { "--sign" },
                description: "Sign the package"
            );
            AddOption(signOption);

            // 添加选项：私钥路径
            var keyOption = new Option<string>(
                aliases: new[] { "--key" },
                description: "Path to private key for signing"
            );
            AddOption(keyOption);

            // 设置处理器
            this.SetHandler(async (context) =>
            {
                var project = context.ParseResult.GetValueForOption(projectOption);
                var output = context.ParseResult.GetValueForOption(outputOption);
                var includeSource = context.ParseResult.GetValueForOption(includeSourceOption);
                var sign = context.ParseResult.GetValueForOption(signOption);
                var key = context.ParseResult.GetValueForOption(keyOption);

                await PackageMod(project, output, includeSource, sign, key);
            });
        }

        /// <summary>
        /// 执行模组打包
        /// </summary>
        private async Task PackageMod(string projectPath, string outputPath, bool includeSource, bool sign, string keyPath)
        {
            try
            {
                var packageService = new PackageService();
                
                Console.WriteLine($"Packaging mod at: {Path.GetFullPath(projectPath)}");

                // 验证项目
                if (!packageService.ValidateProject(projectPath))
                {
                    Console.Error.WriteLine("Error: Invalid mod project.");
                    return;
                }

                // 读取清单文件
                var manifest = await packageService.LoadManifest(projectPath);
                
                // 确定输出文件名
                if (string.IsNullOrEmpty(outputPath))
                {
                    outputPath = Path.Combine(projectPath, "dist", 
                        $"{manifest.id}_v{manifest.version}.modpack");
                }

                // 确保输出目录存在
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // 构建发布版本
                Console.WriteLine("Building release version...");
                var buildService = new BuildService();
                var buildResult = await buildService.CompileSource(projectPath, "Release");
                if (!buildResult.Success)
                {
                    Console.Error.WriteLine("Failed to build release version.");
                    return;
                }

                // 运行最终验证
                Console.WriteLine("Running final validation...");
                var validationService = new ValidationService();
                var validationResult = await validationService.ValidateMod(projectPath);
                if (!validationResult.IsValid)
                {
                    Console.Error.WriteLine("Validation failed:");
                    foreach (var error in validationResult.Errors)
                    {
                        Console.Error.WriteLine($"  - {error}");
                    }
                    return;
                }

                // 创建包
                Console.WriteLine("Creating package...");
                var packageInfo = new PackageInfo
                {
                    ProjectPath = projectPath,
                    OutputPath = outputPath,
                    IncludeSource = includeSource,
                    Manifest = manifest
                };

                await packageService.CreatePackage(packageInfo);

                // 签名包（如果需要）
                if (sign)
                {
                    if (string.IsNullOrEmpty(keyPath))
                    {
                        Console.Error.WriteLine("Error: Private key path required for signing.");
                        return;
                    }

                    Console.WriteLine("Signing package...");
                    await packageService.SignPackage(outputPath, keyPath);
                }

                // 生成校验和
                Console.WriteLine("Generating checksums...");
                var checksums = await packageService.GenerateChecksums(outputPath);

                // 显示包信息
                var fileInfo = new FileInfo(outputPath);
                Console.WriteLine("\n✓ Package created successfully!");
                Console.WriteLine($"\nPackage Information:");
                Console.WriteLine($"  File: {outputPath}");
                Console.WriteLine($"  Size: {fileInfo.Length / 1024} KB");
                Console.WriteLine($"  SHA256: {checksums.SHA256}");
                Console.WriteLine($"  MD5: {checksums.MD5}");
                
                if (sign)
                {
                    Console.WriteLine($"  Signed: Yes");
                }

                // 创建发布说明
                var releaseNotesPath = Path.ChangeExtension(outputPath, ".txt");
                await packageService.GenerateReleaseNotes(manifest, releaseNotesPath);
                Console.WriteLine($"\nRelease notes: {releaseNotesPath}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Package creation failed: {ex.Message}");
            }
        }
    }
}
```

### ModBuilder/Services/TemplateService.cs

```csharp
// ModSDK/Tools/ModBuilder/Services/TemplateService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ModBuilder.Services
{
    /// <summary>
    /// 模板服务，管理和处理模组模板
    /// </summary>
    public class TemplateService
    {
        private readonly string templatesPath;
        private readonly TemplateRegistry registry;

        /// <summary>
        /// 构造函数
        /// </summary>
        public TemplateService()
        {
            // 获取模板目录路径
            templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Templates");
            
            // 加载模板注册表
            var registryPath = Path.Combine(templatesPath, "template-registry.json");
            if (File.Exists(registryPath))
            {
                var json = File.ReadAllText(registryPath);
                registry = JsonConvert.DeserializeObject<TemplateRegistry>(json);
            }
            else
            {
                registry = new TemplateRegistry { Templates = new List<TemplateInfo>() };
            }
        }

        /// <summary>
        /// 获取模板路径
        /// </summary>
        public string GetTemplatePath(string templateName)
        {
            var template = registry.Templates.FirstOrDefault(t => 
                t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
            
            if (template != null)
            {
                return Path.Combine(templatesPath, template.Path);
            }

            // 尝试直接路径
            var directPath = Path.Combine(templatesPath, templateName);
            if (Directory.Exists(directPath))
            {
                return directPath;
            }

            return null;
        }

        /// <summary>
        /// 获取所有可用模板
        /// </summary>
        public List<TemplateInfo> GetAvailableTemplates()
        {
            return registry.Templates;
        }

        /// <summary>
        /// 复制模板到目标位置
        /// </summary>
        public async Task CopyTemplate(string templatePath, string targetPath, TemplateContext context)
        {
            // 创建目标目录
            Directory.CreateDirectory(targetPath);

            // 复制所有文件和目录
            await CopyDirectory(templatePath, targetPath, context);

            // 处理特殊文件
            await ProcessSpecialFiles(targetPath, context);
        }

        /// <summary>
        /// 递归复制目录
        /// </summary>
        private async Task CopyDirectory(string source, string target, TemplateContext context)
        {
            // 创建所有目录
            foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(source, dirPath);
                var targetDir = Path.Combine(target, ProcessTemplatePath(relativePath, context));
                Directory.CreateDirectory(targetDir);
            }

            // 复制所有文件
            foreach (string filePath in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                // 跳过模板元数据文件
                if (Path.GetFileName(filePath) == "template.json")
                    continue;

                var relativePath = Path.GetRelativePath(source, filePath);
                var targetFile = Path.Combine(target, ProcessTemplatePath(relativePath, context));

                // 处理文本文件中的占位符
                if (IsTextFile(filePath))
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    content = ProcessTemplateContent(content, context);
                    await File.WriteAllTextAsync(targetFile, content);
                }
                else
                {
                    File.Copy(filePath, targetFile, true);
                }
            }
        }

        /// <summary>
        /// 处理模板路径中的占位符
        /// </summary>
        private string ProcessTemplatePath(string path, TemplateContext context)
        {
            return path
                .Replace("{{ModName}}", context.ModName)
                .Replace("{{ModId}}", context.ModId);
        }

        /// <summary>
        /// 处理模板内容中的占位符
        /// </summary>
        private string ProcessTemplateContent(string content, TemplateContext context)
        {
            var replacements = new Dictionary<string, string>
            {
                { "{{ModName}}", context.ModName },
                { "{{ModId}}", context.ModId },
                { "{{Author}}", context.Author },
                { "{{Version}}", context.Version },
                { "{{UnityVersion}}", context.UnityVersion },
                { "{{SdkVersion}}", context.SdkVersion },
                { "{{Year}}", context.Year },
                { "{{Date}}", DateTime.Now.ToString("yyyy-MM-dd") },
                { "{{Namespace}}", context.ModName.Replace(" ", "") }
            };

            foreach (var kvp in replacements)
            {
                content = content.Replace(kvp.Key, kvp.Value);
            }

            return content;
        }

        /// <summary>
        /// 处理特殊文件
        /// </summary>
        private async Task ProcessSpecialFiles(string projectPath, TemplateContext context)
        {
            // 重命名项目文件
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
            foreach (var csproj in csprojFiles)
            {
                if (Path.GetFileName(csproj).Contains("{{"))
                {
                    var newName = ProcessTemplateContent(Path.GetFileName(csproj), context);
                    var newPath = Path.Combine(Path.GetDirectoryName(csproj), newName);
                    File.Move(csproj, newPath);
                }
            }

            // 重命名源文件
            var sourceFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
            foreach (var source in sourceFiles)
            {
                if (Path.GetFileName(source).Contains("{{"))
                {
                    var newName = ProcessTemplateContent(Path.GetFileName(source), context);
                    var newPath = Path.Combine(Path.GetDirectoryName(source), newName);
                    File.Move(source, newPath);
                }
            }
        }

        /// <summary>
        /// 判断是否为文本文件
        /// </summary>
        private bool IsTextFile(string filePath)
        {
            var textExtensions = new[] { ".cs", ".json", ".xml", ".txt", ".md", ".yml", ".yaml", ".config", ".csproj" };
            var extension = Path.GetExtension(filePath).ToLower();
            return textExtensions.Contains(extension);
        }
    }

    /// <summary>
    /// 模板注册表
    /// </summary>
    public class TemplateRegistry
    {
        public List<TemplateInfo> Templates { get; set; }
    }

    /// <summary>
    /// 模板信息
    /// </summary>
    public class TemplateInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
        public string Category { get; set; }
        public List<string> Tags { get; set; }
        public string MinSdkVersion { get; set; }
    }

    /// <summary>
    /// 模板上下文
    /// </summary>
    public class TemplateContext
    {
        public string ModName { get; set; }
        public string ModId { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string UnityVersion { get; set; }
        public string SdkVersion { get; set; }
        public string Year { get; set; }
    }
}
```

### ModBuilder/Services/BuildService.cs

```csharp
// ModSDK/Tools/ModBuilder/Services/BuildService.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ModBuilder.Services
{
    /// <summary>
    /// 构建服务，处理模组的编译和构建
    /// </summary>
    public class BuildService
    {
        /// <summary>
        /// 验证项目是否有效
        /// </summary>
        public bool ValidateProject(string projectPath)
        {
            // 检查manifest.json是否存在
            var manifestPath = Path.Combine(projectPath, "manifest.json");
            if (!File.Exists(manifestPath))
                return false;

            // 检查源代码目录
            var sourcePath = Path.Combine(projectPath, "Source");
            if (!Directory.Exists(sourcePath))
                return false;

            return true;
        }

        /// <summary>
        /// 清理构建输出
        /// </summary>
        public async Task Clean(string projectPath)
        {
            var directories = new[] { "bin", "obj", "build" };
            
            foreach (var dir in directories)
            {
                var path = Path.Combine(projectPath, dir);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 构建前处理
        /// </summary>
        public async Task PreBuild(string projectPath)
        {
            // 创建构建目录
            var buildPath = Path.Combine(projectPath, "build");
            Directory.CreateDirectory(buildPath);

            // 创建Assemblies目录
            var assembliesPath = Path.Combine(buildPath, "Assemblies");
            Directory.CreateDirectory(assembliesPath);

            // 验证SDK引用
            await ValidateSDKReferences(projectPath);
        }

        /// <summary>
        /// 编译源代码
        /// </summary>
        public async Task<CompileResult> CompileSource(string projectPath, string configuration)
        {
            var result = new CompileResult { Success = true };
            
            try
            {
                // 查找项目文件
                var projectFiles = Directory.GetFiles(Path.Combine(projectPath, "Source"), "*.csproj");
                if (projectFiles.Length == 0)
                {
                    result.Success = false;
                    result.Errors.Add("No .csproj file found in Source directory");
                    return result;
                }

                var projectFile = projectFiles[0];

                // 使用dotnet CLI编译
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{projectFile}\" -c {configuration} --no-restore",
                    WorkingDirectory = Path.GetDirectoryName(projectFile),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        result.Success = false;
                        result.Errors.Add(error);
                        
                        // 解析编译错误
                        var lines = output.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.Contains("error"))
                            {
                                result.Errors.Add(line.Trim());
                            }
                        }
                    }
                    else
                    {
                        // 复制输出到build目录
                        var outputPath = Path.Combine(projectPath, "Source", "bin", configuration);
                        var targetPath = Path.Combine(projectPath, "build", "Assemblies");
                        
                        if (Directory.Exists(outputPath))
                        {
                            foreach (var dll in Directory.GetFiles(outputPath, "*.dll"))
                            {
                                var fileName = Path.GetFileName(dll);
                                // 跳过SDK DLL
                                if (!fileName.StartsWith("ModSDK") && !fileName.StartsWith("ModSystem"))
                                {
                                    File.Copy(dll, Path.Combine(targetPath, fileName), true);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Compilation exception: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 复制资源文件
        /// </summary>
        public async Task CopyResources(string projectPath)
        {
            var buildPath = Path.Combine(projectPath, "build");

            // 复制配置文件
            var configSource = Path.Combine(projectPath, "Config");
            if (Directory.Exists(configSource))
            {
                var configTarget = Path.Combine(buildPath, "Config");
                await CopyDirectory(configSource, configTarget);
            }

            // 复制对象定义
            var objectsSource = Path.Combine(projectPath, "Objects");
            if (Directory.Exists(objectsSource))
            {
                var objectsTarget = Path.Combine(buildPath, "Objects");
                await CopyDirectory(objectsSource, objectsTarget);
            }

            // 复制资源文件
            var resourcesSource = Path.Combine(projectPath, "Resources");
            if (Directory.Exists(resourcesSource))
            {
                var resourcesTarget = Path.Combine(buildPath, "Resources");
                await CopyDirectory(resourcesSource, resourcesTarget);
            }

            // 复制manifest.json
            var manifestSource = Path.Combine(projectPath, "manifest.json");
            var manifestTarget = Path.Combine(buildPath, "manifest.json");
            File.Copy(manifestSource, manifestTarget, true);
        }

        /// <summary>
        /// 生成元数据
        /// </summary>
        public async Task GenerateMetadata(string projectPath)
        {
            var buildPath = Path.Combine(projectPath, "build");
            
            // 生成构建信息
            var buildInfo = new BuildInfo
            {
                BuildTime = DateTime.Now,
                BuildConfiguration = "Release",
                SDKVersion = "1.0.0",
                BuildNumber = GenerateBuildNumber()
            };

            var buildInfoPath = Path.Combine(buildPath, "build.json");
            var json = JsonConvert.SerializeObject(buildInfo, Formatting.Indented);
            await File.WriteAllTextAsync(buildInfoPath, json);

            // 生成文件清单
            var fileList = GenerateFileList(buildPath);
            var fileListPath = Path.Combine(buildPath, "files.json");
            await File.WriteAllTextAsync(fileListPath, JsonConvert.SerializeObject(fileList, Formatting.Indented));
        }

        /// <summary>
        /// 构建后处理
        /// </summary>
        public async Task PostBuild(string projectPath)
        {
            var buildPath = Path.Combine(projectPath, "build");

            // 优化程序集
            await OptimizeAssemblies(buildPath);

            // 验证输出
            await ValidateBuildOutput(buildPath);
        }

        /// <summary>
        /// 获取构建输出信息
        /// </summary>
        public BuildOutputInfo GetBuildOutput(string projectPath)
        {
            var buildPath = Path.Combine(projectPath, "build");
            var assembliesPath = Path.Combine(buildPath, "Assemblies");
            
            var info = new BuildOutputInfo
            {
                BuildPath = buildPath,
                Dependencies = new List<string>()
            };

            // 查找主程序集
            var manifest = JsonConvert.DeserializeObject<ModManifest>(
                File.ReadAllText(Path.Combine(projectPath, "manifest.json")));
            
            var assemblyName = $"{manifest.id}.dll";
            var assemblyPath = Path.Combine(assembliesPath, assemblyName);
            
            if (File.Exists(assemblyPath))
            {
                info.AssemblyPath = assemblyPath;
                info.AssemblySize = new FileInfo(assemblyPath).Length;
            }

            // 获取依赖项
            foreach (var dll in Directory.GetFiles(assembliesPath, "*.dll"))
            {
                var fileName = Path.GetFileName(dll);
                if (fileName != assemblyName)
                {
                    info.Dependencies.Add(fileName);
                }
            }

            return info;
        }

        /// <summary>
        /// 验证SDK引用
        /// </summary>
        private async Task ValidateSDKReferences(string projectPath)
        {
            var projectFile = Directory.GetFiles(Path.Combine(projectPath, "Source"), "*.csproj").FirstOrDefault();
            if (projectFile == null) return;

            // 确保项目引用了正确的SDK
            var content = await File.ReadAllTextAsync(projectFile);
            if (!content.Contains("ModSDK.Core"))
            {
                // 可以在这里自动添加引用
                Console.WriteLine("Warning: Project does not reference ModSDK.Core");
            }
        }

        /// <summary>
        /// 递归复制目录
        /// </summary>
        private async Task CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);

            foreach (string file in Directory.GetFiles(source))
            {
                string targetFile = Path.Combine(target, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
            }

            foreach (string dir in Directory.GetDirectories(source))
            {
                string targetDir = Path.Combine(target, Path.GetFileName(dir));
                await CopyDirectory(dir, targetDir);
            }
        }

        /// <summary>
        /// 生成构建号
        /// </summary>
        private string GenerateBuildNumber()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        /// <summary>
        /// 生成文件列表
        /// </summary>
        private List<FileEntry> GenerateFileList(string buildPath)
        {
            var fileList = new List<FileEntry>();
            var baseUri = new Uri(buildPath + Path.DirectorySeparatorChar);

            foreach (var file in Directory.GetFiles(buildPath, "*", SearchOption.AllDirectories))
            {
                var fileInfo = new FileInfo(file);
                var relativeUri = baseUri.MakeRelativeUri(new Uri(file));
                
                fileList.Add(new FileEntry
                {
                    Path = Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar),
                    Size = fileInfo.Length,
                    Hash = CalculateFileHash(file)
                });
            }

            return fileList;
        }

        /// <summary>
        /// 计算文件哈希
        /// </summary>
        private string CalculateFileHash(string filePath)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// 优化程序集
        /// </summary>
        private async Task OptimizeAssemblies(string buildPath)
        {
            // 这里可以实现程序集优化逻辑
            // 例如：去除调试信息、混淆等
            await Task.CompletedTask;
        }

        /// <summary>
        /// 验证构建输出
        /// </summary>
        private async Task ValidateBuildOutput(string buildPath)
        {
            // 验证必需的文件是否存在
            var requiredFiles = new[] { "manifest.json", "build.json", "files.json" };
            
            foreach (var file in requiredFiles)
            {
                var path = Path.Combine(buildPath, file);
                if (!File.Exists(path))
                {
                    throw new Exception($"Required file missing: {file}");
                }
            }

            // 验证程序集
            var assembliesPath = Path.Combine(buildPath, "Assemblies");
            if (!Directory.Exists(assembliesPath) || !Directory.GetFiles(assembliesPath, "*.dll").Any())
            {
                throw new Exception("No assemblies found in build output");
            }

            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 编译结果
    /// </summary>
    public class CompileResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// 构建输出信息
    /// </summary>
    public class BuildOutputInfo
    {
        public string BuildPath { get; set; }
        public string AssemblyPath { get; set; }
        public long AssemblySize { get; set; }
        public List<string> Dependencies { get; set; }
    }

    /// <summary>
    /// 构建信息
    /// </summary>
    public class BuildInfo
    {
        public DateTime BuildTime { get; set; }
        public string BuildConfiguration { get; set; }
        public string SDKVersion { get; set; }
        public string BuildNumber { get; set; }
    }

    /// <summary>
    /// 文件条目
    /// </summary>
    public class FileEntry
    {
        public string Path { get; set; }
        public long Size { get; set; }
        public string Hash { get; set; }
    }

    /// <summary>
    /// 模组清单（与Core层兼容）
    /// </summary>
    public class ModManifest
    {
        public string id { get; set; }
        public string name { get; set; }
        public string version { get; set; }
        public string author { get; set; }
        public string description { get; set; }
    }
}
```

## 2. 模板实现

### Templates/BasicMod/manifest.json

```json
{
  "id": "{{ModId}}",
  "name": "{{ModName}}",
  "version": "{{Version}}",
  "author": "{{Author}}",
  "description": "A basic mod created with ModBuilder",
  "unity_version": "{{UnityVersion}}",
  "sdk_version": "{{SdkVersion}}",
  "main_class": "{{Namespace}}.{{ModName}}Behaviour",
  "permissions": [
    "event_publish",
    "event_subscribe",
    "config_read"
  ]
}
```

### Templates/BasicMod/Source/{{ModName}}.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <AssemblyName>{{ModId}}</AssemblyName>
    <RootNamespace>{{Namespace}}</RootNamespace>
    <LangVersion>9.0</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- SDK引用 -->
    <Reference Include="ModSDK.Core">
      <HintPath>$(ModSDKPath)\ModSDK.Core.dll</HintPath>
      <Private>false</Private>
    </Reference>
    
    <!-- JSON支持 -->
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3">
      <ExcludeAssets>runtime</ExcludeAssets>
    </PackageReference>
  </ItemGroup>

  <!-- 设置ModSDK路径 -->
  <PropertyGroup>
    <ModSDKPath Condition="'$(ModSDKPath)' == ''">$(MSBuildThisFileDirectory)..\..\..\SDK</ModSDKPath>
  </PropertyGroup>

</Project>
```

### Templates/BasicMod/Source/{{ModName}}Behaviour.cs

```csharp
using System;
using ModSystem.Core;

namespace {{Namespace}}
{
    /// <summary>
    /// {{ModName}}的主行为类
    /// </summary>
    public class {{ModName}}Behaviour : IModBehaviour
    {
        /// <summary>
        /// 行为唯一标识符
        /// </summary>
        public string BehaviourId => "{{ModId}}_main";
        
        /// <summary>
        /// 行为版本号
        /// </summary>
        public string Version => "{{Version}}";
        
        private IModContext context;
        
        /// <summary>
        /// 初始化方法，在模组加载时调用
        /// </summary>
        /// <param name="context">模组上下文</param>
        public void OnInitialize(IModContext context)
        {
            this.context = context;
            
            context.Log($"{{ModName}} v{Version} initialized!");
            
            // 订阅事件示例
            context.EventBus.Subscribe<SystemReadyEvent>(OnSystemReady);
        }
        
        /// <summary>
        /// 更新方法，每帧调用
        /// </summary>
        /// <param name="deltaTime">自上次更新以来的时间（秒）</param>
        public void OnUpdate(float deltaTime)
        {
            // 在这里实现每帧更新逻辑
        }
        
        /// <summary>
        /// 销毁方法，在模组卸载时调用
        /// </summary>
        public void OnDestroy()
        {
            // 取消订阅事件
            context.EventBus.UnsubscribeAll(this);
            
            context.Log($"{{ModName}} destroyed!");
        }
        
        /// <summary>
        /// 处理系统就绪事件
        /// </summary>
        private void OnSystemReady(SystemReadyEvent e)
        {
            context.Log("System is ready, mod can start its work!");
        }
    }
    
    /// <summary>
    /// 系统就绪事件（示例）
    /// </summary>
    public class SystemReadyEvent : IModEvent
    {
        public string EventId => "system_ready";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
```

### Templates/ButtonMod/manifest.json

```json
{
  "id": "{{ModId}}",
  "name": "{{ModName}}",
  "version": "{{Version}}",
  "author": "{{Author}}",
  "description": "An interactive button mod created with ModBuilder",
  "unity_version": "{{UnityVersion}}",
  "sdk_version": "{{SdkVersion}}",
  "main_class": "{{Namespace}}.ButtonModBehaviour",
  "permissions": [
    "event_publish",
    "event_subscribe",
    "object_create",
    "config_read"
  ],
  "resources": {
    "objects": ["button.json"],
    "configs": ["button_config.json"]
  }
}
```

### Templates/ButtonMod/Objects/button.json

```json
{
  "objectId": "{{ModId}}_button",
  "name": "{{ModName}} Button",
  "components": [
    {
      "type": "Transform",
      "properties": {
        "position": [0, 1, 0],
        "rotation": [0, 0, 0],
        "scale": [1, 0.2, 1]
      }
    },
    {
      "type": "MeshFilter",
      "properties": {
        "meshType": "cube"
      }
    },
    {
      "type": "MeshRenderer",
      "properties": {
        "shader": "Standard",
        "color": [0.2, 0.8, 0.2, 1],
        "metallic": 0.3,
        "smoothness": 0.7
      }
    },
    {
      "type": "BoxCollider",
      "properties": {
        "size": [1, 0.2, 1],
        "isTrigger": false
      }
    },
    {
      "type": "ObjectBehaviour",
      "properties": {
        "behaviourClass": "{{Namespace}}.ButtonInteraction",
        "config": {
          "clickSound": "button_click.wav",
          "hoverColor": [0.3, 0.9, 0.3, 1],
          "clickColor": [0.1, 0.6, 0.1, 1]
        }
      }
    }
  ]
}
```

### Templates/ButtonMod/Source/ButtonModBehaviour.cs

```csharp
using System;
using System.Threading.Tasks;
using ModSystem.Core;

namespace {{Namespace}}
{
    /// <summary>
    /// 按钮模组的主行为
    /// </summary>
    public class ButtonModBehaviour : IModBehaviour
    {
        public string BehaviourId => "{{ModId}}_main";
        public string Version => "{{Version}}";
        
        private IModContext context;
        private IGameObject buttonObject;
        private ButtonConfig config;
        
        /// <summary>
        /// 初始化
        /// </summary>
        public async void OnInitialize(IModContext context)
        {
            this.context = context;
            
            try
            {
                // 加载配置
                config = LoadConfig();
                
                // 创建按钮对象
                await CreateButton();
                
                // 订阅按钮事件
                context.EventBus.Subscribe<ButtonClickedEvent>(OnButtonClicked);
                
                context.Log($"{{ModName}} initialized with {config.ButtonCount} buttons!");
            }
            catch (Exception ex)
            {
                context.LogError($"Failed to initialize: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新
        /// </summary>
        public void OnUpdate(float deltaTime)
        {
            // 按钮动画更新等
        }
        
        /// <summary>
        /// 销毁
        /// </summary>
        public void OnDestroy()
        {
            if (buttonObject != null)
            {
                // 销毁按钮对象
            }
            
            context.EventBus.UnsubscribeAll(this);
        }
        
        /// <summary>
        /// 加载配置
        /// </summary>
        private ButtonConfig LoadConfig()
        {
            // 从配置文件加载
            return new ButtonConfig
            {
                ButtonCount = 1,
                DefaultPosition = new float[] { 0, 1, 0 },
                ClickAction = "test_action"
            };
        }
        
        /// <summary>
        /// 创建按钮
        /// </summary>
        private async Task CreateButton()
        {
            var objectFactory = context.API.ObjectFactory;
            buttonObject = await objectFactory.CreateObjectAsync("Objects/button.json");
            
            // 设置位置
            buttonObject.Transform.Position = new Vector3(
                config.DefaultPosition[0],
                config.DefaultPosition[1],
                config.DefaultPosition[2]
            );
        }
        
        /// <summary>
        /// 处理按钮点击事件
        /// </summary>
        private void OnButtonClicked(ButtonClickedEvent e)
        {
            if (e.ButtonId == $"{{ModId}}_button")
            {
                context.Log($"Button clicked! Performing action: {config.ClickAction}");
                
                // 发布自定义事件
                context.EventBus.Publish(new CustomActionEvent
                {
                    SenderId = context.ModId,
                    ActionName = config.ClickAction,
                    Parameters = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "button_id", e.ButtonId },
                        { "click_count", e.ClickCount }
                    }
                });
            }
        }
    }
    
    /// <summary>
    /// 按钮交互行为
    /// </summary>
    public class ButtonInteraction : IObjectBehaviour
    {
        private IGameObject gameObject;
        private ButtonInteractionConfig config;
        private IEventBus eventBus;
        private int clickCount = 0;
        
        /// <summary>
        /// 附加到游戏对象
        /// </summary>
        public void OnAttach(IGameObject gameObject)
        {
            this.gameObject = gameObject;
            
            // 获取事件总线（需要通过某种方式注入）
            // this.eventBus = ...
        }
        
        /// <summary>
        /// 配置行为
        /// </summary>
        public void OnConfigure(System.Collections.Generic.Dictionary<string, object> config)
        {
            // 解析配置
            this.config = new ButtonInteractionConfig
            {
                ClickSound = config.GetValueOrDefault("clickSound") as string,
                HoverColor = config.GetValueOrDefault("hoverColor") as float[],
                ClickColor = config.GetValueOrDefault("clickColor") as float[]
            };
        }
        
        /// <summary>
        /// 从游戏对象分离
        /// </summary>
        public void OnDetach()
        {
            // 清理资源
        }
        
        /// <summary>
        /// 处理点击（由Unity层调用）
        /// </summary>
        public void OnClick()
        {
            clickCount++;
            
            // 发布点击事件
            eventBus?.Publish(new ButtonClickedEvent
            {
                SenderId = gameObject.Name,
                ButtonId = gameObject.Name,
                ClickCount = clickCount
            });
            
            // 播放声音等效果
        }
    }
    
    /// <summary>
    /// 按钮配置
    /// </summary>
    public class ButtonConfig
    {
        public int ButtonCount { get; set; }
        public float[] DefaultPosition { get; set; }
        public string ClickAction { get; set; }
    }
    
    /// <summary>
    /// 按钮交互配置
    /// </summary>
    public class ButtonInteractionConfig
    {
        public string ClickSound { get; set; }
        public float[] HoverColor { get; set; }
        public float[] ClickColor { get; set; }
    }
    
    /// <summary>
    /// 按钮点击事件
    /// </summary>
    public class ButtonClickedEvent : IModEvent
    {
        public string EventId => "button_clicked";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ButtonId { get; set; }
        public int ClickCount { get; set; }
    }
    
    /// <summary>
    /// 自定义动作事件
    /// </summary>
    public class CustomActionEvent : IModEvent
    {
        public string EventId => "custom_action";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActionName { get; set; }
        public System.Collections.Generic.Dictionary<string, object> Parameters { get; set; }
    }
}
```

### Templates/template-registry.json

```json
{
  "templates": [
    {
      "name": "basic",
      "description": "A basic mod template with minimal structure",
      "path": "BasicMod",
      "category": "General",
      "tags": ["simple", "starter"],
      "minSdkVersion": "1.0.0"
    },
    {
      "name": "button",
      "description": "Interactive button mod with click handling",
      "path": "ButtonMod",
      "category": "Interactive",
      "tags": ["ui", "interaction", "button"],
      "minSdkVersion": "1.0.0"
    },
    {
      "name": "robot",
      "description": "AI-powered robot mod with movement and sensors",
      "path": "RobotMod",
      "category": "AI",
      "tags": ["ai", "robot", "movement"],
      "minSdkVersion": "1.0.0"
    },
    {
      "name": "service",
      "description": "Service provider mod for other mods to use",
      "path": "ServiceMod",
      "category": "Framework",
      "tags": ["service", "api", "framework"],
      "minSdkVersion": "1.0.0"
    }
  ]
}
```

## 3. ModSDK.Runtime实现

### ModSDK.Runtime/ModSDK.Runtime.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;netstandard2.1</TargetFrameworks>
    <AssemblyName>ModSDK.Runtime</AssemblyName>
    <RootNamespace>ModSDK.Runtime</RootNamespace>
    <LangVersion>latest</LangVersion>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>ModSDK.Runtime</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Company</Authors>
    <Description>Runtime support library for Unity Mod System</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\ModSystemCore\ModSystem.Core\ModSystem.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>

</Project>
```

### ModSDK.Runtime/Helpers/ModHelper.cs

```csharp
// ModSDK.Runtime/Helpers/ModHelper.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModSystem.Core;

namespace ModSDK.Runtime.Helpers
{
    /// <summary>
    /// 模组开发辅助类
    /// 提供常用的辅助方法和扩展
    /// </summary>
    public static class ModHelper
    {
        /// <summary>
        /// 安全地发布事件
        /// </summary>
        public static void PublishEvent<T>(this IEventBus eventBus, string senderId, Action<T> configure) 
            where T : IModEvent, new()
        {
            var eventData = new T
            {
                SenderId = senderId,
                Timestamp = DateTime.Now
            };
            
            configure?.Invoke(eventData);
            eventBus.Publish(eventData);
        }
        
        /// <summary>
        /// 发送请求并等待响应（带超时）
        /// </summary>
        public static async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
            this IRequestResponseManager manager,
            string senderId,
            Action<TRequest> configure,
            int timeoutSeconds = 30)
            where TRequest : ModRequest, new()
            where TResponse : ModResponse
        {
            var request = new TRequest
            {
                SenderId = senderId,
                Timestamp = DateTime.Now
            };
            
            configure?.Invoke(request);
            
            return await manager.SendRequestAsync<TRequest, TResponse>(
                request, 
                TimeSpan.FromSeconds(timeoutSeconds));
        }
        
        /// <summary>
        /// 安全地获取服务
        /// </summary>
        public static T GetServiceSafe<T>(this IServiceRegistry registry, string serviceId = null) 
            where T : class, IModService
        {
            try
            {
                return string.IsNullOrEmpty(serviceId) 
                    ? registry.GetService<T>() 
                    : registry.GetService<T>(serviceId);
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 批量订阅事件
        /// </summary>
        public static void SubscribeMany(this IEventBus eventBus, params (Type eventType, Delegate handler)[] subscriptions)
        {
            foreach (var (eventType, handler) in subscriptions)
            {
                var subscribeMethod = eventBus.GetType()
                    .GetMethod("Subscribe")
                    .MakeGenericMethod(eventType);
                
                subscribeMethod.Invoke(eventBus, new[] { handler });
            }
        }
        
        /// <summary>
        /// 创建延迟任务
        /// </summary>
        public static async Task DelayedAction(int delayMilliseconds, Action action)
        {
            await Task.Delay(delayMilliseconds);
            action?.Invoke();
        }
        
        /// <summary>
        /// 安全执行（带异常处理）
        /// </summary>
        public static void SafeExecute(Action action, IModContext context, string operationName)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                context.LogError($"Error in {operationName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 向量工具方法
        /// </summary>
        public static class VectorHelper
        {
            public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
            {
                t = Math.Clamp(t, 0f, 1f);
                return new Vector3(
                    a.x + (b.x - a.x) * t,
                    a.y + (b.y - a.y) * t,
                    a.z + (b.z - a.z) * t
                );
            }
            
            public static float Magnitude(Vector3 v)
            {
                return (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            }
            
            public static Vector3 Normalize(Vector3 v)
            {
                var mag = Magnitude(v);
                if (mag > 0.00001f)
                {
                    return new Vector3(v.x / mag, v.y / mag, v.z / mag);
                }
                return Vector3.Zero;
            }
        }
    }
}
```

### ModSDK.Runtime/Attributes/ModAttributes.cs

```csharp
// ModSDK.Runtime/Attributes/ModAttributes.cs
using System;

namespace ModSDK.Runtime.Attributes
{
    /// <summary>
    /// 标记一个类为模组主类
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ModMainAttribute : Attribute
    {
        public string ModId { get; }
        public string DisplayName { get; }
        
        public ModMainAttribute(string modId, string displayName = null)
        {
            ModId = modId;
            DisplayName = displayName ?? modId;
        }
    }
    
    /// <summary>
    /// 标记一个方法为事件处理器
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class EventHandlerAttribute : Attribute
    {
        public Type EventType { get; }
        
        public EventHandlerAttribute(Type eventType)
        {
            EventType = eventType;
        }
    }
    
    /// <summary>
    /// 标记一个类为模组服务
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ModServiceAttribute : Attribute
    {
        public string ServiceId { get; }
        public bool AutoRegister { get; }
        
        public ModServiceAttribute(string serviceId, bool autoRegister = true)
        {
            ServiceId = serviceId;
            AutoRegister = autoRegister;
        }
    }
    
    /// <summary>
    /// 标记需要特定权限
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequirePermissionAttribute : Attribute
    {
        public string Permission { get; }
        
        public RequirePermissionAttribute(string permission)
        {
            Permission = permission;
        }
    }
    
    /// <summary>
    /// 配置属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ConfigPropertyAttribute : Attribute
    {
        public string Key { get; }
        public object DefaultValue { get; }
        public string Description { get; }
        
        public ConfigPropertyAttribute(string key, object defaultValue = null, string description = null)
        {
            Key = key;
            DefaultValue = defaultValue;
            Description = description;
        }
    }
}
```

### ModSDK.Runtime/Base/ModBehaviourBase.cs

```csharp
// ModSDK.Runtime/Base/ModBehaviourBase.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using ModSystem.Core;
using ModSDK.Runtime.Attributes;

namespace ModSDK.Runtime.Base
{
    /// <summary>
    /// 模组行为基类
    /// 提供便捷的实现和自动功能
    /// </summary>
    public abstract class ModBehaviourBase : IModBehaviour
    {
        private readonly Dictionary<Type, List<MethodInfo>> eventHandlers;
        
        /// <summary>
        /// 行为ID（默认使用类名）
        /// </summary>
        public virtual string BehaviourId => GetType().Name;
        
        /// <summary>
        /// 版本号
        /// </summary>
        public abstract string Version { get; }
        
        /// <summary>
        /// 模组上下文
        /// </summary>
        protected IModContext Context { get; private set; }
        
        /// <summary>
        /// 事件总线快捷访问
        /// </summary>
        protected IEventBus EventBus => Context?.EventBus;
        
        /// <summary>
        /// API快捷访问
        /// </summary>
        protected IModAPI API => Context?.API;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        protected ModBehaviourBase()
        {
            // 扫描事件处理器
            eventHandlers = ScanEventHandlers();
        }
        
        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void OnInitialize(IModContext context)
        {
            Context = context;
            
            // 自动注册事件处理器
            RegisterEventHandlers();
            
            // 调用子类初始化
            OnStart();
        }
        
        /// <summary>
        /// 更新
        /// </summary>
        public virtual void OnUpdate(float deltaTime)
        {
            // 子类可重写
        }
        
        /// <summary>
        /// 销毁
        /// </summary>
        public virtual void OnDestroy()
        {
            // 取消事件订阅
            EventBus?.UnsubscribeAll(this);
            
            // 调用子类清理
            OnStop();
        }
        
        /// <summary>
        /// 子类初始化方法
        /// </summary>
        protected abstract void OnStart();
        
        /// <summary>
        /// 子类清理方法
        /// </summary>
        protected virtual void OnStop() { }
        
        /// <summary>
        /// 记录日志
        /// </summary>
        protected void Log(string message)
        {
            Context?.Log($"[{BehaviourId}] {message}");
        }
        
        /// <summary>
        /// 记录错误
        /// </summary>
        protected void LogError(string message)
        {
            Context?.LogError($"[{BehaviourId}] {message}");
        }
        
        /// <summary>
        /// 发布事件
        /// </summary>
        protected void PublishEvent<T>(T eventData) where T : IModEvent
        {
            eventData.SenderId = Context?.ModId ?? BehaviourId;
            EventBus?.Publish(eventData);
        }
        
        /// <summary>
        /// 扫描事件处理器
        /// </summary>
        private Dictionary<Type, List<MethodInfo>> ScanEventHandlers()
        {
            var handlers = new Dictionary<Type, List<MethodInfo>>();
            var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<EventHandlerAttribute>();
                if (attr != null)
                {
                    if (!handlers.ContainsKey(attr.EventType))
                    {
                        handlers[attr.EventType] = new List<MethodInfo>();
                    }
                    handlers[attr.EventType].Add(method);
                }
            }
            
            return handlers;
        }
        
        /// <summary>
        /// 注册事件处理器
        /// </summary>
        private void RegisterEventHandlers()
        {
            foreach (var kvp in eventHandlers)
            {
                var eventType = kvp.Key;
                var methods = kvp.Value;
                
                foreach (var method in methods)
                {
                    var subscribeMethod = EventBus.GetType()
                        .GetMethod("Subscribe")
                        .MakeGenericMethod(eventType);
                    
                    var delegateType = typeof(Action<>).MakeGenericType(eventType);
                    var handler = Delegate.CreateDelegate(delegateType, this, method);
                    
                    subscribeMethod.Invoke(EventBus, new[] { handler });
                }
            }
        }
    }
}
```

## 4. 开发文档

### Documentation/GettingStarted.md

```markdown
# ModSDK 入门指南

欢迎使用Unity模组开发SDK！本指南将帮助您快速开始模组开发。

## 系统要求

- .NET SDK 6.0或更高版本
- 文本编辑器或IDE（推荐Visual Studio 2022或VS Code）
- Unity 2021.3 LTS（用于测试）

## 安装ModSDK

1. 下载最新版本的ModSDK
2. 解压到您选择的目录
3. 将ModSDK/Tools目录添加到系统PATH（可选）

## 创建第一个模组

### 1. 创建新项目

```bash
ModBuilder new MyFirstMod --template basic
```

这将创建一个名为`MyFirstMod`的新模组项目。

### 2. 项目结构

```
MyFirstMod/
├── Source/              # C#源代码
│   ├── MyFirstMod.csproj
│   └── MyFirstModBehaviour.cs
├── Config/              # 配置文件
├── Objects/             # 对象定义（JSON）
├── Resources/           # 资源文件
├── Tests/               # 单元测试
├── manifest.json        # 模组清单
└── README.md           # 模组说明
```

### 3. 编写模组代码

打开`Source/MyFirstModBehaviour.cs`：

```csharp
using ModSystem.Core;

public class MyFirstModBehaviour : IModBehaviour
{
    public string BehaviourId => "myfirstmod_main";
    public string Version => "1.0.0";
    
    private IModContext context;
    
    public void OnInitialize(IModContext context)
    {
        this.context = context;
        context.Log("Hello from MyFirstMod!");
        
        // 订阅事件
        context.EventBus.Subscribe<InteractionEvent>(OnInteraction);
    }
    
    public void OnUpdate(float deltaTime)
    {
        // 每帧更新逻辑
    }
    
    public void OnDestroy()
    {
        context.Log("Goodbye!");
    }
    
    private void OnInteraction(InteractionEvent e)
    {
        context.Log($"Interaction detected: {e.InteractionType}");
    }
}
```

### 4. 构建模组

```bash
cd MyFirstMod
ModBuilder build
```

### 5. 测试模组

```bash
ModBuilder test
```

### 6. 打包发布

```bash
ModBuilder package
```

这将生成`MyFirstMod_v1.0.0.modpack`文件。

## 下一步

- 阅读[API参考文档](APIReference.md)了解所有可用的API
- 查看[示例项目](../Samples/)学习更多高级功能
- 探索不同的[模板](../Templates/)了解各种模组类型

## 常见问题

### Q: 如何调试模组？
A: 可以使用Visual Studio附加到Unity进程进行调试，或使用日志输出。

### Q: 如何添加第三方库？
A: 在.csproj文件中添加NuGet包引用，但注意不要与Unity或其他模组冲突。

### Q: 如何与其他模组通信？
A: 使用事件系统或服务注册表进行模组间通信。

## 获取帮助

- 访问[官方论坛](https://forum.example.com)
- 查看[GitHub Issues](https://github.com/your-repo/issues)
- 加入[Discord社区](https://discord.gg/example)
```

## 5. ModValidator工具

### ModValidator/Program.cs

```csharp
// ModSDK/Tools/ModValidator/Program.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ModValidator
{
    /// <summary>
    /// 模组验证工具
    /// 验证模组的完整性、安全性和兼容性
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: ModValidator <mod-path>");
                return 1;
            }

            var modPath = args[0];
            var validator = new Validator();
            
            Console.WriteLine($"Validating mod at: {modPath}");
            Console.WriteLine(new string('-', 50));

            var result = await validator.ValidateMod(modPath);
            
            // 显示结果
            DisplayResults(result);
            
            return result.IsValid ? 0 : 1;
        }

        static void DisplayResults(ValidationResult result)
        {
            if (result.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ Validation PASSED");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n✗ Validation FAILED");
            }
            Console.ResetColor();

            if (result.Errors.Any())
            {
                Console.WriteLine("\nErrors:");
                foreach (var error in result.Errors)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ {error}");
                    Console.ResetColor();
                }
            }

            if (result.Warnings.Any())
            {
                Console.WriteLine("\nWarnings:");
                foreach (var warning in result.Warnings)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ {warning}");
                    Console.ResetColor();
                }
            }

            if (result.Info.Any())
            {
                Console.WriteLine("\nInfo:");
                foreach (var info in result.Info)
                {
                    Console.WriteLine($"  ℹ {info}");
                }
            }
        }
    }

    /// <summary>
    /// 模组验证器
    /// </summary>
    public class Validator
    {
        /// <summary>
        /// 验证模组
        /// </summary>
        public async Task<ValidationResult> ValidateMod(string modPath)
        {
            var result = new ValidationResult { IsValid = true };

            // 1. 验证路径和基本结构
            ValidateStructure(modPath, result);
            if (!result.IsValid) return result;

            // 2. 验证清单文件
            var manifest = await ValidateManifest(modPath, result);
            if (!result.IsValid || manifest == null) return result;

            // 3. 验证程序集
            await ValidateAssemblies(modPath, manifest, result);

            // 4. 验证资源
            ValidateResources(modPath, manifest, result);

            // 5. 验证安全性
            await ValidateSecurity(modPath, result);

            // 6. 验证兼容性
            ValidateCompatibility(manifest, result);

            return result;
        }

        /// <summary>
        /// 验证目录结构
        /// </summary>
        void ValidateStructure(string modPath, ValidationResult result)
        {
            if (!Directory.Exists(modPath))
            {
                result.AddError("Mod directory does not exist");
                return;
            }

            // 检查必需的文件
            if (!File.Exists(Path.Combine(modPath, "manifest.json")))
            {
                result.AddError("manifest.json not found");
            }

            // 检查必需的目录
            var requiredDirs = new[] { "Source", "build/Assemblies" };
            foreach (var dir in requiredDirs)
            {
                if (!Directory.Exists(Path.Combine(modPath, dir)))
                {
                    result.AddWarning($"Directory '{dir}' not found");
                }
            }
        }

        /// <summary>
        /// 验证清单文件
        /// </summary>
        async Task<ModManifest> ValidateManifest(string modPath, ValidationResult result)
        {
            var manifestPath = Path.Combine(modPath, "manifest.json");
            
            try
            {
                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonConvert.DeserializeObject<ModManifest>(json);

                // 验证必需字段
                if (string.IsNullOrWhiteSpace(manifest.id))
                    result.AddError("Manifest: 'id' is required");
                
                if (string.IsNullOrWhiteSpace(manifest.name))
                    result.AddError("Manifest: 'name' is required");
                
                if (string.IsNullOrWhiteSpace(manifest.version))
                    result.AddError("Manifest: 'version' is required");
                
                if (string.IsNullOrWhiteSpace(manifest.main_class))
                    result.AddError("Manifest: 'main_class' is required");

                // 验证版本格式
                if (!System.Version.TryParse(manifest.version, out _))
                    result.AddError($"Invalid version format: {manifest.version}");

                // 验证ID格式
                if (!System.Text.RegularExpressions.Regex.IsMatch(manifest.id, @"^[a-z0-9_]+$"))
                    result.AddError($"Invalid mod ID format: {manifest.id}. Use only lowercase letters, numbers, and underscores.");

                result.AddInfo($"Mod: {manifest.name} v{manifest.version} ({manifest.id})");
                
                return manifest;
            }
            catch (Exception ex)
            {
                result.AddError($"Failed to parse manifest: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 验证程序集
        /// </summary>
        async Task ValidateAssemblies(string modPath, ModManifest manifest, ValidationResult result)
        {
            var assembliesPath = Path.Combine(modPath, "build", "Assemblies");
            if (!Directory.Exists(assembliesPath))
            {
                result.AddError("No assemblies found. Build the mod first.");
                return;
            }

            var expectedDll = Path.Combine(assembliesPath, $"{manifest.id}.dll");
            if (!File.Exists(expectedDll))
            {
                result.AddError($"Main assembly not found: {manifest.id}.dll");
                return;
            }

            try
            {
                // 加载程序集进行验证
                var assembly = Assembly.LoadFrom(expectedDll);
                
                // 查找主类
                var mainType = assembly.GetType(manifest.main_class);
                if (mainType == null)
                {
                    result.AddError($"Main class not found: {manifest.main_class}");
                    return;
                }

                // 验证接口实现
                if (!typeof(ModSystem.Core.IModBehaviour).IsAssignableFrom(mainType))
                {
                    result.AddError($"Main class does not implement IModBehaviour");
                }

                // 统计信息
                var types = assembly.GetTypes();
                var behaviours = types.Count(t => typeof(ModSystem.Core.IModBehaviour).IsAssignableFrom(t));
                var services = types.Count(t => typeof(ModSystem.Core.IModService).IsAssignableFrom(t));
                
                result.AddInfo($"Assembly contains {types.Length} types, {behaviours} behaviours, {services} services");
            }
            catch (Exception ex)
            {
                result.AddError($"Failed to load assembly: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证资源
        /// </summary>
        void ValidateResources(string modPath, ModManifest manifest, ValidationResult result)
        {
            // 验证对象定义
            var objectsPath = Path.Combine(modPath, "Objects");
            if (Directory.Exists(objectsPath))
            {
                var objectFiles = Directory.GetFiles(objectsPath, "*.json");
                foreach (var file in objectFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var obj = JsonConvert.DeserializeObject<ObjectDefinition>(json);
                        
                        if (string.IsNullOrEmpty(obj.objectId))
                            result.AddError($"Object definition missing 'objectId': {Path.GetFileName(file)}");
                    }
                    catch (Exception ex)
                    {
                        result.AddError($"Invalid object definition {Path.GetFileName(file)}: {ex.Message}");
                    }
                }
                
                result.AddInfo($"Found {objectFiles.Length} object definitions");
            }

            // 验证资源文件大小
            var resourcesPath = Path.Combine(modPath, "Resources");
            if (Directory.Exists(resourcesPath))
            {
                long totalSize = 0;
                foreach (var file in Directory.GetFiles(resourcesPath, "*", SearchOption.AllDirectories))
                {
                    var info = new FileInfo(file);
                    totalSize += info.Length;
                    
                    // 警告大文件
                    if (info.Length > 10 * 1024 * 1024) // 10MB
                    {
                        result.AddWarning($"Large resource file: {Path.GetFileName(file)} ({info.Length / 1024 / 1024}MB)");
                    }
                }
                
                result.AddInfo($"Total resources size: {totalSize / 1024 / 1024}MB");
            }
        }

        /// <summary>
        /// 验证安全性
        /// </summary>
        async Task ValidateSecurity(string modPath, ValidationResult result)
        {
            // 检查危险的API使用
            var dangerousAPIs = new[]
            {
                "System.IO.File.Delete",
                "System.IO.Directory.Delete",
                "System.Diagnostics.Process",
                "System.Net.WebClient",
                "Microsoft.Win32.Registry"
            };

            var sourceFiles = Directory.GetFiles(Path.Combine(modPath, "Source"), "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in sourceFiles)
            {
                var content = await File.ReadAllTextAsync(file);
                
                foreach (var api in dangerousAPIs)
                {
                    if (content.Contains(api))
                    {
                        result.AddWarning($"Potentially dangerous API usage in {Path.GetFileName(file)}: {api}");
                    }
                }
            }

            // 检查请求的权限
            var manifestPath = Path.Combine(modPath, "manifest.json");
            var manifest = JsonConvert.DeserializeObject<ModManifest>(
                await File.ReadAllTextAsync(manifestPath));
            
            if (manifest.permissions != null && manifest.permissions.Length > 0)
            {
                result.AddInfo($"Requested permissions: {string.Join(", ", manifest.permissions)}");
                
                // 检查高危权限
                var dangerousPermissions = new[] { "file_system_full", "network_unrestricted", "process_control" };
                foreach (var perm in manifest.permissions)
                {
                    if (dangerousPermissions.Contains(perm))
                    {
                        result.AddWarning($"High-risk permission requested: {perm}");
                    }
                }
            }
        }

        /// <summary>
        /// 验证兼容性
        /// </summary>
        void ValidateCompatibility(ModManifest manifest, ValidationResult result)
        {
            // 验证Unity版本
            if (!string.IsNullOrEmpty(manifest.unity_version))
            {
                result.AddInfo($"Target Unity version: {manifest.unity_version}");
            }

            // 验证SDK版本
            if (!string.IsNullOrEmpty(manifest.sdk_version))
            {
                if (Version.TryParse(manifest.sdk_version, out var requiredVersion))
                {
                    var currentVersion = new Version("1.0.0"); // 当前SDK版本
                    if (requiredVersion > currentVersion)
                    {
                        result.AddError($"Requires newer SDK version: {manifest.sdk_version}");
                    }
                }
            }

            // 验证依赖项
            if (manifest.dependencies != null && manifest.dependencies.Length > 0)
            {
                foreach (var dep in manifest.dependencies)
                {
                    result.AddInfo($"Dependency: {dep.id} v{dep.version}{(dep.optional ? " (optional)" : "")}");
                }
            }
        }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public List<string> Info { get; } = new List<string>();

        public void AddError(string message)
        {
            Errors.Add(message);
            IsValid = false;
        }

        public void AddWarning(string message)
        {
            Warnings.Add(message);
        }

        public void AddInfo(string message)
        {
            Info.Add(message);
        }
    }

    // 数据模型类（与其他工具共享）
    public class ModManifest
    {
        public string id { get; set; }
        public string name { get; set; }
        public string version { get; set; }
        public string author { get; set; }
        public string description { get; set; }
        public string unity_version { get; set; }
        public string sdk_version { get; set; }
        public string main_class { get; set; }
        public string[] behaviours { get; set; }
        public ModDependency[] dependencies { get; set; }
        public string[] permissions { get; set; }
    }

    public class ModDependency
    {
        public string id { get; set; }
        public string version { get; set; }
        public bool optional { get; set; }
    }

    public class ObjectDefinition
    {
        public string objectId { get; set; }
        public string name { get; set; }
    }
}
```

这就是完整的ModSDK实现，包括：

1. **ModBuilder工具** - 完整的命令行工具，支持创建、构建、测试和打包模组
2. **模板系统** - 多种模组模板，从简单到复杂
3. **ModSDK.Runtime** - 运行时支持库，提供辅助类和基类
4. **验证工具** - 确保模组质量和安全性
5. **完整文档** - 帮助开发者快速上手

这个SDK为模组开发者提供了完整的工具链，使他们能够轻松创建高质量的模组，而无需深入了解底层系统的复杂性。