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
            string filePath = @"C:\Users\Alexey\Desktop\C#\htmlparser\bin\Debug\net8.0/file.html";
            var startTime = Environment.TickCount;
            
            var oak = Oak.GetOakInstance();

            FromFile fName = new FromFile(filePath);
            oak.Load(fName);
            oak.GrowTree();

            Console.WriteLine("-------------------------");
            Console.WriteLine($"Program timing is: {Environment.TickCount - startTime}ms");

            Console.ReadKey();
        }
    }        
}
