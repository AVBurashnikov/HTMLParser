using YellowOak.HTMLLexicAnalisys;

namespace YellowOak
{
    class Program
    {
        public static void Main()
        {
            var startTime = Environment.TickCount;

            var sr = new StreamReader("file.html");
            var data = sr.ReadToEnd();
            int length = data.Length;
            
            var tokenizer = new Tokenizer();
            var tokens = tokenizer.Parse(data);

            /*int i = 0;
            int counter = 1;

            while (i < tokens.Count)
            {
                if ((tokens[i].Kind.Equals(SyntaxKind.OpenTag) ||
                    tokens[i].Kind.Equals(SyntaxKind.AutoClosingTag) || 
                    tokens[i].Attributes is not null) &&
                    tokens[i].TagName.Equals("a"))
                {
                    var href = tokens[i].Attributes.GetAttribute("href");
                    if (href is not null)
                    {
                        Console.WriteLine("{0}: Link '{1}'", counter, href.Value);
                        counter++;
                    }
                }
                i++;
            }*/

            foreach (var token in tokens)
                Console.WriteLine(token);
      
            Console.WriteLine("-------------------------");
            Console.WriteLine($"Program timing is: {Environment.TickCount - startTime}ms");
            Console.WriteLine($"Chars in markup: {length}");
            Console.WriteLine($"Tokens count: {tokens.Count}");
            Console.WriteLine($"Warnings count: {tokenizer.Diagnostics.Count}");
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
