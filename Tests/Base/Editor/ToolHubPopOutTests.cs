using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace RippleGame.Base.Tests
{
    public sealed class ToolHubPopOutTests
    {
        private static bool HasGraphics => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        private static void RequireGraphics()
        {
            if (!HasGraphics)
                Assert.Ignore("Opening an editor window needs a graphics device.");
        }

        [TearDown]
        public void TearDown()
        {
            if (!HasGraphics)
                return;

            foreach (var window in Resources.FindObjectsOfTypeAll<ToolHubPageWindow>())
                window.Close();
            foreach (var window in Resources.FindObjectsOfTypeAll<ToolHubWindow>())
                window.Close();
        }

        private static ToolHubPageWindow[] OpenWindows() => Resources.FindObjectsOfTypeAll<ToolHubPageWindow>();

        [Test]
        public void CreatePage_WorksForEverySection()
        {
            foreach (var section in ToolHubCatalog.Sections)
                Assert.That(ToolHubCatalog.CreatePage(section.Id), Is.Not.Null, section.Id);
        }

        [Test]
        public void CreatePage_GivesEachPageItsOwnType()
        {
            var types = ToolHubCatalog.Sections.Select(section => ToolHubCatalog.CreatePage(section.Id).GetType()).ToArray();

            Assert.That(types.Distinct().Count(), Is.EqualTo(types.Length));
        }

        [Test]
        public void CreatePage_Sdk_ReturnsTheSdkPage()
        {
            var page = ToolHubCatalog.CreatePage("sdk");

            Assert.That(page, Is.Not.Null);
            Assert.That(page.GetType().Name, Is.EqualTo("SdkHubPage"));
        }

        [Test]
        public void Open_SdkPage_ShowsItsTitle()
        {
            RequireGraphics();

            ToolHubPageWindow.Open("sdk");

            Assert.That(OpenWindows().Length, Is.EqualTo(1));
            Assert.That(OpenWindows()[0].PageId, Is.EqualTo("sdk"));
            Assert.That(OpenWindows()[0].titleContent.text, Is.EqualTo("SDK"));
        }

        [Test]
        public void CreatePage_UnknownId_ReturnsNull()
        {
            Assert.That(ToolHubCatalog.CreatePage("nothing"), Is.Null);
        }

        [Test]
        public void Open_ShowsPageWithItsTitle()
        {
            RequireGraphics();

            ToolHubPageWindow.Open("save");

            Assert.That(OpenWindows().Length, Is.EqualTo(1));
            Assert.That(OpenWindows()[0].PageId, Is.EqualTo("save"));
            Assert.That(OpenWindows()[0].titleContent.text, Is.EqualTo("Save"));
        }

        [Test]
        public void Open_SamePageTwice_KeepsOneWindow()
        {
            RequireGraphics();

            ToolHubPageWindow.Open("save");
            ToolHubPageWindow.Open("save");

            Assert.That(OpenWindows().Length, Is.EqualTo(1));
        }

        [Test]
        public void Open_DifferentPages_OpenSeparateWindows()
        {
            RequireGraphics();

            ToolHubPageWindow.Open("save");
            ToolHubPageWindow.Open("ads");

            Assert.That(OpenWindows().Select(window => window.PageId), Is.EquivalentTo(new[] { "save", "ads" }));
        }

        [Test]
        public void Open_UnknownPage_OpensNothing()
        {
            RequireGraphics();

            ToolHubPageWindow.Open("nothing");

            Assert.That(OpenWindows(), Is.Empty);
        }

        [Test]
        public void HubButton_ShowsOnPagesOnly()
        {
            RequireGraphics();
            ToolHubWindow.Open();
            var hub = EditorWindow.GetWindow<ToolHubWindow>();
            hub.CreateGUI();
            var button = hub.rootVisualElement.Q<Button>("pop-out");

            hub.ShowPage("overview");
            Assert.That(button.style.display.value, Is.EqualTo(DisplayStyle.None));

            hub.ShowPage("save");
            Assert.That(button.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            hub.ShowPage("sdk");
            Assert.That(button.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(hub.rootVisualElement.Q("inline-sdk"), Is.Not.Null);
        }
    }
}
