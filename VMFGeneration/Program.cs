using System.IO;

namespace VMFConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            string directory = @"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\sdk_content\maps";
            if(args.Length > 0)
            {
                if(Directory.Exists(args[0]))
                {
                    directory = args[0];
                }
            }
            string vmfPath = directory + @"\GeneratedMap.vmf";

            Generator gen = new Generator();
            File.WriteAllText(vmfPath, gen.Generate(vmfPath));
        }
    }
}
