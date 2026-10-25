using System.Collections;
using NUnit.Framework;
using RippleGame.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.Base.Tests
{
    public sealed class PopupActivationTests
    {
        private GameObject hostObject;
        private GameObject firstTemplate;
        private GameObject nextTemplate;

        [SetUp]
        public void SetUp()
        {
            hostObject = new GameObject("Activation test host");
            firstTemplate = new GameObject("First template");
            firstTemplate.AddComponent<PopupView>();
            nextTemplate = new GameObject("Next template");
            nextTemplate.AddComponent<PopupView>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(hostObject);
            Object.DestroyImmediate(firstTemplate);
            Object.DestroyImmediate(nextTemplate);
        }

        [UnityTest]
        public IEnumerator OnEnableCanOpenANewerTrackedPopup()
        {
            var host = hostObject.AddComponent<PopupHost>();
            firstTemplate.AddComponent<OpenPopupOnEnable>().Configure(host, nextTemplate.GetComponent<PopupView>());
            var superseded = host.Show(firstTemplate.GetComponent<PopupView>());
            Assert.That(host.Active == null, Is.False);
            Assert.That(host.Active, Is.Not.SameAs(superseded));
            Assert.That(host.GetComponentsInChildren<PopupView>().Length, Is.EqualTo(1));
            host.CloseActive();
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnEnableCanCloseItsOwnInstance()
        {
            var host = hostObject.AddComponent<PopupHost>();
            firstTemplate.AddComponent<OpenPopupOnEnable>().Configure(host, null);
            var instance = host.Show(firstTemplate.GetComponent<PopupView>());
            Assert.That(host.Active == null, Is.True);
            Assert.That(host.GetComponentsInChildren<PopupView>().Length, Is.Zero);
            yield return null;
            Assert.That(instance == null, Is.True);
        }
    }
}
