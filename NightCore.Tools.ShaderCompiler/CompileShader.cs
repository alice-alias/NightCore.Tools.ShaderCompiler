using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpDX.D3DCompiler;
using System.Collections.Generic;
using System.IO;

namespace NightCore.Tools.ShaderCompiler
{
    public class CompileShader : Task
    {
        [Required]
        public ITaskItem[] InputFiles { get; set; }

        [Required]
        public string OutputPath { get; set; }

        [Output]
        public ITaskItem[] OutputFiles { get; private set; }

        static bool IsAbsolutePath(string path)
            => path.StartsWith("\\") || path.Contains(":\\");

        public override bool Execute()
        {
            Log.LogMessage($"OutputPath: {OutputPath}");
            var list = new List<ITaskItem>();
            foreach (var file in InputFiles)
            {
                if (file.ItemSpec.Length > 0)
                {
                    Log.LogMessage(file.ItemSpec);
                    var profile = file.GetMetadata("Profile");
                    if (string.IsNullOrEmpty(profile))
                    {
                        Log.LogError($"{file.ItemSpec}: CompileShader には Profile が必要です。");
                    }
                    var entrypoint = file.GetMetadata("Entrypoint");
                    if (string.IsNullOrEmpty(profile))
                    {
                        Log.LogError($"{file.ItemSpec}: CompileShader には Entrypoint が必要です。");
                    }

                    var outfile = file.GetMetadata("OutputFileName");
                    if (string.IsNullOrEmpty(outfile))
                    {
                        outfile = OutputPath +
                            Path.Combine(Path.GetDirectoryName(file.ItemSpec), Path.GetFileNameWithoutExtension(file.ItemSpec) + "." + profile);
                    }
                    else if (!IsAbsolutePath(outfile))
                    {
                        outfile = OutputPath + outfile;
                    }
                    Log.LogMessage($"{file.ItemSpec}: {profile}, {entrypoint}, {outfile}");
                    try
                    {
                        using (var result = ShaderBytecode.CompileFromFile(file.ItemSpec, entrypoint, profile))
                        {
                            if (result.HasErrors)
                            {
                                Log.LogError(result.Message);
                            }
                            else
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(outfile));
                                using (var stream = File.Create(outfile))
                                    result.Bytecode.Save(stream);
                            }
                        }
                    }
                    catch (SharpDX.CompilationException ex)
                    {
                        Log.LogError($"{file.ItemSpec}[{profile}, {entrypoint}]: {ex.Message}");
                    }
                    list.Add(new TaskItem(outfile));
                }
            }
            OutputFiles = list.ToArray();
            return !Log.HasLoggedErrors;
        }
    }
}
