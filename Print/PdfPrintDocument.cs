using Patagames.Pdf.Enums;
using System;
using System.Drawing;
using System.Drawing.Printing;

namespace Patagames.Pdf.Net.Controls.WinForms
{
    /// <summary>
    /// Defines a reusable object that sends output to a printer, when printing from a Windows Forms application.
    /// </summary>
    /// <seealso href="https://pdfium.patagames.com/c-pdf-library/">Printing PDF Files With C#</seealso>
    public class PdfPrintDocument : PrintDocument
	{
		#region Private members
		PdfDocument _pdfDoc;
		int _pageForPrint;
		IntPtr _printHandle;
		IntPtr _docForPrint;
		bool _useDP;
		int _scale = 100;
		#endregion;

		#region Public events
		/// <summary>
		/// Occurs after the page of the document is loaded and before prints.
		/// </summary>
		public event EventHandler<BeforeRenderPageEventArgs> BeforeRenderPage;

		/// <summary>
		/// Occurs after rendering a document page.
		/// </summary>
		public event EventHandler<BeforeRenderPageEventArgs> AfterRenderPage;
		#endregion

		#region Public properties
		/// <summary>
		/// Automatically rotate pages when printing
		/// </summary>
		public bool AutoRotate { get; set; }

		/// <summary>
		/// Gets or sets a Boolean indicating whether a page will be centered automatically on the sheet when printing.
		/// </summary>
		public bool AutoCenter { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the PDF document that will be printed.
		/// </summary>
		public PdfDocument Document
		{
			get
			{
				return _pdfDoc;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException("Document");
				if (_pdfDoc != value)
				{
					_pdfDoc = value;
					PrinterSettings.MinimumPage = 1;
					PrinterSettings.MaximumPage = _pdfDoc.Pages.Count;
					PrinterSettings.FromPage = PrinterSettings.MinimumPage;
					PrinterSettings.ToPage = PrinterSettings.MaximumPage;
				}
			}
		}

		/// <summary>
		/// Gets or sets the size mode for the pages.
		/// </summary>
		public PrintSizeMode PrintSizeMode { get; set; }

		/// <summary>
		/// The scale of the pages in prcent. Must be between 1 and 1000
		/// </summary>
		public int Scale
		{
			get { return _scale; }
			set
			{
				if (value == _scale)
					return;
				else if (value < 1 || value > 1000)
					throw new ArgumentOutOfRangeException("Value", value, string.Format(Properties.Error.err0003, 1, 1000));
				_scale = value;
				PrintSizeMode = PrintSizeMode.CustomScale;
			}
		}

		/// <summary>
		/// Gets or sets a combination of <see cref="Patagames.Pdf.Enums.RenderFlags"/> for printing.
		/// </summary>
		public RenderFlags RenderFlags { get; set; }
		#endregion

		#region Constructors, destructors and initialization
		/// <summary>
		/// Initializes a new instance of the <see cref="PdfPrintDocument"/> class.
		/// </summary>
		public PdfPrintDocument()
        {
			_useDP = false;
			AutoRotate = true;
			AutoCenter = false;
			RenderFlags = RenderFlags.FPDF_PRINTING | RenderFlags.FPDF_ANNOT;

			PrinterSettings.MinimumPage = 1;
			PrinterSettings.MaximumPage = 1;
			PrinterSettings.FromPage = PrinterSettings.MinimumPage;
			PrinterSettings.ToPage = PrinterSettings.MaximumPage;
		}

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
			_useDP = (mode == 1);
			AutoRotate = true;
			AutoCenter = false;
			RenderFlags = RenderFlags.FPDF_PRINTING | RenderFlags.FPDF_ANNOT;

			PrinterSettings.MinimumPage = 1;
			PrinterSettings.MaximumPage = _pdfDoc.Pages.Count;
			PrinterSettings.FromPage = PrinterSettings.MinimumPage;
			PrinterSettings.ToPage = PrinterSettings.MaximumPage;
		}
		#endregion

		#region Overriding
		/// <summary>
		/// Raises the BeforeRenderPage event.
		/// </summary>
		/// <param name="g">Drawing surface.</param>
		/// <param name="page">The pagewhat will be printed.</param>
		/// <param name="x">Horizontal position of the <paramref name="page"/> on the drawing surface.</param>
		/// <param name="y">Vertical position of the <paramref name="page"/> on the drawing surface.</param>
		/// <param name="width">The page's width calculated to match the sheet size.</param>
		/// <param name="height">The page's height calculated to match the sheet size.</param>
		/// <param name="rotation">The page rotation.</param>
		protected virtual void OnBeforeRenderPage(Graphics g, PdfPage page, ref int x, ref int y, ref int width, ref int height, PageRotate rotation)
		{
			if (BeforeRenderPage != null)
			{
				var args = new BeforeRenderPageEventArgs(g, page, x, y, width, height, rotation);
				BeforeRenderPage(this, args);
				x = args.X;
				y = args.Y;
				width = args.Width;
				height = args.Height;
			}
		}

