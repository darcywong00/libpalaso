// Copyright (c) 2013, SIL International.
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).

using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.WritingSystems.WindowsForms.Keyboarding;
using SIL.WritingSystems.WindowsForms.Keyboarding.Types;

namespace SIL.WritingSystems.WindowsForms.Tests.Keyboarding
{
	[TestFixture]
	public class KeyboardControllerTests
	{
		[SetUp]
		public void Setup()
		{
			KeyboardController.Initialize();
		}

		[TearDown]
		public void TearDown()
		{
			KeyboardController.Shutdown();
		}

		[Test]
		public void CreateKeyboardDefinition_ExistingKeyboard_ReturnsReference()
		{
			var inputLanguage = new InputLanguageWrapper(new CultureInfo("en-US"), IntPtr.Zero, "foo");
			var expectedKeyboard = new KeyboardDescription("foo - English (US)", "foo", "en-US", inputLanguage,
				KeyboardController.Adaptors[0]);
			KeyboardController.Manager.RegisterKeyboard(expectedKeyboard);
			var keyboard = Keyboard.Controller.CreateKeyboardDefinition("foo", "en-US");
			Assert.That(keyboard, Is.SameAs(expectedKeyboard));
			Assert.That(keyboard, Is.TypeOf<KeyboardDescription>());
			Assert.That(((KeyboardDescription) keyboard).InputLanguage, Is.EqualTo(inputLanguage));
		}

		[Test]
		public void GetKeyboard_FromInputLanguage_ExistingKeyboardReturnsKeyboard()
		{
			var keyboard = Keyboard.Controller.CreateKeyboardDefinition("foo", "en-US");
			KeyboardController.Manager.RegisterKeyboard(keyboard);

			var inputLanguage = new InputLanguageWrapper(new CultureInfo("en-US"), IntPtr.Zero, "foo");
			Assert.That(Keyboard.Controller.GetKeyboard(inputLanguage), Is.EqualTo(keyboard));
		}

		[Test]
		public void GetKeyboard_FromNewPalasoId_ExistingKeyboardReturnsKeyboard()
		{
			var keyboard = Keyboard.Controller.CreateKeyboardDefinition("foo", "en-US");
			KeyboardController.Manager.RegisterKeyboard(keyboard);

			Assert.That(Keyboard.Controller.GetKeyboard("en-US_foo"), Is.EqualTo(keyboard));
		}

		[Test]
		public void GetKeyboard_FromNewPalasoId_NonExistingKeyboard()
		{
			var keyboard = Keyboard.Controller.CreateKeyboardDefinition("foo", "en-US");
			KeyboardController.Manager.RegisterKeyboard(keyboard);

			Assert.That(Keyboard.Controller.GetKeyboard("en-US_glop"), Is.EqualTo(KeyboardDescription.Zero));
		}

		[Test]
		public void GetKeyboard_FromOldParatextId_ExistingKeyboardReturnsKeyboard()
		{
			// This is the case of a keyboard that the old Palaso system was incapable of supporting.
			var keyboardFooBoo = Keyboard.Controller.CreateKeyboardDefinition("foo", "az-Latn-AZ");
			KeyboardController.Manager.RegisterKeyboard(keyboardFooBoo);

			var keyboard = Keyboard.Controller.CreateKeyboardDefinition("foo", "en-US");
			KeyboardController.Manager.RegisterKeyboard(keyboard);

			Assert.That(Keyboard.Controller.GetKeyboard("foo|en-US"), Is.EqualTo(keyboard));
			Assert.That(Keyboard.Controller.GetKeyboard("foo|az-Latn-AZ"), Is.EqualTo(keyboardFooBoo));
		}

		[Test]
		public void GetKeyboard_FromOldParatextId_NonExistingKeyboard()
		{
			var keyboard = Keyboard.Controller.CreateKeyboardDefinition("foo", "en-US");
			KeyboardController.Manager.RegisterKeyboard(keyboard);

			Assert.That(Keyboard.Controller.GetKeyboard("glop|en-US"), Is.EqualTo(KeyboardDescription.Zero));
		}

