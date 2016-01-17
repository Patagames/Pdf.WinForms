using Patagames.Pdf.Enums;
using Patagames.Pdf.Net.EventArguments;
using Patagames.Pdf.Net.Exceptions;
using System;
using System.Drawing;
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
			//Set up PrintDocument object
			PrintDocument pd = new PrintDocument();
			pd.PrinterSettings.MinimumPage = 1;
			pd.PrinterSettings.MaximumPage = PdfViewer.Document.Pages.Count;
			pd.PrinterSettings.FromPage = pd.PrinterSettings.MinimumPage;
			pd.PrinterSettings.ToPage = pd.PrinterSettings.MaximumPage;
			int pageForPrint = 0;

			pd.BeginPrint += (s, e) => 
			{
				//Calculate range of pages for print
				switch(pd.PrinterSettings.PrintRange)
				{
					case PrintRange.Selection:
					case PrintRange.CurrentPage: //Curent page
						pd.PrinterSettings.FromPage = PdfViewer.Document.Pages.CurrentIndex+1;
						pd.PrinterSettings.ToPage = PdfViewer.Document.Pages.CurrentIndex+1;
						break;
					case PrintRange.SomePages: //The range specified by the user
						break;
					default: //All pages
						pd.PrinterSettings.FromPage = pd.PrinterSettings.MinimumPage;
						pd.PrinterSettings.ToPage = pd.PrinterSettings.MaximumPage;
						break;
				}

				pageForPrint = pd.PrinterSettings.FromPage-1;
			};

			pd.QueryPageSettings += (s, e) =>
			{
				//Set the paper orientation to Landscape if the page is rotated
				e.PageSettings.Landscape = e.PageSettings.Landscape;
				if ((PdfViewer.Document.Pages[pageForPrint].Rotation == PageRotate.Rotate270
					|| PdfViewer.Document.Pages[pageForPrint].Rotation == PageRotate.Rotate90
					))
					e.PageSettings.Landscape = true;
			};

			pd.PrintPage += (s, e) =>
			{
				//Calculate the size of the printable area in pixels
				var fitSize = new Size(
					(int)(e.Graphics.DpiX * e.PageSettings.PrintableArea.Width * 0.254 / 25.4f),
					(int)(e.Graphics.DpiY * e.PageSettings.PrintableArea.Height * 0.254 / 25.4f)
					);
				//Get page's size
				var pageSize = new Size(
					(int)PdfViewer.Document.Pages[pageForPrint].Width, 
					(int)PdfViewer.Document.Pages[pageForPrint].Height);

				//If page was rotated in original file, then we need to "rotate the paper in printer". 
				//For that just swap the width and height of the paper.
				if (PdfViewer.Document.Pages[pageForPrint].OriginalRotation == PageRotate.Rotate270
					|| PdfViewer.Document.Pages[pageForPrint].OriginalRotation == PageRotate.Rotate90)
					fitSize = new Size(fitSize.Height, fitSize.Width);

				//Calculate the page's size fitted to the paper's size.  
				var rSize = GetRenderSize(pageSize, fitSize);

				using (PdfBitmap bmp = new PdfBitmap((int)rSize.Width, (int)rSize.Height, true))
				{
					//Render to PdfBitmap using page's Render method with FPDF_PRINTING flag.
					PdfViewer.Document.Pages[pageForPrint].Render
						(bmp,
						0,
						0,
						(int)rSize.Width,
						(int)rSize.Height,
						PageRotate.Normal,
						RenderFlags.FPDF_PRINTING | RenderFlags.FPDF_ANNOT);

					//Rotates the PdfBitmap image depending on the orientation of the page
					if (PageRotation(PdfViewer.Document.Pages[pageForPrint]) == PageRotate.Rotate270)
						bmp.Image.RotateFlip(System.Drawing.RotateFlipType.Rotate270FlipNone);
					else if (PageRotation(PdfViewer.Document.Pages[pageForPrint]) == PageRotate.Rotate180)
						bmp.Image.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);
					else if (PageRotation(PdfViewer.Document.Pages[pageForPrint]) == PageRotate.Rotate90)
						bmp.Image.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);

					//Set DPI of the image same as the printer's DPI
					(bmp.Image as Bitmap).SetResolution(e.Graphics.DpiX, e.Graphics.DpiY);
					//Draw rendered image to printer's graphics surface
					e.Graphics.DrawImageUnscaled(bmp.Image,
						(int)e.PageSettings.PrintableArea.X,
						(int)e.PageSettings.PrintableArea.Y);
				}

				//Print next page
				if (pageForPrint < pd.PrinterSettings.ToPage-1)
				{
					pageForPrint++;
					e.HasMorePages = true;
				}

			};

			//Show standard print dilog
			var dlg = new PrintDialog();
			dlg.AllowCurrentPage = true;
			dlg.AllowSomePages = true;
			dlg.UseEXDialog = true;
			dlg.Document = pd;
			if(dlg.ShowDialog()== DialogResult.OK)
			{
				pd.Print();
				//var dlg2 = new PrintPreviewDialog();
				//dlg2.Document = pd;
				//dlg2.ShowDialog();
			}


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

		private PageRotate PageRotation(PdfPage pdfPage)
		{
			int rot = pdfPage.Rotation - pdfPage.OriginalRotation;
			if (rot < 0)
				rot = 4 + rot;
			return (PageRotate)rot;
		}

		private SizeF GetRenderSize(Size pageSize, Size fitSize)
		{
			double w, h;
			w = pageSize.Width;
			h = pageSize.Height;

			double nw = fitSize.Width;
			double nh = h * nw / w;

			nh = fitSize.Height;
			nw = w * nh / h;
			if (nw > fitSize.Width)
			{
				nw = fitSize.Width;
				nh = h * nw / w;
			}
			return new SizeF((float)nw, (float)nh);
		}
		#endregion



	}
}
