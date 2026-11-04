using System;
using System.Collections;
using NUnit.Framework;
using RippleGame.Scenes;
using RippleGame.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.Base.Tests
{
    public sealed class ScenesAndSafeAreaTests
    {
        [Test]
        public void Load_UnknownScene_ThrowsClearError()
        {
            var error = Assert.Throws<ArgumentException>(() => SceneLoader.Load("NoSuchScene"));
            Assert.That(error.Message, Does.Contain("NoSuchScene"));
        }

        [Test]
        public void LoadRoutine_UnknownScene_ThrowsClearError()
        {
            var routine = SceneLoader.LoadRoutine("NoSuchScene", 0f, null);
            var error = Assert.Throws<ArgumentException>(() => routine.MoveNext());
            Assert.That(error.Message, Does.Contain("NoSuchScene"));
        }

        [UnityTest]
        public IEnumerator SafeArea_MatchesScreenSafeArea()
        {
            var host = new GameObject("Safe area test", typeof(RectTransform));
            try
            {
                host.AddComponent<SafeArea>();
                yield return null;

                var area = Screen.safeArea;
                var rect = (RectTransform)host.transform;
                Assert.That(rect.anchorMin.x, Is.EqualTo(area.xMin / Screen.width).Within(0.001f));
                Assert.That(rect.anchorMin.y, Is.EqualTo(area.yMin / Screen.height).Within(0.001f));
                Assert.That(rect.anchorMax.x, Is.EqualTo(area.xMax / Screen.width).Within(0.001f));
                Assert.That(rect.anchorMax.y, Is.EqualTo(area.yMax / Screen.height).Within(0.001f));
                Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));
            }
            finally
            {
                UnityEngine.Object.Destroy(host);
            }
        }
    }
}
