using System;
using System.Collections.Generic;

namespace WordsToolkit.Scripts.Popups
{
    public interface IPopupStack
    {
        void Push(Popup popup);
        void Pop();
        Popup Peek();
        bool IsEmpty();
        void Clear();
        int Count { get; }
        event Action<Popup> OnPopupPushed;
        event Action<Popup> OnPopupPopped;
    }
} 