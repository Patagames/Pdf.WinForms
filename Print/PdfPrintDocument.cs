using Patagames.Pdf.Enums;
using System;
using System.Drawing;
using System.Drawing.Printing;

namespace Patagames.Pdf.Net.Controls.WinForms
{
	/// <summary>
	/// Defines a reusable object that sends output to a printer, when printing from a Windows Forms application.
	/// </summary>
	public class PdfPrintDocument : PrintDocument
	{
		#region Private members
		PdfDocument _pdfDoc;
		int _pageForPrint;
		IntPtr _printHandle;
		IntPtr _docForPrint;
		IntPtr _currentPage;
		bool _useDP;
		bool _autoRotate = false;
		#endregion;

		#region Constructors, destructors and initialization
		/// <summary>
		/// Initializes a new instance of the <see cref="PdfPrintDocument"/> class.
		/// </summary>
		/// <param name="Document">The document to print</param>
		/// <param name="mode">Reserved. Must be zero.</param>
		public PdfPrintDocument(PdfDocument Document, int mode = 0)
		{
			if (Document == null)
				throw new ArgumentNullException("Document");

			_pdfDoc = Document;
			_useDP = (mode==1);

			PrinterSettings.MinimumPage = 1;
			PrinterSettings.MaximumPage = _pdfDoc.Pages.Count;
			PrinterSettings.FromPage = PrinterSettings.MinimumPage;
			PrinterSettings.ToPage = PrinterSettings.MaximumPage;
		}
		#endregion

		#region Overriding
		/// <summary>
		/// Raises the System.Drawing.Printing.PrintDocument.BeginPrint event. It is called
		/// after the System.Drawing.Printing.PrintDocument.Print method is called and before
		/// the first page of the document prints.
		/// </summary>
		/// <param name="e">A System.Drawing.Printing.PrintEventArgs that contains the event data.</param>
		protected override void OnBeginPrint(PrintEventArgs e)
		{
			base.OnBeginPrint(e);

			//Calculate range of pages for print
			switch (PrinterSettings.PrintRange)
			{
				case PrintRange.Selection:
				case PrintRange.CurrentPage: //Curent page
					PrinterSettings.FromPage = _pdfDoc.Pages.CurrentIndex + 1;
					PrinterSettings.ToPage = _pdfDoc.Pages.CurrentIndex + 1;
					break;
				case PrintRange.SomePages: //The range specified by the user
					break;
				default: //All pages
					PrinterSettings.FromPage = PrinterSettings.MinimumPage;
					PrinterSettings.ToPage = PrinterSettings.MaximumPage;
					break;
			}

			_docForPrint = InitDocument();
			if (_docForPrint == IntPtr.Zero)
			{
				e.Cancel = true;
				return;
			}
			_pageForPrint = _useDP ? 0 : PrinterSettings.FromPage-1;
		}


		/// <summary>
		/// Raises the System.Drawing.Printing.PrintDocument.EndPrint event. It is called
		/// when the last page of the document has printed.
		/// </summary>
		/// <param name="e">A System.Drawing.Printing.PrintEventArgs that contains the event data.</param>
		protected override void OnEndPrint(PrintEventArgs e)
		{
			base.OnEndPrint(e);

			if (_printHandle != IntPtr.Zero)
				Pdfium.FPDFPRINT_Close(_printHandle);
			_printHandle = IntPtr.Zero;
		}

		/// <summary>
		/// Raises the QueryPageSettings event. It is called immediately before each PrintPage event.
		/// </summary>
		/// <param name="e">A System.Drawing.Printing.QueryPageSettingsEventArgs that contains the event data.</param>
		protected override void OnQueryPageSettings(QueryPageSettingsEventArgs e)
		{
			base.OnQueryPageSettings(e);

			_currentPage = Pdfium.FPDF_LoadPage(_docForPrint, _pageForPrint);
			if (_currentPage == IntPtr.Zero)
			{
				e.Cancel = true;
				return;
			}
		}

