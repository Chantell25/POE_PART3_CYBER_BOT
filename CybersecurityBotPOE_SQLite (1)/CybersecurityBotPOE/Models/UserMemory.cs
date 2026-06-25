using System.Collections.Generic;

namespace CybersecurityBotGUI.Models
{
    /// <summary>Stores information the bot remembers about the user (Part 2 requirement).</summary>
    public class UserMemory
    {
        public string Name            { get; set; } = string.Empty;
        public string? FavouriteTopic { get; set; }
        public string? LastTopic      { get; set; }
        public Dictionary<string, string> Facts { get; set; } = new Dictionary<string, string>();
    }
}
