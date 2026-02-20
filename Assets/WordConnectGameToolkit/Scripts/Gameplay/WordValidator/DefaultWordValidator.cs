// // ©2015 - 2025 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.NLP;

namespace WordsToolkit.Scripts.Gameplay.WordValidator
{
    public class DefaultWordValidator : IWordValidator
    {
        private readonly IModelController modelController;
        private readonly ICustomWordRepository customWordRepository;
        private readonly Level levelData;

        public DefaultWordValidator(IModelController modelController, ICustomWordRepository customWordRepository, Level levelData)
        {
            this.modelController = modelController;
            this.customWordRepository = customWordRepository;
            this.levelData = levelData;
        }

        public bool IsWordKnown(string word, string currentLanguage)
        {
            if (string.IsNullOrEmpty(word))
                return false;

            return (modelController != null && modelController.IsWordKnown(word, currentLanguage)) ||
                   (customWordRepository != null && customWordRepository.ContainsWord(word, currentLanguage));
        }

        public bool IsExtraWordValid(string word, string language)
        {
            return customWordRepository.AddExtraWord(word);
        }
    }
}