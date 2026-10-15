using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetSpeedMeter.Helpers;
using NetSpeedMeter.Models;

namespace NetSpeedMeter.Tests
{
    [TestClass]
    public class ThemeHelperTests
    {
        [TestMethod]
        public void ResolveIsDark_ExplicitDark_ReturnsTrue()
        {
            bool isDark = ThemeHelper.ResolveIsDark(AppTheme.Dark);
            Assert.IsTrue(isDark);
        }

        [TestMethod]
        public void ResolveIsDark_ExplicitLight_ReturnsFalse()
        {
            bool isDark = ThemeHelper.ResolveIsDark(AppTheme.Light);
            Assert.IsFalse(isDark);
        }

        [TestMethod]
        public void ResolveIsDark_SystemTheme_MatchesIsWindowsTaskbarDark()
        {
            bool expected = ThemeHelper.IsWindowsTaskbarDark();
            bool actual = ThemeHelper.ResolveIsDark(AppTheme.System);
            Assert.AreEqual(expected, actual);
        }
    }
}
