namespace FocusBlocker.Models
{
    /// <summary>
    /// Websites that are ALWAYS blocked, baked into the app. They don't appear
    /// in (or depend on) any of the user's lists, can't be edited or removed
    /// from the UI, and - unlike the user's own blocked sites - can't be
    /// unlocked by solving a challenge. They're blocked at all times while the
    /// app is running, not just during focus time or concentration mode, and
    /// they stay blocked even if the same site is on the concentration-mode
    /// allowed list.
    ///
    /// Add one root domain per line, WITHOUT "https://", "www." or any path -
    /// subdomains are covered automatically (listing "example.com" also blocks
    /// "www.example.com" and "m.example.com"). Changing this list means
    /// rebuilding the app, which is the point: end users can't touch it.
    /// </summary>
    public static class BuiltInBlocklist
    {
        public static readonly string[] Domains =
        {
            "pornhub.com",
            "xvideos.com",
            "ixxx.com",
            "xhaccess.com",
            "tube8.com",
            "eporner.com",
            "youjizz.com",
            "balkanjizz.com",
            "spankbang.com",
            "qorno.com",
            "redtube.com",
            "rule34video.com",
            "noodlemagazine.com",
            "pimpbunny.com",
            "xnxx.com",
            "fuq.com",
            "thepornjoy.com",
            "erome.com",
            "redgifs.com",
            "youporn.com",
            "rule34.xxx",
            "beeg.xxx"
        };
    }
}