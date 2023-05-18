using Azure.AI.OpenAI;

namespace Open.Autom.AI.tion.Console;

internal class OpenAiGrantChecker
{
    private readonly OpenAIClient _openAiClient;
    private readonly OpenAiOptions _openAiOptions;

    public OpenAiGrantChecker(OpenAIClient openAiClient, OpenAiOptions openAiOptions)
    {
        _openAiClient = openAiClient;
        _openAiOptions = openAiOptions;
    }

    public async Task<bool?> GetGrantAsync(string question, string answer)
    {
        var response = await _openAiClient.GetCompletionsAsync(_openAiOptions.ModelName,
            new CompletionsOptions
            {
                Temperature = 0,
                ChoicesPerPrompt = 1,
                MaxTokens = 3,
                Echo = false,
                Prompts =
                {
                    $"Assume a user answers the question \"{question}\".\n" +
                    $"For the answer, provide one of the following labels: Agree, Disagree, Uncertain\n" +
                    $"Answer: {answer}" +
                    $"Label: "
                }
            });

        if (!response.HasValue || !response.Value.Choices.Any())
        {
            return null;
        }

        var choice = response.Value.Choices[0].Text.Trim();
        var agreeVote = choice.StartsWith("agree", StringComparison.InvariantCultureIgnoreCase);
        var disagreeVote = choice.StartsWith("disagree", StringComparison.InvariantCultureIgnoreCase);
        return agreeVote != disagreeVote ? agreeVote : null;
    }
}