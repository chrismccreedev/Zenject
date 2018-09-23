using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Zenject.ReflectionBaking
{
    public class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1 && args.Length != 2)
            {
                Console.WriteLine("Usage: {0} [Input Path] [Output Path]", System.AppDomain.CurrentDomain.FriendlyName);
                return 1;
            }

            var inputPath = args[0];

            string outputPath;

            if (args.Length == 2)
            {
                outputPath = args[1];
            }
            else
            {
                outputPath = inputPath;
            }

            if (!File.Exists(inputPath))
            {
                Console.WriteLine("Invalid path to dll provided: '{0}'", inputPath);
                return 1;
            }

            var assembly = Assembly.Load(File.ReadAllBytes(inputPath));

            var readerParams = new ReaderParameters()
            {
                ReadSymbols = true,
                AssemblyResolver = new CustomAssemblyResolver(Path.GetDirectoryName(inputPath)),
            };

            var module = ModuleDefinition.ReadModule(inputPath, readerParams);

            var weaver = new ReflectionBakingCodeWeaver(module);

            int numChanges = 0;

            foreach (var typeDef in module.LoopupAllTypes())
            {
                var actualType = typeDef.TryGetActualType(assembly);

                if (actualType == null)
                {
                    Console.WriteLine("Could not find type '{0}'", typeDef.FullName);
                    continue;
                }

                if (weaver.EditType(typeDef, actualType))
                {
                    numChanges++;
                }
            }

            Console.WriteLine("Changed {0} types in assembly '{1}'", numChanges, module.Name);

            var writerParams = new WriterParameters()
            {
                WriteSymbols = true
            };

            module.Write(outputPath, writerParams);

            Console.WriteLine("Updated file '{0}'", outputPath);

            return 0;
        }
    }
}
