using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace RippleGame.Base.Tests
{
    public sealed class ToolHubMenuTests
    {
        [TearDown]
        public void TearDown()
        {
            if (UnityEngine.SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;

            foreach (var window in UnityEngine.Resources.FindObjectsOfTypeAll<ToolHubWindow>())
                window.Close();
        }

        private static void RequireGraphics()
        {
            if (UnityEngine.SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Opening an editor window needs a graphics device.");
        }

        [TestCase("RIPPLE Game/Localization")]
        [TestCase("RIPPLE Game/Save")]
        [TestCase("RIPPLE Game/Update")]
        public void Menu_HasPageItem(string path)
        {
            var items = typeof(ToolHubWindow)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .SelectMany(method => method.GetCustomAttributes<MenuItem>())
                .Select(item => item.menuItem);

            Assert.That(items, Does.Contain(path));
        }

        [TestCase("localization")]
        [TestCase("save")]
        [TestCase("update")]
        public void OpenPage_SelectsPage(string id)
        {
            RequireGraphics();

            ToolHubWindow.OpenPage(id);

            Assert.That(EditorWindow.GetWindow<ToolHubWindow>().SelectedPage, Is.EqualTo(id));
        }

        [Test]
        public void OpenPage_WhenAlreadyOpen_SwitchesPage()
        {
            RequireGraphics();

            ToolHubWindow.OpenPage("save");

            ToolHubWindow.OpenPage("update");

            Assert.That(UnityEngine.Resources.FindObjectsOfTypeAll<ToolHubWindow>().Length, Is.EqualTo(1));
            Assert.That(EditorWindow.GetWindow<ToolHubWindow>().SelectedPage, Is.EqualTo("update"));
        }
    }
}
