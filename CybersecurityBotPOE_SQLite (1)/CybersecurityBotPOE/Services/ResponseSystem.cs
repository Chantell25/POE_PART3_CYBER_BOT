using System;
using System.Collections.Generic;
using System.Linq;

namespace CybersecurityBotGUI.Services
{
    /// <summary>
    /// Handles general cybersecurity keyword recognition, random responses,
    /// and follow-up conversation flow. This is the Part 2 engine, kept
    /// unchanged so Part 1/2 functionality remains fully intact in Part 3.
    /// </summary>
    public class ResponseSystem
    {
        private readonly Random _rng = new Random();

        private readonly Dictionary<string, string> _single = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["how are you"] =
                "I am running perfectly and ready to help you stay safe online!",

            ["what is your purpose"] =
                "My purpose is to educate South African citizens about cybersecurity threats and how to stay protected online. " +
                "I cover phishing, passwords, privacy, malware, scams, safe browsing, 2FA, and VPNs.",

            ["2fa"] =
                "Two-Factor Authentication (2FA) adds a second layer of security to your accounts.\n\n" +
                "Even if your password is stolen, an attacker still cannot log in without the second factor.\n\n" +
                "Enable 2FA on your email, banking, and social media accounts immediately!",

            ["two-factor"] =
                "Two-factor authentication requires something you KNOW (password) AND something you HAVE (phone or hardware key).",

            ["vpn"] =
                "A VPN (Virtual Private Network) encrypts your internet traffic, hiding it from your ISP, hackers, " +
                "and anyone snooping on public Wi-Fi. Always use a reputable VPN at coffee shops, airports, or hotels.",

            ["browsing"] =
                "Safe Browsing Tips:\n" +
                "  - Look for HTTPS (padlock) before entering personal info\n" +
                "  - Keep your browser updated\n" +
                "  - Use ad-blockers to block malicious ads\n" +
                "  - Avoid downloading software from unofficial sources\n" +
                "  - Clear your cookies and cache regularly",

            ["social engineering"] =
                "Social engineering is when attackers manipulate people psychologically to reveal confidential information.\n\n" +
                "Common tactics: pretexting (fake scenarios), baiting (fake prizes), and vishing (phone scams).\n" +
                "Always verify the identity of anyone requesting sensitive information!",

            ["privacy"] =
                "Privacy Best Practices:\n" +
                "  - Review app permissions regularly\n" +
                "  - Use privacy-focused browsers like Firefox or Brave\n" +
                "  - Limit personal info on social media\n" +
                "  - Enable full-disk encryption on your devices",
        };

