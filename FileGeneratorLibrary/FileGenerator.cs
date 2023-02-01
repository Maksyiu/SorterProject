using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FileGeneratorLibrary
{
    public class FileGenerator
    {
        public async Task Generate(string destination, long maxFileSize ,CancellationToken cancellation)
        {
            long currentFileSize = 0L;
            var baseData = Environment.CurrentDirectory + @"\GeneratorBase.txt";
            var input = await File.ReadAllLinesAsync(baseData);

            if (!Directory.Exists(Path.GetDirectoryName(destination)))
            {
                throw new Exception("Directory not exists");
            }

            StreamWriter writer = new StreamWriter(destination);

            try
            {            
                foreach (var item in input)
                {
                    await writer.WriteLineAsync(item);
                    await writer.WriteLineAsync(item);
                }

                List<string> genList = new List<string>();

                foreach (var item in input)
                {
                    currentFileSize += item.Length;
                    var temp = item.Split(". ");
                    genList.Add(temp[1]);
                }

                string output = "";
                Random random = new Random();

                while (currentFileSize < maxFileSize)
                {
                    var itemNumber = random.Next(genList.Count);

                    output = $"{random.Next()}. {genList[itemNumber]}";
                    currentFileSize += output.Length + 2;

                    await writer.WriteLineAsync(output);
                }
            }
            finally 
            { 
                writer.Close(); 
            }
        }           
    }
}
