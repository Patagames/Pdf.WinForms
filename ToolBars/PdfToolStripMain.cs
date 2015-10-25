using Patagames.Pdf.Net.EventArguments;
using Patagames.Pdf.Net.Exceptions;
using System;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace Patagames.Pdf.Net.Controls.WinForms.ToolBars
{
	/// <summary>
	/// Provides a container for Windows toolbar objects with predefined functionality for opening and printing
	/// </summary>
	public class PdfToolStripMain : PdfToolStrip
	{
		#region Public events
		/// <summary>
		/// Occurs when the loaded document protected by password. Application should return the password through Value property
		/// </summary>1
		public event EventHandler<EventArgs<string>> PasswordRequired = null;
		#endregion

		#region Overriding
		/// <summary>
		/// Create all buttons and add its into toolbar. Override this method to create custom buttons
		/// </summary>
		protected override void InitializeButtons()
		{
			var btn = CreateButton("btnOpenDoc",
				Properties.PdfToolStrip.btnOpenText,
				Properties.PdfToolStrip.btnOpenToolTipText,
				Properties.PdfToolStrip.btnOpenImage,
				btn_OpenDocClick);
			this.Items.Add(btn);

			btn = CreateButton("btnPrintDoc",
				Properties.PdfToolStrip.btnPrintText,
				Properties.PdfToolStrip.btnPrintToolTipText,
				Properties.PdfToolStrip.btnPrintImage,
				btn_PrintDocClick);
			this.Items.Add(btn);
		}

		/// <summary>
		/// Called when the ToolStrip's items need to change its states
		/// </summary>
		protected override void UpdateButtons()
		{
			var tsi = this.Items["btnOpenDoc"];
			if (tsi != null)
				tsi.Enabled = (PdfViewer != null);

			tsi = this.Items["btnPrintDoc"];
			if (tsi != null)
				tsi.Enabled = (PdfViewer != null) && (PdfViewer.Document != null);

			if (PdfViewer == null || PdfViewer.Document == null)
				return;
		}

		/// <summary>
		/// Called when the current PdfViewer control associated with the ToolStrip is changing.
		/// </summary>
		/// <param name="oldValue">PdfViewer control of which was associated with the ToolStrip.</param>
		/// <param name="newValue">PdfViewer control of which will be associated with the ToolStrip.</param>
		protected override void OnPdfViewerChanging(PdfViewer oldValue, PdfViewer newValue)
		{
			base.OnPdfViewerChanging(oldValue, newValue);
			if (oldValue != null)
				UnsubscribePdfViewEvents(oldValue);
			if (newValue != null)
				SubscribePdfViewEvents(newValue);
		}

		#endregion

		#region Event handlers for PdfViewer
		private void PdfViewer_SomethingChanged(object sender, EventArgs e)
		{
			UpdateButtons();
		}
		#endregion

		#region Event handlers for buttons
		private void btn_OpenDocClick(object sender, EventArgs e)
		{
			OnOpenClick(this.Items["btnOpenDoc"] as ToolStripButton);
		}
		private void btn_PrintDocClick(object sender, EventArgs e)
		{
			OnPrintClick(this.Items["btnPrintDoc"] as ToolStripButton);
		}
		#endregion

		#region Protected methods
		/// <summary>
		/// Occurs when the Open button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnOpenClick(ToolStripButton item)
		{
			var dlg = new OpenFileDialog();
			dlg.Multiselect = false;
			dlg.Filter = Properties.PdfToolStrip.OpenDialogFilter;
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				try {
					PdfViewer.LoadDocument(dlg.FileName);
				}
				catch (InvalidPasswordException)
				{
					string password = OnPasswordRequired();
					try {
						PdfViewer.LoadDocument(dlg.FileName);
					}
					catch(Exception ex)
					{
						MessageBox.Show(ex.Message, Properties.Error.ErrorHeader, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		/// <summary>
		/// Occurs when the Loaded document protected by password. Application should return the password
		/// </summary>
		/// <returns></returns>
		protected virtual string OnPasswordRequired()
		{
			var args = new EventArgs<string>(null);
			if (PasswordRequired != null)
				PasswordRequired(this, args);
			return args.Value;
		}

		/// <summary>
		/// Occurs when the Print button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnPrintClick(ToolStripButton item)
		{
			PrintDocument pd = new PrintDocument();
			int pageForPrint = 0;

			pd.PrintPage += (s, e) =>
			{
				using (PdfBitmap bmp = new PdfBitmap((int)e.PageSettings.PrintableArea.Width, (int)e.PageSettings.PrintableArea.Height, true))
				{
					//Render to PdfBitmap using page's Render method with FPDF_PRINTING flag
					PdfViewer.Document.Pages[pageForPrint].Render
						(bmp,
						0,
						0,
						(int)e.PageSettings.PrintableArea.Width,
						(int)e.PageBounds.Height,
						Patagames.Pdf.Enums.PageRotate.Normal, Patagames.Pdf.Enums.RenderFlags.FPDF_PRINTING);

					//Draw rendered image to printer's graphics surface
					e.Graphics.DrawImageUnscaled(bmp.Image,
						(int)e.PageSettings.PrintableArea.X,
						(int)e.PageSettings.PrintableArea.Y);

					//Print next page
					if (pageForPrint < PdfViewer.Document.Pages.Count - 1)
					{
						pageForPrint++;
						e.HasMorePages = true;
					}
				}
			};
			PrintPreviewDialog dlg = new PrintPreviewDialog();
			dlg.Document = pd;
			dlg.ShowDialog();
		}
		#endregion

		#region Private methods
		private void UnsubscribePdfViewEvents(PdfViewer oldValue)
		{
			oldValue.DocumentLoaded -= PdfViewer_SomethingChanged;
			oldValue.DocumentClosed -= PdfViewer_SomethingChanged;
		}

		private void SubscribePdfViewEvents(PdfViewer newValue)
		{
			newValue.DocumentLoaded += PdfViewer_SomethingChanged;
			newValue.DocumentClosed += PdfViewer_SomethingChanged;
		}
		#endregion



	}
}
