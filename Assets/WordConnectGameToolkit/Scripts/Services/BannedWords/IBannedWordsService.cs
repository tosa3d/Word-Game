using System.Collections.Generic;

namespace WordsToolkit.Scripts.Services.BannedWords
{
    public interface IBannedWordsService
    {
        List<string> GetBannedWords(string languageCode);
        bool IsWordBanned(string word, string languageCode);
        void AddBannedWord(string word, string languageCode);
        void RemoveBannedWord(string word, string languageCode);
    }
}
