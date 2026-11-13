using UnityEngine;

namespace RippleGame.Ads
{
    /// <summary>Pauses game time and audio while a full-screen ad shows, then restores the values it found.</summary>
    internal sealed class FullScreenPause
    {
        private bool _active;
        private float _savedTimeScale;
        private bool _savedAudioPause;

        internal bool IsActive => _active;

        internal void Begin()
        {
            if (_active)
            {
                return;
            }

            _savedTimeScale = Time.timeScale;
            _savedAudioPause = AudioListener.pause;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            _active = true;
        }

        internal void End()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            Time.timeScale = _savedTimeScale;
            AudioListener.pause = _savedAudioPause;
        }
    }
}
