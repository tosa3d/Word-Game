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

using UnityEngine;
using UnityEngine.Pool;

namespace WordsToolkit.Scripts.GUI.Labels
{
    public interface IAnimationSource
    {
        GameObject GetAnimatedObject();
        void ReleaseObject(GameObject animatedObject);
    }

    public class PooledAnimationSource : IAnimationSource
    {
        private readonly ObjectPool<GameObject> _pool;

        public PooledAnimationSource(GameObject prefab, Transform transform)
        {
            _pool = new ObjectPool<GameObject>(
                () => Object.Instantiate(prefab, transform),
                animatedObject => animatedObject.SetActive(true),
                animatedObject => animatedObject.SetActive(false),
                Object.Destroy
            );
        }

        public GameObject GetAnimatedObject()
        {
            return _pool.Get();
        }

        public void ReleaseObject(GameObject animatedObject)
        {
            _pool.Release(animatedObject);
        }
    }

    public class SingleObjectAnimationSource : IAnimationSource
    {
        private readonly GameObject _animatedObject;

        public SingleObjectAnimationSource(GameObject animatedObject)
        {
            _animatedObject = animatedObject;
        }

        public GameObject GetAnimatedObject()
        {
            return _animatedObject;
        }

        public void ReleaseObject(GameObject animatedObject)
        {
            animatedObject.SetActive(false);
        }
    }
}