		/// <summary>
		/// Raises the System.Drawing.Printing.PrintDocument.PrintPage event. It is called
		/// before a page prints.
		/// </summary>
		/// <param name="e"> A System.Drawing.Printing.PrintPageEventArgs that contains the event data.</param>
		protected override void OnPrintPage(PrintPageEventArgs e)
		{
			base.OnPrintPage(e);

			IntPtr hdc = IntPtr.Zero;
			try
			{
				if (e.Cancel)
					return;

				if (_currentPage == IntPtr.Zero)
				{
					e.Cancel = true;
					return;
				}

				double dpiX = e.Graphics.DpiX;
				double dpiY = e.Graphics.DpiY;

				double width, height;
				CalcSize(dpiX, dpiY, e.PageSettings.PrintableArea, e.PageSettings.Landscape, out width, out height);
				PageRotate rotation = CalcRotation(e.PageSettings.Landscape, ref width, ref height);

				hdc = e.Graphics.GetHdc();
				Pdfium.FPDF_RenderPage(
					hdc,
					_currentPage,
					0,
					0,
					(int)(width),
					(int)(height),
					rotation,
					RenderFlags.FPDF_PRINTING | RenderFlags.FPDF_ANNOT);

				//Print next page
				if (_pageForPrint < PrinterSettings.ToPage - (_useDP ? PrinterSettings.FromPage : 1))
				{
					_pageForPrint++;
					e.HasMorePages = true;
				}
			}
			finally
			{
				if (hdc != IntPtr.Zero)
					e.Graphics.ReleaseHdc(hdc);
				hdc = IntPtr.Zero;
				if (_currentPage != IntPtr.Zero)
					Pdfium.FPDF_ClosePage(_currentPage);
				_currentPage = IntPtr.Zero;
			}
		}
		#endregion

		#region Private methods
		private IntPtr InitDocument()
		{
			if(!_useDP)
				return _pdfDoc.Handle;

			_printHandle = Pdfium.FPDFPRINT_Open(
				_pdfDoc.Handle,
				string.Format("{0}-{1}", PrinterSettings.FromPage, PrinterSettings.ToPage),
				DefaultPageSettings.PaperSize.Width / 100 * 72,
				DefaultPageSettings.PaperSize.Height / 100 * 72,
				(int)((double)DefaultPageSettings.PrintableArea.X / 100 * 72),
				(int)((double)DefaultPageSettings.PrintableArea.Y / 100 * 72),
				(int)((double)DefaultPageSettings.PrintableArea.Width / 100 * 72),
				(int)((double)DefaultPageSettings.PrintableArea.Height / 100 * 72),
				PrintScallingMode.PrintableArea);

			if (_printHandle == IntPtr.Zero)
				return IntPtr.Zero;

			_docForPrint = Pdfium.FPDFPRINT_GetDocument(_printHandle);
			if (_docForPrint == IntPtr.Zero)
				return IntPtr.Zero;

			return _docForPrint;
		}

		private void CalcSize(double dpiX, double dpiY, RectangleF printableArea, bool isLandscape, out double width, out double height)
		{
			width = Pdfium.FPDF_GetPageWidth(_currentPage) / 72 * dpiX;
			height = Pdfium.FPDF_GetPageHeight(_currentPage) / 72 * dpiY;
			if (_useDP)
				return;

			//Calculate the size of the printable area in pixels
			var fitSize = new SizeF(
				(float)dpiX * printableArea.Width / 100.0f,
				(float)dpiY * printableArea.Height / 100.0f
				);
			var pageSize = new SizeF(
				(float)width,
				(float)height
				);

			var rot = Pdfium.FPDFPage_GetRotation(_currentPage);
			bool isRotated = (rot == PageRotate.Rotate270 || rot == PageRotate.Rotate90);

			if (_autoRotate && isRotated)
				fitSize = new SizeF(fitSize.Height, fitSize.Width);
			else if (!_autoRotate && isLandscape)
				fitSize = new SizeF(fitSize.Height, fitSize.Width);

			var sz = GetRenderSize(pageSize, fitSize);
			width = sz.Width;
			height = sz.Height;
		}

		private PageRotate CalcRotation(bool isLandscape, ref double width, ref double height)
		{
			var rot = Pdfium.FPDFPage_GetRotation(_currentPage);
			bool isRotated = (rot == PageRotate.Rotate270 || rot == PageRotate.Rotate90);

			if (_autoRotate && isRotated != isLandscape)
			{
				double tmp = width;
				width = height;
				height = tmp;
				return PageRotate.Rotate90;
			}
			return PageRotate.Normal;
		}

		private SizeF GetRenderSize(SizeF pageSize, SizeF fitSize)
		{
			double w, h;
			w = pageSize.Width;
			h = pageSize.Height;

			double nh = fitSize.Height;
			double nw = w * nh / h;
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
