using YellowOak.LexicAnalisys;

namespace YellowOak
{
    class Program
    {
        public static void Main()
        {
            var startTime = Environment.TickCount;

            var sr = new StreamReader("file.html");
            var data = sr.ReadToEnd();
            
            var tokenizer = new Tokenizer();
            var tokens = tokenizer.Parse(data);


            foreach(var token in tokens)
                Console.WriteLine(token);

            Console.WriteLine("-------------------------");
            Console.WriteLine($"Program timing is: {Environment.TickCount - startTime}ms");
            if (tokenizer.Diagnostics.Count > 0 )
            {
                Console.WriteLine($"Diagnostics: {tokenizer.Diagnostics.Count} warnings");

                foreach (var diagnostic in tokenizer.Diagnostics)
                    Console.WriteLine(diagnostic);
            }
            Console.ReadKey();
        }
    }        
}
