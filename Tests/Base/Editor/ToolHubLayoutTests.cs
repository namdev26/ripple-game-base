using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace RippleGame.Base.Tests
{
    public sealed class ToolHubLayoutTests
    {
        private static string[] LabelsIn(VisualElement root) =>
            root.Query<Label>().ToList().Select(label => label.text).ToArray();

        [Test]
        public void Overview_ShowsOnlySectionTitles()
        {
            var overview = ToolHubOverview.Create(string.Empty, _ => { });

            Assert.That(LabelsIn(overview), Is.EquivalentTo(ToolHubCatalog.Sections.Select(section => section.Title)));
        }

        [Test]
        public void Overview_KeepsDescriptionsAsTooltips()
        {
            var overview = ToolHubOverview.Create(string.Empty, _ => { });

            foreach (var section in ToolHubCatalog.Sections)
                Assert.That(overview.Q<Button>("card-" + section.Id).tooltip, Is.EqualTo(section.Description));
        }

        [Test]
        public void Overview_NoMatch_SaysSo()
        {
            var overview = ToolHubOverview.Create("no tool has this name", _ => { });

            Assert.That(LabelsIn(overview), Is.EqualTo(new[] { "No matching tools." }));
        }

        [Test]
        public void Navigation_ShowsOneBrandLine()
        {
            var parent = new VisualElement();
            var navigation = new ToolHubNavigation(parent, _ => { });
            navigation.Update("overview", string.Empty);

            var labels = LabelsIn(parent);
            Assert.That(labels, Does.Contain("RIPPLE Game base by Namdev26"));
            Assert.That(labels, Does.Contain("Overview"));
            Assert.That(labels, Has.None.Contains("WORKSPACE").And.None.Contains("Portable").And.None.EqualTo("TOOLS"));
        }
    }
}
