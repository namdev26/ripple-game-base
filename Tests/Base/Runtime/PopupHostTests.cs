using System;
using System.Collections;
using NUnit.Framework;
using RippleGame.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.Base.Tests
{
    public sealed class PopupHostTests
    {
        private GameObject hostObject;
        private GameObject template;

        [SetUp]
        public void SetUp()
        {
            hostObject = new GameObject("Popup host test");
            template = new GameObject("Caller supplied popup");
            template.AddComponent<PopupView>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(hostObject);
            UnityEngine.Object.DestroyImmediate(template);
        }

        [UnityTest]
        public IEnumerator ReplacingPopupDeactivatesOldViewAndDestroysItAtFrameEnd()
        {
            var host = hostObject.AddComponent<PopupHost>();
            var first = host.Show(template.GetComponent<PopupView>());
            var second = host.Show(template.GetComponent<PopupView>());
            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(host.Active, Is.SameAs(second));
            Assert.That(second.transform.parent, Is.SameAs(host.transform));
            yield return null;
            Assert.That(first == null, Is.True);
            host.CloseActive();
            Assert.That(host.Active == null, Is.True);
            yield return null;
            Assert.That(second == null, Is.True);
        }

        [UnityTest]
        public IEnumerator MissingPrefabDoesNotCloseExistingPopup()
        {
            var host = hostObject.AddComponent<PopupHost>();
            var first = host.Show(template.GetComponent<PopupView>());
            Assert.Throws<ArgumentNullException>(() => host.Show<PopupView>(null));
            Assert.That(host.Active, Is.SameAs(first));
            Assert.That(first.gameObject.activeSelf, Is.True);
            host.CloseActive();
            yield return null;
        }

        [UnityTest]
        public IEnumerator InactivePrefabIsShown()
        {
            var host = hostObject.AddComponent<PopupHost>();
            template.SetActive(false);
            var instance = host.Show(template.GetComponent<PopupView>());
            Assert.That(instance.gameObject.activeSelf, Is.True);
            Assert.That(template.activeSelf, Is.False);
            host.CloseActive();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CloseCallbackCanOpenTrackedNextPopup()
        {
            var host = hostObject.AddComponent<PopupHost>();
            var first = host.Show(template.GetComponent<PopupView>());
            first.gameObject.AddComponent<OpenPopupOnDisable>().Configure(host, template.GetComponent<PopupView>());
            host.CloseActive();
            Assert.That(host.Active == null, Is.False);
            Assert.That(host.Active, Is.Not.SameAs(first));
            Assert.That(host.GetComponentsInChildren<PopupView>().Length, Is.EqualTo(1));
            host.CloseActive();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReplacementCloseCallbackKeepsOnlyTheNewestPopup()
        {
            var host = hostObject.AddComponent<PopupHost>();
            var first = host.Show(template.GetComponent<PopupView>());
            first.gameObject.AddComponent<OpenPopupOnDisable>().Configure(host, template.GetComponent<PopupView>());
            var superseded = host.Show(template.GetComponent<PopupView>());
            Assert.That(host.Active == null, Is.False);
            Assert.That(host.Active, Is.Not.SameAs(superseded));
            Assert.That(superseded.gameObject.activeSelf, Is.False);
            Assert.That(host.GetComponentsInChildren<PopupView>().Length, Is.EqualTo(1));
            host.CloseActive();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExternalContainerIsRespectedAndClosedWithHost()
        {
            var host = hostObject.AddComponent<PopupHost>();
            host.Container = template.transform;
            var instance = host.Show(template.GetComponent<PopupView>());
            Assert.That(instance.transform.parent, Is.SameAs(template.transform));
            UnityEngine.Object.Destroy(hostObject);
            yield return null;
            yield return null;
            Assert.That(instance == null, Is.True);
        }
    }
}