        private readonly Dictionary<string, List<string>> _random = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["phishing"] = new List<string>
            {
                "Phishing Tip: Legitimate organisations will NEVER ask for your password via email. Report any such request immediately!",
                "Phishing Tip: Always hover over links before clicking. The real URL often reveals deception - look for misspellings like 'paypa1.com'.",
                "Phishing Tip: Urgency is a red flag! Messages saying 'Your account closes in 24 hours!' are designed to make you panic.",
                "Phishing Tip: Check the sender's email address carefully. 'support@paypal-secure.com' is NOT PayPal.",
                "Phishing Tip: When in doubt, type the URL yourself instead of clicking any link in an email or SMS."
            },
            ["password"] = new List<string>
            {
                "Password Tip: Use a passphrase - four or more random words like 'correct-horse-battery-staple'.",
                "Password Tip: Never reuse passwords. If one site is breached, attackers try the same credentials everywhere.",
                "Password Tip: Avoid obvious substitutions like '@' for 'a'. Aim for true randomness with 12+ characters.",
                "Password Tip: Change passwords immediately after a data breach notification. Check haveibeenpwned.com.",
                "Password Tip: Do not use personal info in passwords - names and birthdays are easy to find on social media."
            },
            ["scam"] = new List<string>
            {
                "Scam Alert: 'You have won a prize!' messages are almost always scams.",
                "Scam Alert: Romance scammers build relationships online then request money for 'emergencies'.",
                "Scam Alert: Tech support scammers call pretending to be Microsoft. Real companies do not call unsolicited.",
                "Scam Alert: SIM-swap fraud is common in South Africa. Contact your network if your phone loses signal unexpectedly.",
                "Scam Alert: Invoice fraud changes legitimate invoice bank details. Always verify payments via a known phone number."
            },
            ["malware"] = new List<string>
            {
                "Malware Tip: Keep your OS and antivirus updated. Most malware exploits already-patched vulnerabilities.",
                "Malware Tip: Never open email attachments from unknown senders.",
                "Malware Tip: Use the 3-2-1 backup rule against ransomware - 3 copies, 2 media types, 1 offsite.",
                "Malware Tip: Pirated software often hides keyloggers or trojans."
            }
        };

        private readonly List<string> _followUps = new List<string>
        { "tell me more", "explain more", "another tip", "give me another", "go on", "what else", "more info" };

        private readonly List<string> _exits = new List<string>
        { "exit", "quit", "goodbye", "bye", "see you", "farewell" };

        /// <summary>Returns true if the input is a conversational exit request.</summary>
        public bool IsExit(string input) => _exits.Any(t => input.ToLower().Contains(t));

        /// <summary>Returns true if the input is a generic follow-up like "tell me more".</summary>
        public bool IsFollowUp(string input) => _followUps.Any(t => input.ToLower().Contains(t));

        /// <summary>Returns a random tip for a given topic key, or null if no match.</summary>
        public string? GetRandomForTopic(string topic) =>
            _random.TryGetValue(topic, out var list) ? list[_rng.Next(list.Count)] : null;

        /// <summary>Returns the single deterministic response for a topic, or null.</summary>
        public string? GetSingleForTopic(string topic) =>
            _single.TryGetValue(topic, out var resp) ? resp : null;

        /// <summary>
        /// Main entry point for general cybersecurity Q&A.
        /// Returns the bot's reply and updates newTopic for follow-up tracking.
        /// </summary>
        public string GetResponse(string input, string? lastTopic, out string? newTopic)
        {
            newTopic = lastTopic;
            string lower = input.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(lower))
                return "I did not catch that. Could you please rephrase?";

            if (IsFollowUp(lower))
            {
                if (!string.IsNullOrEmpty(lastTopic))
                {
                    string? r = GetRandomForTopic(lastTopic) ?? GetSingleForTopic(lastTopic);
                    if (r != null) return r;
                }
                return "What topic would you like to explore? Ask about phishing, password, scam, malware, privacy, 2fa, vpn, or browsing.";
            }

            string? interest = DetectInterest(lower);
            if (interest != null)
            {
                newTopic = interest;
                string firstTip = GetRandomForTopic(interest) ?? GetSingleForTopic(interest) ?? "";
                return "Great! I will remember that you are interested in " + interest + ". Here is a tip to start:\n\n" + firstTip;
            }

            foreach (var key in _random.Keys)
            {
                if (lower.Contains(key))
                {
                    newTopic = key;
                    return _random[key][_rng.Next(_random[key].Count)];
                }
            }

            foreach (var key in _single.Keys)
            {
                if (lower.Contains(key))
                {
                    newTopic = key;
                    return _single[key];
                }
            }

            return "__NOMATCH__";
        }

        private string? DetectInterest(string lower)
        {
            string[] phrases = { "i'm interested in", "i am interested in", "interested in", "i care about", "i want to learn about" };
            string[] topics  = { "phishing", "password", "passwords", "scam", "scams", "malware", "privacy", "vpn", "browsing", "2fa" };

            foreach (var phrase in phrases)
            {
                if (lower.Contains(phrase))
                {
                    foreach (var topic in topics)
                    {
                        if (lower.Contains(topic))
                            return topic.TrimEnd('s');
                    }
                }
            }
            return null;
        }
    }
}