		[Test]
		public void GetKeyboard_FromOldPalasoId_ExistingKeyboardReturnsKeyboard()
		{
			// This demonstrates the case of a keyboard that the old Palaso system was incapable of supporting.
			// There's no way to reference this keyboard, or is there???
			var keyboardFooBoo = Keyboard.Controller.CreateKeyboardDefinition("foo", "az-Latn-AZ");
			KeyboardController.Manager.RegisterKeyboard(keyboardFooBoo);

			var keyboardFooF = Keyboard.Controller.CreateKeyboardDefinition("foo-az", "en-US");
			KeyboardController.Manager.RegisterKeyboard(keyboardFooF);
			var keyboardFoo = Keyboard.Controller.CreateKeyboardDefinition("foo", "en-US");
			KeyboardController.Manager.RegisterKeyboard(keyboardFoo);

			Assert.That(Keyboard.Controller.GetKeyboard("foo-az-en-US"), Is.EqualTo(keyboardFooF));
			Assert.That(Keyboard.Controller.GetKeyboard("foo-en-US"), Is.EqualTo(keyboardFoo));
			Assert.That(Keyboard.Controller.GetKeyboard("foo-az-Latn-AZ"), Is.EqualTo(keyboardFooBoo));
		}

		[Test]
		public void GetKeyboard_FromOldPalasoId_NonExistingKeyboard()
		{
			var keyboard = Keyboard.Controller.CreateKeyboardDefinition("foo", "en-US");
			KeyboardController.Manager.RegisterKeyboard(keyboard);

			Assert.That(Keyboard.Controller.GetKeyboard("glop-en-US"), Is.EqualTo(KeyboardDescription.Zero));
		}

		[Test]
		public void GetKeyboard_FromInputLanguage_NonExistingKeyboard()
		{
			var inputLanguage = new InputLanguageWrapper(new CultureInfo("en-US"), IntPtr.Zero, "foo");
			Assert.That(Keyboard.Controller.GetKeyboard(inputLanguage), Is.EqualTo(KeyboardDescription.Zero));
		}

		[Test]
		public void DefaultForWritingSystem_NullInput_ReturnsSystemDefault()
		{
			Assert.That(Keyboard.Controller.DefaultForWritingSystem(null),
				Is.EqualTo(KeyboardController.Adaptors[0].DefaultKeyboard));
		}

		[Test]
		public void DefaultForWritingSystem_NoLegacyKeyboardSet_ReturnsSystemDefault()
		{
			var ws = new WritingSystemDefinition();
			Assert.That(Keyboard.Controller.DefaultForWritingSystem(ws),
				Is.EqualTo(KeyboardController.Adaptors[0].DefaultKeyboard));
		}

		[Test]
		public void DefaultForWritingSystem_OldPalasoWinIMEKeyboard()
		{
			var inputLanguage = new InputLanguageWrapper(new CultureInfo("en-US"), IntPtr.Zero, "foo");
			var expectedKeyboard = new KeyboardDescription("foo - English (US)", "foo", "en-US", inputLanguage,
				KeyboardController.Adaptors[0]);
			KeyboardController.Manager.RegisterKeyboard(expectedKeyboard);

			// Palaso sets the keyboard property for Windows system keyboards to <layoutname>-<locale>
			var ws = new WritingSystemDefinition {Keyboard = "foo-en-US"};
			Assert.That(Keyboard.Controller.DefaultForWritingSystem(ws), Is.EqualTo(expectedKeyboard));
		}

		[Test]
		public void DefaultForWritingSystem_OldPalasoKeymanKeyboard()
		{
			var inputLanguage = new InputLanguageWrapper(new CultureInfo("en-US"), IntPtr.Zero, "foo");
			KeyboardDescription expectedKeyboard;
			if (Keyboard.Controller.AllAvailableKeyboards.Any(kbd => kbd.Layout == "IPA Unicode 1.1.1"))
			{
				expectedKeyboard =
					Keyboard.Controller.AllAvailableKeyboards.First(kbd => kbd.Layout == "IPA Unicode 1.1.1")
						as KeyboardDescription;
			}
			else
			{
				expectedKeyboard = new KeyboardDescription("IPA Unicode 1.1.1 - English (US)", "IPA Unicode 1.1.1",
					"en-US", inputLanguage, KeyboardController.Adaptors[0]);
				KeyboardController.Manager.RegisterKeyboard(expectedKeyboard);
			}

			// Palaso sets the keyboard property for Keyman keyboards to <layoutname>
			var ws = new WritingSystemDefinition {Keyboard = "IPA Unicode 1.1.1"};
			Assert.That(Keyboard.Controller.DefaultForWritingSystem(ws), Is.EqualTo(expectedKeyboard));
		}

