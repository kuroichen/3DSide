using TMPro;
using UnityEngine;

namespace Rail
{
    public sealed class LiveView : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI livesText = null;

        public void SetLives(int lives)
        {
            this.livesText.SetText($"x{lives}");
        }
    }
}
