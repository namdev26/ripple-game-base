using System;
using UnityEngine;

namespace RippleGame.UI
{
    /// <summary>Base lifecycle only. Each game supplies its own prefab and behavior.</summary>
    public class PopupView : MonoBehaviour
    {
        public virtual void Close()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        protected void InvokeAndClose(Action action)
        {
            if (action == null) return;
            Close();
            action.Invoke();
        }
    }
}
