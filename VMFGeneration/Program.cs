using System.IO;

namespace VMFConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            string vmfPath = @"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\sdk_content\maps\experiment3.vmf";

            Generator gen = new Generator();
            File.WriteAllText(vmfPath, gen.Generate(vmfPath));
        }
    }
}
