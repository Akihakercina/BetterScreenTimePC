using System;
using System.Collections.Generic;

namespace FocusBlocker.Models
{
    /// <summary>Represents one website/domain the user has chosen to block or allow.</summary>
    public class BlockedWebsite
    {
        /// <summary>Root domain, e.g. "youtube.com" - no protocol, no "www.", no path.</summary>
        public string Domain { get; set; }

        public BlockedWebsite() { }

        public BlockedWebsite(string domain)
        {
            Domain = NormalizeDomain(domain);
        }

        /// <summary>Strips protocol, "www.", and any trailing path so we get a clean root domain.</summary>
        public static string NormalizeDomain(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string domain = input.Trim().ToLowerInvariant();
            domain = domain.Replace("https://", "").Replace("http://", "");

            int slashIndex = domain.IndexOf('/');
            if (slashIndex >= 0)
                domain = domain.Substring(0, slashIndex);

            if (domain.StartsWith("www."))
                domain = domain.Substring(4);

            return domain;
        }

        public IEnumerable<string> GetHostVariants()
        {
            if (string.IsNullOrWhiteSpace(Domain))
                yield break;

            yield return Domain;
            yield return "www." + Domain;
        }

        public override string ToString() => Domain;
    }
}