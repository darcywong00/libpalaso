﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Palaso.IO;
using Palaso.UI.WindowsForms.ClearShare;
using Palaso.UI.WindowsForms.ClearShare.WinFormsUI;
using Palaso.UI.WindowsForms.ImageToolbox;

namespace PalasoUIWindowsForms.Tests.ClearShare
{
	[TestFixture, Ignore("Needs exiftool in the distfiles")]
	public class MetadataTests
	{
		private Bitmap _mediaFile;
		private TempFile _tempFile;
		private Metadata _outgoing;

		[SetUp]
		public void Setup()
		{
			_mediaFile = new Bitmap(10, 10);
			_tempFile = TempFile.WithExtension("png");
			_mediaFile.Save(_tempFile.Path);
		   _outgoing = Metadata.FromFile(_tempFile.Path);
		 }

		[TearDown]
		public void TearDown()
		{
			_tempFile.Dispose();
		}

		[Test]
		public void RoundTripPng_CopyrightNotice()
		{
			_outgoing.CopyrightNotice = "Copyright Test";
			_outgoing.Write();
			Assert.AreEqual("Copyright Test", Metadata.FromFile(_tempFile.Path).CopyrightNotice);
		}

		[Test]
		public void RoundTripPng_PNGWithDangerousCharacters_PreservesCopyrightNotice()
		{
			_outgoing.CopyrightNotice = "Copyright <! ' <hello>";
			_outgoing.Write();
			Assert.AreEqual("Copyright <! ' <hello>", Metadata.FromFile(_tempFile.Path).CopyrightNotice);
		}

		[Test]
		public void RoundTripPng_HasCC_Permissive_License_ReadsInSameLicense()
		{
			_outgoing.License =new CreativeCommonsLicense(false,true,CreativeCommonsLicense.DerivativeRules.Derivatives);
			_outgoing.Write();
			var cc = (CreativeCommonsLicense) Metadata.FromFile(_tempFile.Path).License;
			Assert.AreEqual(cc.AttributionRequired, false);
			Assert.AreEqual(cc.CommercialUseAllowed, true);
			Assert.AreEqual(cc.DerivativeRule, CreativeCommonsLicense.DerivativeRules.Derivatives);
		}

		[Test]
		public void RoundTripPng_HasCC_Strict_License_ReadsInSameLicense()
		{
			_outgoing.License = new CreativeCommonsLicense(true, false, CreativeCommonsLicense.DerivativeRules.NoDerivatives);
			_outgoing.Write();
			var cc = (CreativeCommonsLicense)Metadata.FromFile(_tempFile.Path).License;
			Assert.AreEqual(cc.AttributionRequired, true);
			Assert.AreEqual(cc.CommercialUseAllowed, false);
			Assert.AreEqual(cc.DerivativeRule, CreativeCommonsLicense.DerivativeRules.NoDerivatives);
		}


		[Test]
		public void RoundTripPng_HasCC_Medium_License_ReadsInSameLicense()
		{
			_outgoing.License = new CreativeCommonsLicense(true, true, CreativeCommonsLicense.DerivativeRules.DerivativesWithShareAndShareAlike);
			_outgoing.Write();
			var cc = (CreativeCommonsLicense)Metadata.FromFile(_tempFile.Path).License;
			Assert.AreEqual(cc.AttributionRequired, true);
			Assert.AreEqual(cc.CommercialUseAllowed, true);
			Assert.AreEqual(cc.DerivativeRule, CreativeCommonsLicense.DerivativeRules.DerivativesWithShareAndShareAlike);
		}
		[Test]
		public void RoundTripPng_AttributionUrl()
		{
			_outgoing.AttributionUrl = "http://somewhere.com";
			_outgoing.Write();
			Assert.AreEqual("http://somewhere.com", Metadata.FromFile(_tempFile.Path).AttributionUrl);
		}

		[Test]
		public void RoundTripPng_AttributionName()
		{
			_outgoing.Creator = "joe shmo";
			_outgoing.Write();
			Assert.AreEqual("joe shmo", Metadata.FromFile(_tempFile.Path).Creator);
		}


		[Test]
		public void SetLicense_HasChanges_True()
		{
			var m = new Metadata();
			m.HasChanges = false;
			m.License=new CreativeCommonsLicense(true,true,CreativeCommonsLicense.DerivativeRules.Derivatives);
			Assert.IsTrue(m.HasChanges);
		}

		[Test]
		public void ChangeLicenseObject_HasChanges_True()
		{
			var m = new Metadata();
			m.License = new CreativeCommonsLicense(true, true, CreativeCommonsLicense.DerivativeRules.Derivatives);
			m.HasChanges = false;
			m.License = new NullLicense();
			Assert.IsTrue(m.HasChanges);
		}


		[Test]
		public void ChangeLicenseDetails_HasChanges_True()
		{
			var m = new Metadata();
			m.License = new CreativeCommonsLicense(true, true, CreativeCommonsLicense.DerivativeRules.Derivatives);
			m.HasChanges = false;
			((CreativeCommonsLicense) m.License).CommercialUseAllowed = false;
			Assert.IsTrue(m.HasChanges);
		}


		[Test]
		public void SetHasChangesFalse_AlsoClearsLicenseHasChanges()
		{
			var m = new Metadata();
			m.License = new CreativeCommonsLicense(true, true, CreativeCommonsLicense.DerivativeRules.Derivatives);
			 ((CreativeCommonsLicense)m.License).CommercialUseAllowed = false;
			 Assert.IsTrue(m.HasChanges);
			 m.HasChanges = false;
			 Assert.IsFalse(m.License.HasChanges);
			 Assert.IsFalse(m.HasChanges);
		}

		[Test]
		public void LoadFromFile_CopyrightNotSet_CopyrightGivesNull()
		{
			Assert.IsNull(Metadata.FromFile(_tempFile.Path).Creator);
		}

		[Test]
		public void DeepCopy()
		{
			var m = new Metadata();
			m.License = new CreativeCommonsLicense(true, true,
												   CreativeCommonsLicense.DerivativeRules.
													   DerivativesWithShareAndShareAlike);
			Metadata copy = m.DeepCopy();
			Assert.AreEqual(m.License.Url,copy.License.Url);
		}
	}
}