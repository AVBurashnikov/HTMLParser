using YellowOak.TreeBuilder;

namespace YellowOak
{
    internal sealed class Program
    {
        /// <summary>
        /// Entry point for testing app
        /// </summary>
        public static void Main()
        {
            string file = @"C:\Users\Alexey\Desktop\C#\htmlparser\bin\Debug\net8.0\file.html";
            var startTime = Environment.TickCount;
            
            var oak = Oak.GetInstance();

            FromFile fName = new FromFile(file);
            oak.Load(fName);
            oak.Grow();

            //List<Node> nodes = oak.GetNodesWithTag("span");

            //Node node = oak.GetNodeWithAttr("id", "shs_min_pdv");

            List<Node> nodes = oak.GetNodesWithAttr("class", "opsn_inst_mnsl");

            foreach (Node node in nodes) 
            { 
                Console.WriteLine(node);
            }

            Console.WriteLine("-------------------------");
            Console.WriteLine($"Program timing is: {Environment.TickCount - startTime}ms");

            Console.ReadKey();
        }
    }        
}
