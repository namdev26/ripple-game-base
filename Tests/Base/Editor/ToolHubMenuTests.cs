using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace RippleGame.Base.Tests
{
    public sealed class ToolHubMenuTests
    {
        private static Type[] TypesIn(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null).ToArray();
            }
        }

        [Test]
        public void Menu_HasOnlyOpenHub()
        {
            const BindingFlags anyStatic = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var items = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetName().Name.StartsWith("RippleGame.", StringComparison.Ordinal))
                .SelectMany(TypesIn)
                .SelectMany(type => type.GetMethods(anyStatic))
                .SelectMany(method => method.GetCustomAttributes<MenuItem>())
                .Select(item => item.menuItem)
                .Where(path => path.StartsWith("RIPPLE Game/", StringComparison.Ordinal));

            Assert.That(items, Is.EqualTo(new[] { "RIPPLE Game/Open Hub" }));
        }

        [TestCase("RippleGame.Ads.Editor.AdsBuildProfileUtility", "BuildNoAds")]
        [TestCase("RippleGame.Ads.Editor.AdsBuildProfileUtility", "BuildAdsTest")]
        [TestCase("RippleGame.Ads.Editor.AdsBuildProfileUtility", "BuildAdsProduction")]
        [TestCase("RippleGame.Ads.Editor.AdsBuildProfileUtility", "ConfigureNoAds")]
        [TestCase("RippleGame.Ads.Editor.AdsBuildProfileUtility", "ConfigureAdsTest")]
        [TestCase("RippleGame.Ads.Editor.AdsBuildProfileUtility", "ConfigureAdsProduction")]
        [TestCase("RippleGame.Ads.Editor.AdsConfigurationAssetMenu", "Create")]
        [TestCase("RippleGame.Ads.Editor.AdsSceneInstaller", "AddAdsObjectToActiveScene")]
        [TestCase("RippleGame.Analytics.Editor.AnalyticsMenu", "EnableFirebase")]
        [TestCase("RippleGame.Analytics.Editor.AnalyticsMenu", "DisableFirebase")]
        [TestCase("RippleGame.Analytics.Editor.AnalyticsMenu", "CreateSettings")]
        public void HubCommand_FindsItsMethod(string typeName, string methodName)
        {
            Assert.That(ToolHubCommands.Find(typeName, methodName), Is.Not.Null);
        }

        [Test]
        public void Search_FindsSdkSection()
        {
            Assert.That(ToolHubCatalog.Search("sdk").Select(section => section.Id), Does.Contain("sdk"));
        }

        [Test]
        public void HubCommand_UnknownMethod_IsNotFound()
        {
            Assert.That(ToolHubCommands.Find("RippleGame.Ads.Editor.AdsSceneInstaller", "NoSuchCommand"), Is.Null);
            Assert.That(ToolHubCommands.Find("No.Such.Type", "Create"), Is.Null);
        }
    }
}
