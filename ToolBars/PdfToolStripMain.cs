using Patagames.Pdf.Net.EventArguments;
using Patagames.Pdf.Net.Exceptions;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Patagames.Pdf.Net.Controls.WinForms.ToolBars
{
	/// <summary>
	/// Provides a container for Windows toolbar objects with predefined functionality for opening and printing
	/// </summary>
	public class PdfToolStripMain : PdfToolStrip
	{
		#region private members
		delegate void ShowPrintDialogDelegate(PrintDialog dlg);
		#endregion

		#region Public events
		/// <summary>
		/// Occurs when the loaded document protected by password. Application should return the password through Value property
		/// </summary>
		public event EventHandler<EventArgs<string>> PasswordRequired = null;

		/// <summary>
		/// Occurs after an instance of PdfPrintDocument class is created and before printing is started.
		/// </summary>
		/// <remarks>
		/// You can use this event to get access to PdfPrintDialog which is used in printing routine.
		/// For example, the printing routine shows the standard dialog with printing progress. 
		/// If you want to suppress it you can write in event handler the following:
		/// <code>
		/// private void ToolbarMain1_PdfPrintDocumentCreated(object sender, EventArgs&lt;PdfPrintDocument&gt; e)
		/// {
		///		e.Value.PrintController = new StandardPrintController();
		/// }
		/// </code>
		/// </remarks>
		public event EventHandler<EventArgs<PdfPrintDocument>> PdfPrintDocumentCreated = null;
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
				try
				{
					PdfViewer.LoadDocument(dlg.FileName);
				}
				catch (InvalidPasswordException)
				{
					string password = OnPasswordRequired();
					try
					{
						PdfViewer.LoadDocument(dlg.FileName, password);
					}
					catch (Exception ex)
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
			if (PdfViewer.Document.FormFill != null)
				PdfViewer.Document.FormFill.ForceToKillFocus();

			//Show standard print dialog
			var printDoc = new PdfPrintDocument(PdfViewer.Document);
			var dlg = new PrintDialog();
			dlg.AllowCurrentPage = true;
			dlg.AllowSomePages = true;
			dlg.UseEXDialog = true;
			dlg.Document = printDoc;
			OnPdfPrinDocumentCreaded(new EventArgs<PdfPrintDocument>(printDoc));
			ShowPrintDialogDelegate showprintdialog = ShowPrintDialog;
			this.BeginInvoke(showprintdialog, dlg);
			//this.BeginInvoke(new Action(() =>
			//{
			//	if (dlg.ShowDialog() == DialogResult.OK)
			//	{
			//		PrintController printController = new StandardPrintController();
			//		printDoc.PrintController = printController;
			//		printDoc.Print();
			//	}
			//}));
		}

		/// <summary>
		/// Occurs after an instance of PdfPrintDocument class is created and before printing is started.
		/// </summary>
		protected virtual void OnPdfPrinDocumentCreaded(EventArgs<PdfPrintDocument> e)
		{
			if (PdfPrintDocumentCreated != null)
				PdfPrintDocumentCreated(this, e);
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

		private static void ShowPrintDialog(PrintDialog dlg)
		{
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				try
				{
					dlg.Document.Print();
				}
				catch (Win32Exception)
				{
					//Printing was canceled
				}
			}
		}
		#endregion

	}
}
