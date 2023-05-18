namespace Open.Autom.AI.tion.Console;

internal class InteractiveGrantChecker
{
    private readonly OpenAiGrantChecker _openAiGrantChecker;

    public InteractiveGrantChecker(OpenAiGrantChecker openAiGrantChecker)
    {
        _openAiGrantChecker = openAiGrantChecker;
    }

    public async Task<bool> GetAsync(string question, bool? require)
    {
        while (true)
        {
            System.Console.Write(" > ");

            var response = System.Console.ReadLine()!;

            var grant = await _openAiGrantChecker.GetGrantAsync(question, response);

            if (grant == null)
            {
                System.Console.WriteLine(" 😯 I'm sorry, I don't understand your answer...");
                continue;
            }

            if (require == null) return grant.Value;

            if (grant == require)
            {
                return grant.Value;
            }

            System.Console.WriteLine(" 😌 Actually, I need the grant...");
        }
    }
}