using System;
using System.Threading.Tasks;

namespace WordsToolkit.Scripts.Services.IAP
{
    public interface IInitializeGamingServices
    {
        Task Initialize(Action onSuccess, Action<string> onError);
    }
}