		/// <summary>
		/// Raises the AfterRenderPage event.
		/// </summary>
		/// <param name="g">Drawing surface.</param>
		/// <param name="page">The pagewhat will be printed.</param>
		/// <param name="x">Horizontal position of the <paramref name="page"/> on the drawing surface.</param>
		/// <param name="y">Vertical position of the <paramref name="page"/> on the drawing surface.</param>
		/// <param name="width">The page's width calculated to match the sheet size.</param>
		/// <param name="height">The page's height calculated to match the sheet size.</param>
		/// <param name="rotation">The page rotation.</param>
		protected virtual void OnAfterRenderPage(Graphics g, PdfPage page, int x, int y, int width, int height, PageRotate rotation)
		{
			if (AfterRenderPage != null)
				AfterRenderPage(this, new BeforeRenderPageEventArgs(g, page, x, y, width, height, rotation));
		}

		/// <summary>
		/// Raises the System.Drawing.Printing.PrintDocument.BeginPrint event. It is called
		/// after the System.Drawing.Printing.PrintDocument.Print method is called and before
		/// the first page of the document prints.
		/// </summary>
		/// <param name="e">A System.Drawing.Printing.PrintEventArgs that contains the event data.</param>
		/// <seealso href="https://pdfium.patagames.com/c-pdf-library/">C# Print PDF</seealso>
		protected override void OnBeginPrint(PrintEventArgs e)
		{
			base.OnBeginPrint(e);
			if (_pdfDoc == null)
				throw new ArgumentNullException("Document");

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
			_pageForPrint = _useDP ? 0 : PrinterSettings.FromPage - 1;
		}

        /// <summary>
        /// Raises the System.Drawing.Printing.PrintDocument.EndPrint event. It is called
        /// when the last page of the document has printed.
        /// </summary>
        /// <param name="e">A System.Drawing.Printing.PrintEventArgs that contains the event data.</param>
        /// <seealso href="https://pdfium.patagames.com/c-pdf-library/">C# Print PDF</seealso>
        protected override void OnEndPrint(PrintEventArgs e)
		{
			base.OnEndPrint(e);

			if (_printHandle != IntPtr.Zero)
				Pdfium.FPDFPRINT_Close(_printHandle);
			_printHandle = IntPtr.Zero;
		}

		/// <summary>
		/// Raises the System.Drawing.Printing.PrintDocument.QueryPageSettings event. It
		/// is called immediately before each System.Drawing.Printing.PrintDocument.PrintPage event.
		/// </summary>
		/// <param name="e">A System.Drawing.Printing.QueryPageSettingsEventArgs that contains the event data.</param>
		protected override void OnQueryPageSettings(QueryPageSettingsEventArgs e)
		{
			if (AutoRotate)
			{
				IntPtr currentPage = Pdfium.FPDF_StartLoadPage(_docForPrint, _pageForPrint);
				if (currentPage == IntPtr.Zero)
				{
					e.Cancel = true;
					return;
				}
				double width = Pdfium.FPDF_GetPageWidth(currentPage);
				double height = Pdfium.FPDF_GetPageHeight(currentPage);
				var rotation = Pdfium.FPDFPage_GetRotation(currentPage);
				bool isRotated = (/*rotation == PageRotate.Rotate270 || rotation == PageRotate.Rotate90 ||*/ width > height);
				e.PageSettings.Landscape = isRotated;
				if (currentPage != IntPtr.Zero)
					Pdfium.FPDF_ClosePage(currentPage);
			}

			base.OnQueryPageSettings(e);
		}

