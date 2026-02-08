using System.IO;
using System.Linq;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace ArcCreate.Utility.Lua
{
    public class ScriptLoader : FileSystemScriptLoader
    {
        private readonly string[] paths;

        public ScriptLoader(params string[] paths)
        {
            this.paths = paths.Select(x => Path.GetFullPath(x)).ToArray();
            ModulePaths = new[] { "?", "?.lua" };
        }

        public override bool ScriptFileExists(string name)
        {
            foreach (var path in paths)
            {
                var fullPath = Path.GetFullPath(Path.Combine(path, name));
                if (!base.ScriptFileExists(fullPath)) continue;

                if (fullPath.Length < path.Length) continue;

                var match = true;
                for (var i = 0; i < path.Length; i++)
                    if (path[i] != fullPath[i])
                    {
                        match = false;
                        break;
                    }

                if (!match) continue;

                return true;
            }

            return false;
        }

        public override object LoadFile(string name, Table context)
        {
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, name);
                if (File.Exists(fullPath)) return base.LoadFile(fullPath, context);
            }

            throw new FileNotFoundException(name);
        }
    }
}