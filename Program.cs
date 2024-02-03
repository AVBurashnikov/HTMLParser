using System.Text;
using htmlparser.Lexer;

namespace htmlParser
{
    class Program
    {
        public static void Main()
        {
            var startTime = Environment.TickCount;
            var sr = new StreamReader("file.html");
            var data = sr.ReadToEnd();
            
            var tokenizer = new Lexer();
            var tokens = tokenizer.Parse(data);

            if (tokenizer.Diagnostics.Count > 0 )
                foreach (var diagnostic in tokenizer.Diagnostics)
                    Console.WriteLine(diagnostic);

            foreach(var token in tokens)
                Console.WriteLine(token);

            Console.WriteLine("-------------------------");
            Console.WriteLine($"Program timing is: {Environment.TickCount - startTime}ms");
            Console.ReadKey();
        }
    }        
}