		/// <summary>
		/// Raises the System.Drawing.Printing.PrintDocument.PrintPage event. It is called
		/// before a page prints.
		/// </summary>
		/// <param name="e"> A System.Drawing.Printing.PrintPageEventArgs that contains the event data.</param>
		/// <seealso href="https://pdfium.patagames.com/c-pdf-library/">C# Print PDF</seealso>
		protected override void OnPrintPage(PrintPageEventArgs e)
		{
			base.OnPrintPage(e);
			if (_pdfDoc == null)
				throw new ArgumentNullException("Document");

			IntPtr hdc = IntPtr.Zero;
			IntPtr currentPage = IntPtr.Zero;
			try
			{
				if (e.Cancel)
					return;

				currentPage = Pdfium.FPDF_LoadPage(_docForPrint, _pageForPrint);
				if (currentPage == IntPtr.Zero)
				{
					e.Cancel = true;
					return;
				}

				double dpiX = e.Graphics.DpiX;
				double dpiY = e.Graphics.DpiY;

				double width = Pdfium.FPDF_GetPageWidth(currentPage) / 72 * dpiX;
				double height = Pdfium.FPDF_GetPageHeight(currentPage) / 72 * dpiY;
				double x, y;
				Rectangle clipRect;
				
				CalcSize(dpiX, dpiY, e.PageSettings.PrintableArea, e.MarginBounds, new PointF(e.PageSettings.HardMarginX, e.PageSettings.HardMarginY), e.PageSettings.Landscape, ref width, ref height, out x, out y, out clipRect);

				int ix = (int)x;
				int iy = (int)y;
				int iw = (int)width;
				int ih = (int)height;
				using (var page = PdfPage.FromHandle(_pdfDoc, currentPage, _pageForPrint, true))
					OnBeforeRenderPage(e.Graphics, page, ref ix, ref iy, ref iw, ref ih, PageRotate.Normal);

				hdc = e.Graphics.GetHdc();
				if (OriginAtMargins)
					Pdfium.IntersectClipRect(hdc, clipRect.Left, clipRect.Top, clipRect.Right, clipRect.Bottom);

				Pdfium.FPDF_RenderPage( hdc, currentPage, ix, iy, iw, ih, PageRotate.Normal, RenderFlags);

				if (hdc != IntPtr.Zero)
					e.Graphics.ReleaseHdc(hdc);
				hdc = IntPtr.Zero;
				using (var page = PdfPage.FromHandle(_pdfDoc, currentPage, _pageForPrint, true))
					OnAfterRenderPage(e.Graphics, page, ix, iy, iw, ih, PageRotate.Normal);

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
				if (currentPage != IntPtr.Zero)
					Pdfium.FPDF_ClosePage(currentPage);
			}
		}
        #endregion

        #region Private methods
        private IntPtr InitDocument()
		{
			if (!_useDP)
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

		private void CalcSize(double dpiX, double dpiY, RectangleF printableArea, Rectangle marginBounds, PointF hardMargin, bool isLandscape, ref double width, ref double height, out double x, out double y, out Rectangle clipRect)
		{
			x = y = 0;
			clipRect = Rectangle.Empty;
			if (_useDP)
				return;

			RectangleF forPrintArea = !OriginAtMargins ?
				new RectangleF(
					(float)(printableArea.X),
					(float)(printableArea.Y),
					printableArea.Width,
					printableArea.Height
					) :
				new RectangleF(
					(float)(marginBounds.X),
					(float)(marginBounds.Y),
					marginBounds.Width,
					marginBounds.Height);

			if (isLandscape)
				forPrintArea = new RectangleF(forPrintArea.X, forPrintArea.Y, forPrintArea.Height, forPrintArea.Width);

			//Calculate the size of the printable area in pixels
			var fitSize = new SizeF(
				(float)dpiX * forPrintArea.Width / 100.0f,
				(float)dpiY * forPrintArea.Height / 100.0f
				);
			var pageSize = new SizeF(
				(float)width,
				(float)height
				);

			if (OriginAtMargins && isLandscape)
					fitSize = new SizeF(fitSize.Height, fitSize.Width);

			switch (PrintSizeMode)
			{
				case PrintSizeMode.Fit:
					var sz = GetRenderSize(pageSize, fitSize);
					width = sz.Width;
					height = sz.Height;
					break;
				case PrintSizeMode.CustomScale:
					width *= (double)Scale / 100.0;
					height *= (double)Scale / 100.0;
					break;
				case PrintSizeMode.ActualSize:
				default:
					break;
			}

			x = forPrintArea.X * dpiX / 100 - hardMargin.X * dpiX / 100;
			y = forPrintArea.Y * dpiY / 100 - hardMargin.Y * dpiY / 100;

			if (AutoCenter)
			{
				x = x + (fitSize.Width - width) / 2;
				y = y + (fitSize.Height - height) / 2;
			}

			clipRect = new Rectangle((int)(marginBounds.Left * dpiX / 100),
				(int)(marginBounds.Top * dpiY / 100),
				(int)(marginBounds.Width * dpiX / 100),
				(int)(marginBounds.Height * dpiX / 100));
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
