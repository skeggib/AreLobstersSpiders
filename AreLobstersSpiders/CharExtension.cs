namespace AreLobstersSpiders
{
    public static class CharExtension
    {
        public static bool IsVowel(this char c)
        {
            return c == 'a' || c == 'e' || c == 'y' || c == 'u' || c == 'i' || c == 'o';
        }
    }
}