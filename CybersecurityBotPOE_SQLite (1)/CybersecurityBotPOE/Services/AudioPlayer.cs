using System;
using System.IO;
using System.Media;

namespace CybersecurityBotGUI.Services
{
    /// <summary>Plays the voice greeting WAV file when the application starts (Part 1 requirement).</summary>
    public class AudioPlayer
    {
        private readonly string _wavPath;

        public AudioPlayer()
        {
            _wavPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "greeting.wav");
        }

        public void PlayGreeting()
        {
            if (!File.Exists(_wavPath))
                return;

            try
            {
                var player = new SoundPlayer(_wavPath);
                player.LoadAsync();
                player.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[AudioPlayer] Could not play greeting: " + ex.Message);
            }
        }
    }
}