		[Test]
		public void DefaultForWritingSystem_OldPalasoIbusKeyboard()
		{
			#if __MonoCS__
			// For this test on Linux we only use the XkbKeyboardAdaptor and simulate an available
			// IBus keyboard. This is necessary because otherwise the test might return an
			// installed Danish IBus keyboard (m17n:da:post) instead of our expected dummy one.
			KeyboardController.Manager.SetKeyboardAdaptors(new[] { new SIL.WritingSystems.WindowsForms.Keyboarding.Linux.XkbKeyboardAdaptor() });
			#endif
			var inputLanguage = new InputLanguageWrapper(new CultureInfo("en-US"), IntPtr.Zero, "foo");
			var expectedKeyboard = new KeyboardDescription("m17n:da:post - English (US)", "m17n:da:post", "en-US", inputLanguage,
				KeyboardController.Adaptors[0]);
			KeyboardController.Manager.RegisterKeyboard(expectedKeyboard);

			// Palaso sets the keyboard property for Ibus keyboards to <ibus longname>
			var ws = new WritingSystemDefinition {Keyboard = "m17n:da:post"};
			Assert.That(Keyboard.Controller.DefaultForWritingSystem(ws), Is.EqualTo(expectedKeyboard));
		}

		[Test]
		public void DefaultForWritingSystem_OldFwSystemKeyboard()
		{
			// 0x001C is Albanian (see http://msdn.microsoft.com/en-us/goglobal/bb896001.aspx).
			// Make sure it's not installed on current system.
			if (InputLanguage.InstalledInputLanguages.Cast<InputLanguage>().Any(lang => lang.Culture.LCID == 0x041C))
				Assert.Ignore("Input language 'Albanian (Albania)' is installed on current system. Can't run this test.");

			var inputLanguage = new InputLanguageWrapper("sq-AL", IntPtr.Zero, "US");
			var expectedKeyboard = new KeyboardDescription("US - Albanian (Albania)", "US", "sq-AL", inputLanguage,
				KeyboardController.Adaptors[0]);
			KeyboardController.Manager.RegisterKeyboard(expectedKeyboard);

			// FieldWorks sets the WindowsLcid property for System keyboards to <lcid>
			var ws = new WritingSystemDefinition {WindowsLcid = 0x041C.ToString(CultureInfo.InvariantCulture)};
			Assert.That(Keyboard.Controller.DefaultForWritingSystem(ws), Is.EqualTo(expectedKeyboard));
		}

		[Test]
		public void DefaultForWritingSystem_OldNonexistingFwSystemKeyboard()
		{
			// 0x001C is Albanian. Make sure it's not installed on current system.
			if (InputLanguage.InstalledInputLanguages.Cast<InputLanguage>().Any(lang => lang.Culture.LCID == 0x041C))
				Assert.Ignore("Input language 'Albanian (Albania)' is installed on current system. Can't run this test.");

			// FieldWorks sets the WindowsLcid property for System keyboards to <lcid>
			var ws = new WritingSystemDefinition {WindowsLcid = 0x041C.ToString(CultureInfo.InvariantCulture)};
			Assert.That(Keyboard.Controller.DefaultForWritingSystem(ws),
				Is.EqualTo(KeyboardController.Adaptors[0].DefaultKeyboard));
		}

		[Test]
		public void DefaultForWritingSystem_OldFwKeymanKeyboard()
		{
			var inputLanguage = new InputLanguageWrapper(new CultureInfo("en-US"), IntPtr.Zero, "foo");
			var expectedKeyboard = new KeyboardDescription("IPA Unicode 1.1.1 - English (US)", "IPA Unicode 1.1.1", "en-US", inputLanguage,
				KeyboardController.Adaptors[0]);
			KeyboardController.Manager.RegisterKeyboard(expectedKeyboard);

			// FieldWorks sets the keyboard property for Keyman keyboards to <layoutname> and WindowsLcid to <lcid>
			var ws = new WritingSystemDefinition {Keyboard = "IPA Unicode 1.1.1", WindowsLcid = 0x409.ToString(CultureInfo.InvariantCulture)};
			Assert.That(Keyboard.Controller.DefaultForWritingSystem(ws), Is.EqualTo(expectedKeyboard));
		}
	}
}