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
            
            var tokenizer = new Tokenizer();
            var tokens = tokenizer.Parse(data);

            /*int i = 0;
            int counter = 1;

            while (i < tokens.Count)
            {
                if (tokens[i].TagName == "a" && 
                    tokens[i].Kind.Equals(SyntaxKind.OpenTag) &&
                    tokens[i].Attributes.ContainsKey("href"))
                    {
                        Console.WriteLine("{0}: {1}", counter, tokens[i].Attributes["href"][0]);
                        Console.WriteLine(tokens[i + 1].Text);
                        Console.WriteLine("----------");
                        counter++;
                    }
                i++;
            }*/

            foreach (var token in tokens)
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
