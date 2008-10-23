using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace Palaso.UI.WindowsForms.WritingSystems
{
	public partial class WSSpellingControl : UserControl
	{
		private WritingSystemSetupPM _model;
		private bool _changingModel;

		public WSSpellingControl()
		{
			InitializeComponent();
		}

		public void BindToModel(WritingSystemSetupPM model)
		{
			if (_model != null)
			{
				model.CurrentItemUpdated -= ModelCurrentItemUpdated;
				model.SelectionChanged -= ModelSelectionChanged;
			}
			_model = model;
			if (_model != null)
			{
				UpdateFromModel();
				model.CurrentItemUpdated += ModelCurrentItemUpdated;
				model.SelectionChanged += ModelSelectionChanged;
			}
		}

		private void ModelSelectionChanged(object sender, EventArgs e)
		{
			UpdateFromModel();
		}

		private void ModelCurrentItemUpdated(object sender, EventArgs e)
		{
			if (_changingModel)
			{
				return;
			}
			UpdateFromModel();
		}

		private void UpdateFromModel()
		{
			if (!_model.HasCurrentSelection)
			{
				Enabled = false;
				return;
			}
			Enabled = true;
			_spellCheckingIdTextBox.Text = _model.CurrentSpellCheckingId;
		}

		private void _spellCheckingIdTextBox_TextChanged(object sender, EventArgs e)
		{
			_changingModel = true;
			try
			{
				_model.CurrentSpellCheckingId = _spellCheckingIdTextBox.Text;
			}
			finally
			{
				_changingModel = false;
			}
		}
	}
}