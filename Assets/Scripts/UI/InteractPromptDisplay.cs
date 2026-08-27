namespace MonsterMiner.UI
{
    public static class InteractPromptDisplay
    {
        public const int PromptFontSize = 36;

        public static string FormatPrompt(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
                return prompt;

            string trimmed = prompt.TrimEnd();
            if (!trimmed.Contains("[E]"))
                return prompt;

            string body = trimmed.Replace(" [E]", "").Replace("[E]", "").Trim();
            while (body.Contains("  "))
                body = body.Replace("  ", " ");

            return string.IsNullOrEmpty(body) ? "[E]" : $"{body}\n[E]";
        }
    }
}
