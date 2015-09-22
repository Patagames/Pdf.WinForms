using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Patagames.Pdf.Enums;
using Patagames.Pdf.Net.EventArguments;

namespace Patagames.Pdf.Net.Controls.WinForms
{
	/// <summary>
	/// Represents a pdf view control for displaying an Pdf document.
	/// </summary>	
	public partial class PdfViewer : UserControl
	{
		#region Private fields
		private SelectInfo _selectInfo = new SelectInfo() { StartPage = -1 };
		private SortedDictionary<int, List<HighlightInfo>> _highlightedText = new SortedDictionary<int, List<HighlightInfo>>();
		private bool _mousePressed = false;
		private bool _mousePressedInLink = false;
		private int _onstartPageIndex = 0;

		private PdfForms _fillForms;
		private List<Rectangle> _selectedRectangles = new List<Rectangle>();
		private Pen _pageBorderColorPen;
		private Brush _selectColorBrush;
		private Pen _pageSeparatorColorPen;
		private Pen _currentPageHighlightColorPen;

		private PdfDocument _document;
		private SizeModes _sizeMode = SizeModes.FitToWidth;
		private Color _formHighlightColor;
		private Color _pageBackColor;
		private Color _pageBorderColor;
		private Color _textSelectColor;
		private Padding _pageMargin;
		private float _zoom;
		private ViewModes _viewMode;
		private Color _pageSeparatorColor;
		private bool _showPageSeparator;
		private Color _currentPageHighlightColor;
		private bool _showCurrentPageHighlight;
		private ContentAlignment _pageAlign;
		private RenderFlags _renderFlags;
		private int _tilesCount;


		private RectangleF[] _renderRects;
		private int _startPage { get { return ViewMode == ViewModes.SinglePage ? Document.Pages.CurrentIndex : 0; } }
		private int _endPage { get { return ViewMode == ViewModes.SinglePage ? Document.Pages.CurrentIndex : _renderRects.Length - 1; } }
		#endregion

		#region Events
		/// <summary>
		/// Occurs whenever the document loads.
		/// </summary>
		public event EventHandler DocumentLoaded;

		/// <summary>
		/// Occurs whenever the document unloads.
		/// </summary>
		public event EventHandler DocumentClosed;

		/// <summary>
		/// Occurs when the <see cref="SizeMode"/> property has changed.
		/// </summary>
		public event EventHandler SizeModeChanged;

		/// <summary>
		/// Event raised when the value of the <see cref="PageBackColor"/> property is changed on Control..
		/// </summary>
		public event EventHandler PageBackColorChanged;

		/// <summary>
		/// Occurs when the <see cref="PageMargin"/> property has changed.
		/// </summary>
		public event EventHandler PageMarginChanged;

		/// <summary>
		/// Event raised when the value of the <see cref="PageBorderColor"/> property is changed on Control.
		/// </summary>
		public event EventHandler PageBorderColorChanged;

		/// <summary>
		/// Event raised when the value of the <see cref="TextSelectColor"/> property is changed on Control.
		/// </summary>
		public event EventHandler TextSelectColorChanged;

		/// <summary>
		/// Event raised when the value of the <see cref="FormHighlightColor"/> property is changed on Control.
		/// </summary>
		public event EventHandler FormHighlightColorChanged;

		/// <summary>
		/// Occurs when the <see cref="Zoom"/> property has changed.
		/// </summary>
		public event EventHandler ZoomChanged;

		/// <summary>
		/// Occurs when the current selection has changed.
		/// </summary>
		public event EventHandler SelectionChanged;

		/// <summary>
		/// Occurs when the <see cref="ViewMode"/> property has changed.
		/// </summary>
		public event EventHandler ViewModeChanged;

		/// <summary>
		/// Occurs when the <see cref="PageSeparatorColor"/> property has changed.
		/// </summary>
		public event EventHandler PageSeparatorColorChanged;

		/// <summary>
		/// Occurs when the <see cref="ShowPageSeparator"/> property has changed.
		/// </summary>
		public event EventHandler ShowPageSeparatorChanged;

		/// <summary>
		/// Occurs when the <see cref="CurrentPage"/> or <see cref="CurrentIndex"/> property has changed.
		/// </summary>
		public event EventHandler CurrentPageChanged;

		/// <summary>
		/// Occurs when the <see cref="CurrentPageHighlightColor"/> property has changed.
		/// </summary>
		public event EventHandler CurrentPageHighlightColorChanged;

		/// <summary>
		/// Occurs when the <see cref="ShowCurrentPageHighlight"/> property has changed.
		/// </summary>
		public event EventHandler ShowCurrentPageHighlightChanged;

		/// <summary>
		/// Occurs when the value of the <see cref="PageAlign"/> property has changed.
		/// </summary>
		public event EventHandler PageAlignChanged;

		/// <summary>
		/// Occurs before PdfLink or WebLink on the page was clicked.
		/// </summary>
		public event EventHandler<PdfBeforeLinkClickedEventArgs> BeforeLinkClicked;

		/// <summary>
		/// Occurs after PdfLink or WebLink on the page was clicked.
		/// </summary>
		public event EventHandler<PdfAfterLinkClickedEventArgs> AfterLinkClicked;

		/// <summary>
		/// Occurs when the value of the <see cref="RenderFlags"/> property has changed.
		/// </summary>
		public event EventHandler RenderFlagsChanged;

		/// <summary>
		/// Occurs when the value of the <see cref="TilesCount"/> property has changed.
		/// </summary>
		public event EventHandler TilesCountChanged;

		/// <summary>
		/// Occurs when the text highlighting changed
		/// </summary>
		public event EventHandler HighlightedTextChanged;

		/// <summary>
		/// Occurs when the value of the <see cref="BookmarksViewer"/> property has changed.
		/// </summary>
		public event EventHandler BookmarksViewerChanged;

		/// <summary>
		/// Occurs when the value of the <see cref="NamedDestinationsViewer"/> property has changed.
		/// </summary>
		public event EventHandler NamedDestinationsViewerChanged;

		#endregion

		#region Event raises
		/// <summary>
		/// Raises the <see cref="DocumentLoaded"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnDocumentLoaded(EventArgs e)
		{
			if (DocumentLoaded != null)
				DocumentLoaded(this, e);
		}

		/// <summary>
		/// Raises the <see cref="DocumentClosed"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnDocumentClosed(EventArgs e)
		{
			if (DocumentClosed != null)
				DocumentClosed(this, e);
		}

		/// <summary>
		/// Raises the <see cref="SizeModeChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnSizeModeChanged(EventArgs e)
		{
			if (SizeModeChanged != null)
				SizeModeChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="PageBackColorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnPageBackColorChanged(EventArgs e)
		{
			if (PageBackColorChanged != null)
				PageBackColorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="PageMarginChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnPageMarginChanged(EventArgs e)
		{
			if (PageMarginChanged != null)
				PageMarginChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="PageBorderColorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnPageBorderColorChanged(EventArgs e)
		{
			if (PageBorderColorChanged != null)
				PageBorderColorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="TextSelectColorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnTextSelectColorChanged(EventArgs e)
		{
			if (TextSelectColorChanged != null)
				TextSelectColorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="FormHighlightColorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnFormHighlightColorChanged(EventArgs e)
		{
			if (FormHighlightColorChanged != null)
				FormHighlightColorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="ZoomChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnZoomChanged(EventArgs e)
		{
			if (ZoomChanged != null)
				ZoomChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="SelectionChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnSelectionChanged(EventArgs e)
		{
			if (SelectionChanged != null)
				SelectionChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="ViewModeChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnViewModeChanged(EventArgs e)
		{
			if (ViewModeChanged != null)
				ViewModeChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="PageSeparatorColorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnPageSeparatorColorChanged(EventArgs e)
		{
			if (PageSeparatorColorChanged != null)
				PageSeparatorColorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="ShowPageSeparatorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnShowPageSeparatorChanged(EventArgs e)
		{
			if (ShowPageSeparatorChanged != null)
				ShowPageSeparatorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="CurrentPageChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnCurrentPageChanged(EventArgs e)
		{
			if (CurrentPageChanged != null)
				CurrentPageChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="CurrentPageHighlightColorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnCurrentPageHighlightColorChanged(EventArgs e)
		{
			if (CurrentPageHighlightColorChanged != null)
				CurrentPageHighlightColorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="ShowCurrentPageHighlightChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnShowCurrentPageHighlightChanged(EventArgs e)
		{
			if (ShowCurrentPageHighlightChanged != null)
				ShowCurrentPageHighlightChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="PageAlignChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnPageAlignChanged(EventArgs e)
		{
			if (PageAlignChanged != null)
				PageAlignChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="BeforeLinkClicked"/> event.
		/// </summary>
		/// <param name="e">An PdfBeforeLinkClickedEventArgs that contains the event data.</param>
		protected virtual void OnBeforeLinkClicked(PdfBeforeLinkClickedEventArgs e)
		{
			if (BeforeLinkClicked != null)
				BeforeLinkClicked(this, e);
		}

		/// <summary>
		/// Raises the <see cref="AfterLinkClicked"/> event.
		/// </summary>
		/// <param name="e">An PdfAfterLinkClickedEventArgs that contains the event data.</param>
		protected virtual void OnAfterLinkClicked(PdfAfterLinkClickedEventArgs e)
		{
			if (AfterLinkClicked != null)
				AfterLinkClicked(this, e);
		}

		/// <summary>
		/// Raises the <see cref="RenderFlagsChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnRenderFlagsChanged(EventArgs e)
		{
			if (RenderFlagsChanged != null)
				RenderFlagsChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="TilesCountChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnTilesCountChanged(EventArgs e)
		{
			if (TilesCountChanged != null)
				TilesCountChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="HighlightedTextChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnHighlightedTextChanged(EventArgs e)
		{
			if (HighlightedTextChanged != null)
				HighlightedTextChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="BookmarksViewerChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnBookmarksViewerChanged(EventArgs e)
		{
			if (BookmarksViewerChanged != null)
				BookmarksViewerChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="NamedDestinationsViewerChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnNamedDestinationsViewerChanged(EventArgs e)
		{
			if (NamedDestinationsViewerChanged != null)
				NamedDestinationsViewerChanged(this, e);
		}

		#endregion

		#region Public properties
		/// <summary>
		/// Gets or sets the Forms object associated with the current PdfViewer control.
		/// </summary>
		/// <remarks>The FillForms object are used for the correct processing of forms within the PdfViewer control</remarks>
		public PdfForms FillForms { get { return _fillForms; } }

		/// <summary>
		/// Gets or sets the PDF document associated with the current PdfViewer control.
		/// </summary>
		public PdfDocument Document
		{
			get
			{
				return _document;
			}
			set
			{
				if (!DesignMode && !AllowSetDocument && value != null)
					throw new ArgumentException(Properties.Error.err0001, "AllowSetDocument");
				if (_document != value)
				{
					CloseDocument();
					_document = value;
					UpdateLayout();
					if (_document != null)
					{
						_document.Pages.CurrentPageChanged += Pages_CurrentPageChanged;
						_document.Pages.PageInserted += Pages_PageInserted;
						_document.Pages.PageDeleted += Pages_PageDeleted;
						SetCurrentPage(_onstartPageIndex);
						ScrollToPage(_onstartPageIndex);
						SetupControls();
						OnDocumentLoaded(EventArgs.Empty);
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the background color for the control under PDF page.
		/// </summary>
		public Color PageBackColor
		{
			get
			{
				return _pageBackColor;
			}
			set
			{
				if (_pageBackColor != value)
				{
					_pageBackColor = value;
					Invalidate();
					OnPageBackColorChanged(EventArgs.Empty);
				}

			}
		}

		/// <summary>
		/// Specifies space between pages margins
		/// </summary>
		public Padding PageMargin
		{
			get
			{
				return _pageMargin;
			}
			set
			{
				if (_pageMargin != value)
				{
					_pageMargin = value;
					UpdateLayout();
					OnPageMarginChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets the border color of the page
		/// </summary>
		public Color PageBorderColor
		{
			get
			{
				return _pageBorderColor;
			}
			set
			{
				if (_pageBorderColor != value)
				{
					_pageBorderColor = value;
					if (_pageBorderColorPen != null)
						_pageBorderColorPen.Dispose();
					_pageBorderColorPen = new Pen(_pageBorderColor);
					Invalidate();
					OnPageBorderColorChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Control how the PdfViewer will handle  pages placement and control sizing
		/// </summary>
		public SizeModes SizeMode
		{
			get
			{
				return _sizeMode;
			}
			set
			{
				if (_sizeMode != value)
				{
					_sizeMode = value;
					UpdateLayout();
					OnSizeModeChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets the selection color of the control.
		/// </summary>
		public Color TextSelectColor
		{
			get
			{
				return _textSelectColor;
			}
			set
			{
				if (_textSelectColor != value)
				{
					_textSelectColor = value;
					if (_selectColorBrush != null)
						_selectColorBrush.Dispose();
					_selectColorBrush = new SolidBrush(_textSelectColor);
					Invalidate();
					OnTextSelectColorChanged(EventArgs.Empty);
				}

			}
		}

		/// <summary>
		/// Gets or set the highlight color of the form fields in the document.
		/// </summary>
		public Color FormHighlightColor
		{
			get
			{
				return _formHighlightColor;
			}
			set
			{
				if (_formHighlightColor != value)
				{
					_formHighlightColor = value;
					if (Document != null && Document.FormFill != null)
						Document.FormFill.SetHighlightColor(FormFieldTypes.FPDF_FORMFIELD_UNKNOWN, _formHighlightColor);
					Invalidate();
					OnFormHighlightColorChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// This property allows you to scale the PDF page. To take effect the <see cref="SizeMode"/> property should be Zoom
		/// </summary>
		public float Zoom
		{
			get
			{
				return _zoom;
			}
			set
			{
				if (_zoom != value)
				{
					_zoom = value;
					UpdateLayout();
					OnZoomChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets selected text from PdfView control
		/// </summary>
		public string SelectedText
		{
			get
			{
				if (Document == null)
					return "";

				var selTmp = NormalizeSelectionInfo();

				if (selTmp.StartPage < 0 || selTmp.StartIndex < 0)
					return "";

				string ret = "";
				for (int i = selTmp.StartPage; i <= selTmp.EndPage; i++)
				{
					if (ret != "")
						ret += "\r\n";

					int s = 0;
					if (i == selTmp.StartPage)
						s = selTmp.StartIndex;

					int len = Document.Pages[i].Text.CountChars;
					if (i == selTmp.EndPage)
						len = (selTmp.EndIndex+1) - s;

					ret += Document.Pages[i].Text.GetText(s, len);
				}
				return ret;
			}
		}

		/// <summary>
		/// Gets information about selected text in a PdfView control
		/// </summary>
		public SelectInfo SelectInfo { get { return NormalizeSelectionInfo(); } }

		/// <summary>
		/// Control how the PdfViewer will display pages
		/// </summary>
		public ViewModes ViewMode
		{
			get
			{
				return _viewMode;
			}
			set
			{
				if (_viewMode != value)
				{
					_viewMode = value;
					UpdateLayout();
					OnViewModeChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets the page separator color.
		/// </summary>
		public Color PageSeparatorColor
		{
			get
			{
				return _pageSeparatorColor;
			}
			set
			{
				if (_pageSeparatorColor != value)
				{
					_pageSeparatorColor = value;
					if (_pageSeparatorColorPen != null)
						_pageSeparatorColorPen.Dispose();
					_pageSeparatorColorPen = new Pen(_pageSeparatorColor);
					Invalidate();
					OnPageSeparatorColorChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Determines whether the page separator is visible or hidden
		/// </summary>
		public bool ShowPageSeparator
		{
			get
			{
				return _showPageSeparator;
			}
			set
			{
				if (_showPageSeparator != value)
				{
					_showPageSeparator = value;
					Invalidate();
					OnShowPageSeparatorChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets the current page highlight color.
		/// </summary>
		public Color CurrentPageHighlightColor
		{
			get
			{
				return _currentPageHighlightColor;
			}
			set
			{
				if (_currentPageHighlightColor != value)
				{
					_currentPageHighlightColor = value;
					if (_currentPageHighlightColorPen != null)
						_currentPageHighlightColorPen.Dispose();
					_currentPageHighlightColorPen = new Pen(_currentPageHighlightColor, 4);
					Invalidate();
					OnCurrentPageHighlightColorChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Determines whether the current page's highlight is visible or hidden.
		/// </summary>
		public bool ShowCurrentPageHighlight
		{
			get
			{
				return _showCurrentPageHighlight;
			}
			set
			{
				if (_showCurrentPageHighlight != value)
				{
					_showCurrentPageHighlight = value;
					Invalidate();
					OnShowCurrentPageHighlightChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets the current index of a page in PdfPageCollection
		/// </summary>
		public int CurrentIndex
		{
			get
			{
				if (Document == null)
					return -1;
				return Document.Pages.CurrentIndex;
			}
			set
			{
				if (Document == null)
					return;
				Document.Pages.CurrentIndex = value;
			}
		}

		/// <summary>
		/// Gets the current PdfPage item by <see cref="CurrentIndex "/>
		/// </summary>
		public PdfPage CurrentPage { get { return Document.Pages.CurrentPage; } }

		/// <summary>
		/// Gets or sets a value indicating whether the control can accept PDF document through Document property.
		/// </summary>
		public bool AllowSetDocument { get; set; }

		/// <summary>
		/// Gets or sets the alignment of page in the control.
		/// </summary>
		public ContentAlignment PageAlign
		{
			get
			{
				return _pageAlign;
			}
			set
			{
				if(_pageAlign!= value)
				{
					_pageAlign = value;
					UpdateLayout();
					OnPageAlignChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets a RenderFlags. None for normal display, or combination of <see cref="RenderFlags"/>
		/// </summary>
		public RenderFlags RenderFlags
		{
			get
			{
				return _renderFlags;
			}
			set
			{
				if(_renderFlags!= value)
				{
					_renderFlags = value;
					Invalidate();
					OnRenderFlagsChanged(EventArgs.Empty);
				}
			}
		}
		
		/// <summary>
		/// Gets or sets visible page count for tiles view mode
		/// </summary>
		public int TilesCount
		{
			get
			{
				return _tilesCount;
			}
			set
			{
				int tmp = value < 2 ? 2 : value;
				if (_tilesCount != tmp)
				{
					_tilesCount = tmp;
					UpdateLayout();
					OnTilesCountChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets information about highlighted text in a PdfView control
		/// </summary>
		public SortedDictionary<int, List<HighlightInfo>> HighlightedTextInfo { get { return _highlightedText; } }

		/// <summary>
		/// Gets or sets the <see cref="BookmarksViewer"/> control associated with current PdfViewer control
		/// </summary>
		public BookmarksViewer BookmarksViewer
		{
			get
			{
				return _bookmarksViewer;
			}
			set
			{
				if (_bookmarksViewer != value)
				{
					UnsubscribeBookmarksEvent();

					_bookmarksViewer = value;
					if (_bookmarksViewer != null)
						_bookmarksViewer.Document = Document;

					SubscribeBookmarksEvent();

					OnBookmarksViewerChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="NamedDestinationsViewer"/> control associated with current PdfViewer control
		/// </summary>
		public NamedDestinationsViewer NamedDestinationsViewer
		{
			get
			{
				return _namedDestinationsViewer;
			}
			set
			{
				if (_namedDestinationsViewer != value)
				{
					UnsubscribeNamedDestinationsEvent();

					_namedDestinationsViewer = value;
					if (_namedDestinationsViewer != null)
						_namedDestinationsViewer.Document = Document;

					SubscribeNamedDestinationsEvent();

					OnNamedDestinationsViewerChanged(EventArgs.Empty);
				}
			}
		}
		#endregion

		#region Public methods
		/// <summary>
		/// Scrolls the control view to the specified page.
		/// </summary>
		/// <param name="index">Zero-based index of a page.</param>
		public void ScrollToPage(int index)
		{
			if (Document == null)
				return;
			if (ViewMode == ViewModes.SinglePage)
			{
				SetCurrentPage(index);
				Invalidate();
			}
			else
			{
				var rect = RFTR(renderRects(index));
				AutoScrollPosition = new Point(rect.X, rect.Y);
			}
		}

		/// <summary>
		/// Rotates the specified page to the specified angle.
		/// </summary>
		/// <param name="pageIndex">Zero-based index of a page for rotation.</param>
		/// <param name="angle">The angle which must be turned page</param>
		/// <remarks>The PDF page rotates clockwise. See <see cref="PageRotate"/> for details.</remarks>
		public void RotatePage(int pageIndex, PageRotate angle)
		{
			if (Document == null)
				return;
			Document.Pages[pageIndex].Rotation = angle;
			UpdateLayout();

		}

		/// <summary>
		/// Selects the text contained in specified pages.
		/// </summary>
		/// <param name="SelInfo"><see cref="SelectInfo"/> structure that describe text selection parameters.</param>
		public void SelectText(SelectInfo SelInfo)
		{
			SelectText(SelInfo.StartPage, SelInfo.StartIndex, SelInfo.EndPage, SelInfo.EndIndex);
		}

		/// <summary>
		/// Selects the text contained in specified pages.
		/// </summary>
		/// <param name="startPage">Zero-based index of a starting page.</param>
		/// <param name="startIndex">Zero-based char index on a startPage.</param>
		/// <param name="endPage">Zero-based index of a ending page.</param>
		/// <param name="endIndex">Zero-based char index on a endPage.</param>
		public void SelectText(int startPage, int startIndex, int endPage, int endIndex)
		{
			if (Document == null)
				return;

			if (startPage < 0)
				startPage = 0;
			if (startPage > Document.Pages.Count - 1)
				startPage = Document.Pages.Count - 1;

			if (endPage < 0)
				endPage = 0;
			if (endPage > Document.Pages.Count - 1)
				endPage = Document.Pages.Count - 1;

			int startCnt = Document.Pages[startPage].Text.CountChars;
			int endCnt = Document.Pages[endPage].Text.CountChars;

			if (startIndex < 0)
				startIndex = 0;
			if (startIndex > startCnt - 1)
				startIndex = startCnt - 1;

			if (endIndex < 0)
				endIndex = 0;
			if (endIndex > endCnt - 1)
				endIndex = endCnt - 1;

			_selectInfo = new SelectInfo()
			{
				StartPage = startPage,
				StartIndex = startIndex,
				EndPage = endPage,
				EndIndex = endIndex
			};
			Invalidate();
			OnSelectionChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Clear text selection
		/// </summary>
		public void DeselectText()
		{
			_selectInfo = new SelectInfo()
			{
				StartPage = -1,
			};
			Invalidate();
			OnSelectionChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Determines if the specified point is contained within Pdf page.
		/// </summary>
		/// <param name="pt">The System.Drawing.Point to test.</param>
		/// <returns>
		/// This method returns the zero based page index if the point represented by pt is contained within this page; otherwise -1.
		/// </returns>
		public int PointInPage(Point pt)
		{
			for (int i = _startPage; i <= _endPage; i++)
			{
				//Actual coordinates of the page with the scroll
				Rectangle actualRect = CalcActualRect(i);
				if (actualRect.Contains(pt))
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Computes the location of the specified client point into page coordinates.
		/// </summary>
		/// <param name="pageIndex">Page index. Can be obtained by <see cref="PointInPage"/> method.</param>
		/// <param name="pt">The client coordinate Point to convert. </param>
		/// <returns>A Point that represents the converted Point, pt, in page coordinates.</returns>
		/// <exception cref="IndexOutOfRangeException">The page index is out of range</exception>
		/// <remarks>Permitted range of pages depends on the current view type and on some other parameters in the control.</remarks>
		public PointF ClientToPage(int pageIndex, Point pt)
		{
			if (pageIndex < _startPage || pageIndex > _endPage)
				throw new IndexOutOfRangeException(Properties.Error.err0002);
			var page = Document.Pages[pageIndex];
			var ar = CalcActualRect(pageIndex);
			return page.DeviceToPage(ar.X, ar.Y, ar.Width, ar.Height, PageRotation(page), pt.X, pt.Y);
		}

		/// <summary>
		/// Computes the location of the specified page point into client coordinates.
		/// </summary>
		/// <param name="pageIndex">Page index. Can be obtained by <see cref="PointInPage"/> method.</param>
		/// <param name="pt">The page coordinate Point to convert. </param>
		/// <returns>A Point that represents the converted Point, pt, in client coordinates.</returns>
		/// <exception cref="IndexOutOfRangeException">The page index is out of range</exception>
		/// <remarks>Permitted range of pages depends on the current view type and on some other parameters in the control.</remarks>
		public Point PageToClient(int pageIndex, PointF pt)
		{
			if(pageIndex < _startPage || pageIndex > _endPage)
				throw new IndexOutOfRangeException(Properties.Error.err0002);
			var page = Document.Pages[pageIndex];
			var ar = CalcActualRect(pageIndex);
			return page.PageToDevice(ar.X, ar.Y, ar.Width, ar.Height, PageRotation(page), pt.X, pt.Y);
		}


		/// <summary>
		/// Highlight text on the page
		/// </summary>
		/// <param name="pageIndex">Zero-based index of the page</param>
		/// <param name="highlightInfo">Sets the options for highlighting text</param>
		public void HighlightText(int pageIndex, HighlightInfo highlightInfo)
		{
			HighlightText(pageIndex, highlightInfo.CharIndex, highlightInfo.CharsCount, highlightInfo.Color);
		}

		/// <summary>
		/// Highlight text on the page
		/// </summary>
		/// <param name="pageIndex">Zero-based index of the page</param>
		/// <param name="charIndex">Zero-based char index on the page.</param>
		/// <param name="charsCount">The number of highlighted characters on the page or -1 for highlight text from charIndex to end of the page.</param>
		/// <param name="color">Highlight color</param>
		public void HighlightText(int pageIndex, int charIndex, int charsCount, Color color)
		{
			//normalize all user input
			if (pageIndex < 0)
				pageIndex = 0;
			if (pageIndex > Document.Pages.Count - 1)
				pageIndex = Document.Pages.Count - 1;

			int charsCnt = Document.Pages[pageIndex].Text.CountChars;
			if (charIndex < 0)
				charIndex = 0;
			if (charIndex > charsCnt - 1)
				charIndex = charsCnt - 1;
			if (charsCount < 0)
				charsCount = charsCnt - charIndex;
			if (charIndex + charsCount > charsCnt - 1)
				charsCount = charsCnt - 1 - charIndex;
			if (charsCount <= 0)
				return;

			var newEntry = new HighlightInfo() { CharIndex = charIndex, CharsCount = charsCount, Color = color };

			if (!_highlightedText.ContainsKey(pageIndex))
			{
				if (color != Color.Empty)
				{
					_highlightedText.Add(pageIndex, new List<HighlightInfo>());
					_highlightedText[pageIndex].Add(newEntry);
				}
			}
			else
			{
				var entries = _highlightedText[pageIndex];
				//Analize exists entries and remove overlapped and trancate intersecting entries
				for (int i = entries.Count - 1; i >= 0; i--)
				{
					List<HighlightInfo> calcEntries;
					if (CalcIntersectEntries(entries[i], newEntry, out calcEntries))
					{
						if (calcEntries.Count == 0)
							entries.RemoveAt(i);
						else
							for (int j = 0; j < calcEntries.Count; j++)
								if (j == 0)
									entries[i] = calcEntries[j];
								else
									entries.Insert(i, calcEntries[j]);
					}
				}
				if (color != Color.Empty)
					entries.Add(newEntry);
			}

			Invalidate();
			OnHighlightedTextChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Removes highlight from the text
		/// </summary>
		/// <param name="pageIndex">Zero-based index of the page</param>
		/// <param name="charIndex">Zero-based char index on the page.</param>
		/// <param name="charsCount">The number of highlighted characters on the page or -1 for highlight text from charIndex to end of the page.</param>
		public void RemoveHighlightFromText(int pageIndex, int charIndex, int charsCount)
		{
			HighlightText(pageIndex, charIndex, charsCount, Color.Empty);
		}

		/// <summary>
		/// Highlight selected text on the page by specified color
		/// </summary>
		/// <param name="color">Highlight color</param>
		public void HilightSelectedText(Color color)
		{
			var selInfo = SelectInfo;
			if (selInfo.StartPage < 0 || selInfo.StartIndex < 0)
				return;

			for (int i = selInfo.StartPage; i <= selInfo.EndPage; i++)
			{
				int start = (i == selInfo.StartPage ? selInfo.StartIndex : 0);
				int len = (i == selInfo.EndPage ? selInfo.EndIndex - start : -1);
				HighlightText(i, start, len, color);
			}
		}

		/// <summary>
		/// Removes highlight from selected text
		/// </summary>
		public void RemoveHilightFromSelectedText()
		{
			HilightSelectedText(Color.Empty);
		}

		/// <summary>
		/// Ensures that all sizes and positions of pages of a PdfViewer control are properly updated for layout.
		/// </summary>
		public void UpdateLayout()
		{
			OnResize(EventArgs.Empty);
		}
		#endregion

		#region Load and Close document
		/// <summary>
		/// Open and load a PDF document from a file.
		/// </summary>
		/// <param name="path">Path to the PDF file (including extension)</param>
		/// <param name="password">A string used as the password for PDF file. If no password needed, empty or NULL can be used.</param>
		/// <exception cref="Exceptions.UnknownErrorException">unknown error</exception>
		/// <exception cref="Exceptions.PdfFileNotFoundException">file not found or could not be opened</exception>
		/// <exception cref="Exceptions.BadFormatException">file not in PDF format or corrupted</exception>
		/// <exception cref="Exceptions.InvalidPasswordException">password required or incorrect password</exception>
		/// <exception cref="Exceptions.UnsupportedSecuritySchemeException">unsupported security scheme</exception>
		/// <exception cref="Exceptions.PdfiumException">Error occured in PDFium. See ErrorCode for detail</exception>
		public void LoadDocument(string path, string password = null)
		{
			CloseDocument();
			_document = PdfDocument.Load(path, _fillForms, password);
			UpdateLayout();
			_document.Pages.CurrentPageChanged += Pages_CurrentPageChanged;
			_document.Pages.PageInserted += Pages_PageInserted;
			_document.Pages.PageDeleted += Pages_PageDeleted;
			SetCurrentPage(_onstartPageIndex);
			ScrollToPage(_onstartPageIndex);
			SetupControls();
			OnDocumentLoaded(EventArgs.Empty);

		}

		/// <summary>
		/// Loads the PDF document from the specified stream.
		/// </summary>
		/// <param name="stream">The stream containing the PDF document to load. The stream must support seeking.</param>
		/// <param name="password">A string used as the password for PDF file. If no password needed, empty or NULL can be used.</param>
		/// <exception cref="Exceptions.UnknownErrorException">unknown error</exception>
		/// <exception cref="Exceptions.PdfFileNotFoundException">file not found or could not be opened</exception>
		/// <exception cref="Exceptions.BadFormatException">file not in PDF format or corrupted</exception>
		/// <exception cref="Exceptions.InvalidPasswordException">password required or incorrect password</exception>
		/// <exception cref="Exceptions.UnsupportedSecuritySchemeException">unsupported security scheme</exception>
		/// <exception cref="Exceptions.PdfiumException">Error occured in PDFium. See ErrorCode for detail</exception>
		/// <remarks><note type="note">The application should maintain the stream resources being valid until the PDF document close.</note></remarks>
		public void LoadDocument(Stream stream, string password = null)
		{
			CloseDocument();
			_document = PdfDocument.Load(stream, _fillForms, password);
			UpdateLayout();
			_document.Pages.CurrentPageChanged += Pages_CurrentPageChanged;
			_document.Pages.PageInserted += Pages_PageInserted;
			_document.Pages.PageDeleted += Pages_PageDeleted;
			SetCurrentPage(_onstartPageIndex);
			ScrollToPage(_onstartPageIndex);
			SetupControls();
			OnDocumentLoaded(EventArgs.Empty);
		}

		/// <summary>
		/// Loads the PDF document from the specified byte array.
		/// </summary>
		/// <param name="pdf">The byte array containing the PDF document to load.</param>
		/// <param name="password">A string used as the password for PDF file. If no password needed, empty or NULL can be used.</param>
		/// <exception cref="Exceptions.UnknownErrorException">unknown error</exception>
		/// <exception cref="Exceptions.PdfFileNotFoundException">file not found or could not be opened</exception>
		/// <exception cref="Exceptions.BadFormatException">file not in PDF format or corrupted</exception>
		/// <exception cref="Exceptions.InvalidPasswordException">password required or incorrect password</exception>
		/// <exception cref="Exceptions.UnsupportedSecuritySchemeException">unsupported security scheme</exception>
		/// <exception cref="Exceptions.PdfiumException">Error occured in PDFium. See ErrorCode for detail</exception>
		/// <remarks><note type="note">The application should not modify the byte array resources being valid until the PDF document close.</note></remarks>
		public void LoadDocument(byte[] pdf, string password = null)
		{
			CloseDocument();
			_document = PdfDocument.Load(pdf, _fillForms, password);
			UpdateLayout();
			_document.Pages.CurrentPageChanged += Pages_CurrentPageChanged;
			_document.Pages.PageInserted += Pages_PageInserted;
			_document.Pages.PageDeleted += Pages_PageDeleted;
			SetCurrentPage(_onstartPageIndex);
			ScrollToPage(_onstartPageIndex);
			SetupControls();
			OnDocumentLoaded(EventArgs.Empty);
		}

		/// <summary>
		/// Close a loaded PDF document.
		/// </summary>
		public void CloseDocument()
		{
			if (_document != null)
			{
				ReleaseControls();
				_document.Dispose();
				OnDocumentClosed(EventArgs.Empty);
			}
			_document = null;

		}
		#endregion

		#region Constructors and initialization
		/// <summary>
		/// Initializes a new instance of the PdfViewer class.
		/// </summary>
		public PdfViewer()
		{
			BackColor = SystemColors.ControlDark;
			PageBackColor = Color.White;
			PageBorderColor = Color.Black;
			FormHighlightColor = Color.Transparent;
			TextSelectColor = Color.FromArgb(70, Color.SteelBlue.R, Color.SteelBlue.G, Color.SteelBlue.B);
			Zoom = 1;
			PageMargin = new System.Windows.Forms.Padding(10);
			ViewMode = ViewModes.Vertical;
			ShowPageSeparator = true;
			PageSeparatorColor = Color.Gray;
			CurrentPageHighlightColor = Color.FromArgb(170, Color.SteelBlue.R, Color.SteelBlue.G, Color.SteelBlue.B);
			ShowCurrentPageHighlight = true;
			PageAlign = ContentAlignment.MiddleCenter;
			RenderFlags = Enums.RenderFlags.FPDF_ANNOT;
			TilesCount = 2;

			InitializeComponent();
			DoubleBuffered = true;

			_fillForms = new PdfForms();
			_fillForms.SetHighlightColor(FormFieldTypes.FPDF_FORMFIELD_UNKNOWN, _formHighlightColor);
			_fillForms.AppAlert += _forms_AppAlert;
			_fillForms.AppBeep += _forms_AppBeep;
			_fillForms.AppResponse += _forms_AppResponse;
			_fillForms.BrowseFile += _forms_BrowseFile;
			_fillForms.DoGotoAction += _forms_DoGotoAction;
			_fillForms.DoNamedAction += _forms_DoNamedAction;
			_fillForms.DoURIAction += _forms_DoURIAction;
			_fillForms.GotoPage += _forms_GotoPage;
			_fillForms.Invalidate += _forms_Invalidate;
			_fillForms.OutputSelectedRect += _forms_OutputSelectedRect;
			_fillForms.GetDocumentPath += Forms_GetDocumentPath;
			_fillForms.SetCursor += Forms_SetCursor;


		}

		#endregion

		#region Overrides
		/// <summary>
		/// Raises the System.Windows.Forms.Control.MouseWheel event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.MouseEventArgs that contains the event data.</param>
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (Document != null)
			{
				int idx = CalcCurrentPage();
				if (idx >= 0)
				{
					SetCurrentPage(idx);
					Invalidate();
				}
			}

			base.OnMouseWheel(e);
		}

		/// <summary>
		/// Raises the System.Windows.Forms.ScrollableControl.Scroll event
		/// </summary>
		/// <param name="se">A System.Windows.Forms.ScrollEventArgs that contains the event data.</param>
		protected override void OnScroll(ScrollEventArgs se)
		{
			if (Document != null)
			{
				int idx = CalcCurrentPage();
				if (idx >= 0)
				{
					SetCurrentPage(idx);
					Invalidate();
				}
			}

			base.OnScroll(se);
		}

		/// <summary>
		/// Raises the Resize event
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected override void OnResize(EventArgs e)
		{
			if (Document != null)
			{
				SizeF size;
				switch (ViewMode)
				{
					case ViewModes.Vertical:
						size = CalcVertical();
						break;
					case ViewModes.Horizontal:
						size = CalcHorizontal();
						break;
					case ViewModes.TilesVertical:
						size = CalcTilesVertical();
						break;
					default:
						size = CalcSingle();
						break;
				}

				if (size.Width != 0 && size.Height != 0)
				{
					AutoScrollMinSize = new Size((int)size.Width, (int)size.Height);
					Invalidate();
				}
			}
			base.OnResize(e);
		}

		/// <summary>
		/// Raises the System.Windows.Forms.Control.Paint event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.PaintEventArgs that contains the event data.</param>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (Document != null && _renderRects != null)
			{
				//Normalize info about text selection
				SelectInfo selTmp = NormalizeSelectionInfo();

				//For store coordinates of pages separators
				var separator = new List<Point>();

				//starting draw pages in vertical or horizontal modes
				for (int i = _startPage; i <= _endPage; i++)
				{
					//Actual coordinates of the page with the scroll
					Rectangle actualRect = CalcActualRect(i);
					if (!actualRect.IntersectsWith(ClientRectangle))
						continue; //Page is invisible. Skip it

					//Draw page
					DrawPage(e.Graphics, Document.Pages[i], actualRect);
					//Draw fillforms selection
					DrawFillFormsSelection(e.Graphics);
					//Draw text highlight
					DrawTextHighlight(e.Graphics, i);
					//Draw text selectionn
					DrawTextSelection(e.Graphics, selTmp, i);
					//Draw current page highlight
					DrawCurrentPageHighlight(e.Graphics, i, actualRect);
					//Calc coordinates for page separator
					CalcPageSeparator(actualRect, i, ref separator);
				}

				//Draw pages separators
				DrawPageSeparators(e.Graphics, ref separator);

				_selectedRectangles.Clear();
			}

			base.OnPaint(e);


		}

		private void DrawTextHighlight(Graphics graphics, int pageIndex)
		{
			if (!_highlightedText.ContainsKey(pageIndex))
				return;

			var entries = _highlightedText[pageIndex];
			foreach(var e in entries)
			{
				var textInfo = Document.Pages[pageIndex].Text.GetTextInfo(e.CharIndex, e.CharsCount);
				foreach(var rc in textInfo.Rects)
				{
					var pt1 = PageToDevice(rc.left, rc.top, pageIndex);
					var pt2 = PageToDevice(rc.right, rc.bottom, pageIndex);
                    int x = pt1.X < pt2.X ? pt1.X : pt2.X;
                    int y = pt1.Y < pt2.Y ? pt1.Y : pt2.Y;
                    int w = pt1.X > pt2.X ? pt1.X - pt2.X : pt2.X - pt1.X;
                    int h = pt1.Y > pt2.Y ? pt1.Y - pt2.Y : pt2.Y - pt1.Y;
                    graphics.FillRectangle(e.Brush, new Rectangle(x, y, w, h));
				}
			}

		}

		/// <summary>
		/// Raises the System.Windows.Forms.Control.MouseDoubleClick event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.MouseEventArgs that contains the event data.</param>
		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				if (Document != null)
				{
					PointF page_point;
					int idx = DeviceToPage(e.X, e.Y, out page_point);
					if (idx >= 0)
					{
						var page = Document.Pages[idx];
                        page.OnLButtonDown(0, page_point.X, page_point.Y);

						int si, ei;
						int ci = page.Text.GetCharIndexAtPos(page_point.X, page_point.Y, 10.0f, 10.0f);
						if(GetWord(page.Text, ci, out si, out ei))
						{
							_selectInfo = new SelectInfo()
							{
								StartPage = idx,
								EndPage = idx,
								StartIndex = si,
								EndIndex = ei
							};
							if (_selectInfo.StartPage >= 0)
								OnSelectionChanged(EventArgs.Empty);
							Invalidate();
						}
					}
				}
			}

			base.OnMouseDoubleClick(e);
		}


		/// <summary>
		/// Raises the System.Windows.Forms.Control.MouseDown event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.MouseEventArgs that contains the event data.</param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				if (Document != null)
				{
					PointF page_point;
					int idx = DeviceToPage(e.X, e.Y, out page_point);
					if (idx >= 0)
					{
						Document.Pages[idx].OnLButtonDown(0, page_point.X, page_point.Y);
						SetCurrentPage(idx);

						var pdfLink = Document.Pages[idx].Links.GetLinkAtPoint(page_point);
						var webLink = Document.Pages[idx].Text.WebLinks.GetWebLinkAtPoint(page_point);
						if (webLink != null || pdfLink != null)
							_mousePressedInLink = true;
						else
							_mousePressedInLink = false;

						if (_selectInfo.StartPage >= 0)
							OnSelectionChanged(EventArgs.Empty);
						_mousePressed = true;
						_selectInfo = new SelectInfo()
						{
							StartPage = idx,
							EndPage = idx,
							StartIndex = Document.Pages[idx].Text.GetCharIndexAtPos(page_point.X, page_point.Y, 10.0f, 10.0f),
							EndIndex = Document.Pages[idx].Text.GetCharIndexAtPos(page_point.X, page_point.Y, 10.0f, 10.0f)
						};
						Invalidate();
					}
				}
			}

			base.OnMouseDown(e);
		}

		/// <summary>
		/// Raises the System.Windows.Forms.Control.MouseMove event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.MouseEventArgs that contains the event data.</param>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (Document != null)
			{
				PointF page_point;
				int idx = DeviceToPage(e.X, e.Y, out page_point);

				if (idx >= 0)
				{
					int ei = Document.Pages[idx].Text.GetCharIndexAtPos(page_point.X, page_point.Y, 10.0f, 10.0f);

					if (!Document.Pages[idx].OnMouseMove(0, page_point.X, page_point.Y))
					{
						if (ei >= 0)
							Cursor = Cursors.IBeam;
						else
							Cursor = DefaultCursor;

					}

					var pdfLink = Document.Pages[idx].Links.GetLinkAtPoint(page_point);
					var webLink = Document.Pages[idx].Text.WebLinks.GetWebLinkAtPoint(page_point);
					if (webLink != null || pdfLink != null)
						Cursor = Cursors.Hand;

					if (_mousePressed)
					{
						if (ei >= 0)
						{
							_selectInfo = new SelectInfo()
							{
								StartPage = _selectInfo.StartPage,
								EndPage = idx,
								EndIndex = ei,
								StartIndex = _selectInfo.StartIndex
							};
							if(ei==6)
							{
								int ijk = 0;
							}
							System.Diagnostics.Trace.TraceInformation("sp={0}, ep={1}, si={2}, ei={3}",
								_selectInfo.StartPage, _selectInfo.EndPage,
								_selectInfo.StartIndex, _selectInfo.EndIndex);
						}
						Invalidate();
					}

				}
			}

			base.OnMouseMove(e);
		}

		/// <summary>
		/// Raises the System.Windows.Forms.Control.MouseUp event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.MouseEventArgs that contains the event data.</param>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			_mousePressed = false;
			if (Document != null)
			{
				if (_selectInfo.StartPage >= 0)
					OnSelectionChanged(EventArgs.Empty);

				PointF page_point;
				int idx = DeviceToPage(e.X, e.Y, out page_point);
				if (idx >= 0)
				{
					Document.Pages[idx].OnLButtonUp(0, page_point.X, page_point.Y);

					if (_mousePressedInLink)
					{
						var pdfLink = Document.Pages[idx].Links.GetLinkAtPoint(page_point);
						var webLink = Document.Pages[idx].Text.WebLinks.GetWebLinkAtPoint(page_point);
						if (webLink != null || pdfLink != null)
							ProcessLinkClicked(pdfLink, webLink);
					}
				}
			}

			base.OnMouseUp(e);
		}

		/// <summary>
		/// Raises the System.Windows.Forms.Control.KeyDown event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.KeyEventArgs that contains the event data.</param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (Document != null)
			{
				KeyboardModifiers mod = (KeyboardModifiers)0;

				if ((e.Modifiers & Keys.Control) != 0)
					mod |= KeyboardModifiers.ControlKey;
				if ((e.Modifiers & Keys.Shift) != 0)
					mod |= KeyboardModifiers.ShiftKey;
				if ((e.Modifiers & Keys.Alt) != 0)
					mod |= KeyboardModifiers.AltKey;

				Document.Pages.CurrentPage.OnKeyDown((FWL_VKEYCODE)e.KeyCode, mod);
			}
			base.OnKeyDown(e);
		}

		/// <summary>
		/// Raises the System.Windows.Forms.Control.KeyUp event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.KeyEventArgs that contains the event data.</param>
		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (Document != null)
			{
				Document.Pages.CurrentPage.OnKeyUp((FWL_VKEYCODE)e.KeyValue, (KeyboardModifiers)e.Modifiers);
			}
			base.OnKeyUp(e);
		}

		/// <summary>
		/// Determines whether the specified key is a regular input key or a special key that requires preprocessing.
		/// </summary>
		/// <param name="keyData">One of the System.Windows.Forms.Keys values.</param>
		/// <returns>true if the specified key is a regular input key; otherwise, false.</returns>
		protected override bool IsInputKey(Keys keyData)
		{
			if (Document == null)
				return base.IsInputKey(keyData);
			return true;
		}


		#endregion

		#region Private methods
		private void ProcessLinkClicked(PdfLink pdfLink, PdfWebLink webLink)
		{
			var args = new PdfBeforeLinkClickedEventArgs(webLink, pdfLink);
			OnBeforeLinkClicked(args);
			if (args.Cancel)
				return;
			if (pdfLink != null && pdfLink.Destination != null)
				ProcessDestination(pdfLink.Destination);
			else if (pdfLink != null && pdfLink.Action != null)
				ProcessAction(pdfLink.Action);
			else if (webLink != null)
				Process.Start(webLink.Url);
			OnAfterLinkClicked(new PdfAfterLinkClickedEventArgs(webLink, pdfLink));

		}

		private void ProcessDestination(PdfDestination pdfDestination)
		{
			ScrollToPage(pdfDestination.PageIndex);
			Invalidate();
		}

		private void ProcessAction(PdfAction pdfAction)
		{
			if (pdfAction.ActionType == ActionTypes.Uri)
				Process.Start(pdfAction.ActionUrl);
			else if (pdfAction.Destination != null)
				ProcessDestination(pdfAction.Destination);
		}

		private int CalcCurrentPage()
		{
			int idx = -1;
			int maxArea = 0;
			for (int i = _startPage; i <= _endPage; i++)
			{
				var page = Document.Pages[i];

				var rect = RFTR(renderRects(i));
				rect.X += AutoScrollPosition.X;
				rect.Y += AutoScrollPosition.Y;
				if (!rect.IntersectsWith(ClientRectangle))
					continue;

				rect.Intersect(ClientRectangle);

				int area = rect.Width * rect.Height;
				if (maxArea < area)
				{
					maxArea = area;
					idx = i;
				}
			}
			return idx;
		}

		private void DrawCurrentPageHighlight(Graphics graphics, int pageIndex, Rectangle actualPageRect)
		{
			if (ShowCurrentPageHighlight && pageIndex == Document.Pages.CurrentIndex)
			{
				actualPageRect.Inflate(0, 0);
				var sm = graphics.SmoothingMode;
				graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				graphics.DrawRectangle(_currentPageHighlightColorPen, actualPageRect);
				graphics.SmoothingMode = sm;
			}
		}

		private void DrawPageSeparators(Graphics graphics, ref List<Point> separator)
		{
			for (int sep = 0; sep < separator.Count; sep += 2)
				graphics.DrawLine(_pageSeparatorColorPen, separator[sep], separator[sep + 1]);
		}

		private void CalcPageSeparator(Rectangle actualRect, int pageIndex, ref List<Point> separator)
		{
			if (!ShowPageSeparator || pageIndex == _endPage || ViewMode == ViewModes.SinglePage)
				return;
			switch (ViewMode)
			{
				case ViewModes.Vertical:
					separator.Add(new Point(actualRect.X, actualRect.Bottom + PageMargin.Bottom));
					separator.Add(new Point(actualRect.Right, actualRect.Bottom + PageMargin.Bottom));
					break;
				case ViewModes.Horizontal:
					separator.Add(new Point(actualRect.Right + PageMargin.Right, actualRect.Top));
					separator.Add(new Point(actualRect.Right + PageMargin.Right, actualRect.Bottom));
					break;
				case ViewModes.TilesVertical:
					if ((pageIndex+1) % TilesCount != 0)
					{
						//vertical
						separator.Add(new Point(actualRect.Right + PageMargin.Right, actualRect.Top));
						separator.Add(new Point(actualRect.Right + PageMargin.Right, actualRect.Bottom));
					}
					if (pageIndex <= _endPage - TilesCount)
					{
						//horizontal
						separator.Add(new Point(actualRect.X, actualRect.Bottom + PageMargin.Bottom));
						separator.Add(new Point(actualRect.Right, actualRect.Bottom + PageMargin.Bottom));
					}
					break;
			}
		}

		private void DrawTextSelection(Graphics graphics, SelectInfo selTmp, int pageIndex)
		{
			if (selTmp.StartPage < 0)
				return;
			if (pageIndex >= selTmp.StartPage && pageIndex <= selTmp.EndPage)
			{
				int s = 0;
				if (pageIndex == selTmp.StartPage)
					s = selTmp.StartIndex;

				int len = Document.Pages[pageIndex].Text.CountChars;
				if (pageIndex == selTmp.EndPage)
					len = (selTmp.EndIndex+1) - s;

				var ti = Document.Pages[pageIndex].Text.GetTextInfo(s, len);
				foreach (var rc in ti.Rects)
				{
					var pt1 = PageToDevice(rc.left, rc.top, pageIndex);
					var pt2 = PageToDevice(rc.right, rc.bottom, pageIndex);

                    int x = pt1.X < pt2.X ? pt1.X : pt2.X;
                    int y = pt1.Y < pt2.Y ? pt1.Y : pt2.Y;
                    int w = pt1.X > pt2.X ? pt1.X - pt2.X : pt2.X - pt1.X;
                    int h = pt1.Y > pt2.Y ? pt1.Y - pt2.Y : pt2.Y - pt1.Y;

                    graphics.FillRectangle(_selectColorBrush, new Rectangle(x, y, w, h));
				}
			}
		}

		private void DrawFillFormsSelection(Graphics graphics)
		{
			foreach (var selectRc in _selectedRectangles)
				graphics.FillRectangle(_selectColorBrush, selectRc);
		}

		private void DrawPage(Graphics graphics, PdfPage page, Rectangle actualRect)
		{
			if (actualRect.Width <= 0 || actualRect.Height <= 0)
				return;
			using (PdfBitmap bmp = new PdfBitmap(actualRect.Width, actualRect.Height, true))
			{
				//Draw background to bitmap
				bmp.FillRect(0, 0, actualRect.Width, actualRect.Height, PageBackColor);

				//Draw page content to bitmap
				page.Render(bmp, 0, 0, actualRect.Width, actualRect.Height, PageRotation(page), RenderFlags);

				//Draw fillforms to bitmap
				page.RenderForms(bmp, 0, 0, actualRect.Width, actualRect.Height, PageRotation(page), RenderFlags);

				//Draw bitmap to drawing surface
				graphics.DrawImageUnscaled(bmp.Image, actualRect.X, actualRect.Y);

				//Draw page border
				graphics.DrawRectangle(_pageBorderColorPen, actualRect);
			}
		}

		private Rectangle CalcActualRect(int index)
		{
			var rect = RFTR(renderRects(index));
			rect.X += AutoScrollPosition.X;
			rect.Y += AutoScrollPosition.Y;
			return rect;
		}

		private Rectangle RFTR(RectangleF rect)
		{
			return new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
		}

		private RectangleF GetRenderRect(int index)
		{
			SizeF size = GetRenderSize(index);
			PointF location = GetRenderLocation(size);
			return new RectangleF(location, size);
		}

		private PointF GetRenderLocation(SizeF size)
		{
			float xcenter = ((float)ClientSize.Width - size.Width) / 2;
			float ycenter = ((float)ClientSize.Height - size.Height) / 2;
			float xright = (float)ClientSize.Width - size.Width;
			float ybottom = (float)ClientSize.Height - size.Height;

			if (xcenter < 0)
				xcenter = 0;
			if (ycenter < 0)
				ycenter = 0;
			
			switch(PageAlign)
			{
				case ContentAlignment.TopLeft: return new PointF(0, 0);
				case ContentAlignment.TopCenter: return new PointF(xcenter, 0);
				case ContentAlignment.TopRight: return new PointF(xright, 0);

				case ContentAlignment.MiddleLeft: return new PointF(0, ycenter);
				case ContentAlignment.MiddleCenter: return new PointF(xcenter, ycenter);
				case ContentAlignment.MiddleRight: return new PointF(xright, ycenter);
				
				case ContentAlignment.BottomLeft: return new PointF(0, ybottom);
				case ContentAlignment.BottomCenter: return new PointF(xcenter, ybottom);
				case ContentAlignment.BottomRight: return new PointF(xright, ybottom);
				
				default: return new PointF(xcenter, ycenter); 
			}
		}

		private SizeF GetRenderSize(int index)
		{
			double w, h;
			Pdfium.FPDF_GetPageSizeByIndex(Document.Handle, index, out w, out h);

			double nw = ClientSize.Width;
			double nh = h * nw / w;

			switch (SizeMode)
			{
				case SizeModes.FitToHeight:
					nh = ClientSize.Height;
					nw = w * nh / h;
					break;
				case SizeModes.FitToSize:
					nh = ClientSize.Height;
					nw = w * nh / h;
					if (nw > ClientSize.Width)
					{
						nw = ClientSize.Width;
						nh = h * nw / w;
					}
					break;
				case SizeModes.Zoom:
					nw = w * Zoom;
					nh = h * Zoom;
					break;
			}
			return new SizeF((float)nw, (float)nh);
		}


		private int DeviceToPage(int x, int y, out PointF pagePoint)
		{
			for (int i = _startPage; i <= _endPage; i++)
			{
				var rect = RFTR(_renderRects[i]);
				rect.X += AutoScrollPosition.X;
				rect.Y += AutoScrollPosition.Y;
				if (!rect.Contains(x, y))
					continue;

				pagePoint = Document.Pages[i].DeviceToPage(
					rect.X, rect.Y,
					rect.Width, rect.Height,
					PageRotation(Document.Pages[i]), x, y);

				return i;
			}
			pagePoint = new PointF(0, 0);
			return -1;

		}

		private Point PageToDevice(float x, float y, int pageIndex)
		{
			var rect = RFTR(renderRects(pageIndex));
			rect.X += AutoScrollPosition.X;
			rect.Y += AutoScrollPosition.Y;

			return Document.Pages[pageIndex].PageToDevice(
					rect.X, rect.Y,
					rect.Width, rect.Height, 
					PageRotation(Document.Pages[pageIndex]), 
                    x, y);
		}

		private PageRotate PageRotation(PdfPage pdfPage)
		{
			int rot = pdfPage.Rotation - pdfPage.OriginalRotation;
			if (rot < 0)
				rot = 4 + rot;
			return  (PageRotate)rot;
		}

		private SelectInfo NormalizeSelectionInfo()
		{
			var selTmp = _selectInfo;
			if (selTmp.StartPage >= 0 && selTmp.EndPage >= 0)
			{
				if (selTmp.StartPage > selTmp.EndPage)
				{
					selTmp = new SelectInfo()
					{
						StartPage = selTmp.EndPage,
						EndPage = selTmp.StartPage,
						StartIndex = selTmp.EndIndex,
						EndIndex = selTmp.StartIndex
					};
				}
				else if ((selTmp.StartPage == selTmp.EndPage) && (selTmp.StartIndex > selTmp.EndIndex))
				{
					selTmp = new SelectInfo()
					{
						StartPage = selTmp.StartPage,
						EndPage = selTmp.EndPage,
						StartIndex = selTmp.EndIndex,
						EndIndex = selTmp.StartIndex
					};
				}
			}
			return selTmp;
		}

		private SizeF CalcVertical()
		{
			_renderRects = new RectangleF[Document.Pages.Count];
			float y = 0;
			float width = 0;
			for (int i = 0; i < _renderRects.Length; i++)
			{
				var page = Document.Pages[i];
				var rrect = GetRenderRect(i);
				_renderRects[i] = new RectangleF(
					rrect.X + PageMargin.Left,
					y + PageMargin.Top,
					rrect.Width - PageMargin.Left - PageMargin.Right,
					rrect.Height - PageMargin.Top - PageMargin.Bottom);
				y += rrect.Height;
				if (width < rrect.Width)
					width = rrect.Width;
			}
			return new SizeF(width, y);
		}

        private SizeF CalcTilesVertical()
        {
            _renderRects = new RectangleF[Document.Pages.Count];
            float maxX = 0;
            float maxY = 0;
            for (int i = 0; i < _renderRects.Length; i += TilesCount)
            {
                float x = 0;
                float y = maxY;
                for (int j = i; j < i + TilesCount; j++)
                {
                    if (j >= _renderRects.Length)
                        break;
                    var page = Document.Pages[j];
                    var rrect = GetRenderRect(j);
                    rrect.Width = rrect.Width / TilesCount;
                    rrect.Height = rrect.Height / TilesCount;

                    _renderRects[j] = new RectangleF(
                        x + PageMargin.Left+(j==i?rrect.X:0),
                        y + PageMargin.Top,
                        rrect.Width - PageMargin.Left - PageMargin.Right,
                        rrect.Height - PageMargin.Top - PageMargin.Bottom);
                    x += rrect.Width + (j == i ? rrect.X : 0);

                    if (maxY < _renderRects[j].Y + _renderRects[j].Height + PageMargin.Bottom)
                        maxY = _renderRects[j].Y + _renderRects[j].Height + PageMargin.Bottom;
                    if (maxX < _renderRects[j].X + _renderRects[j].Width + PageMargin.Right)
                        maxX = _renderRects[j].X + _renderRects[j].Width + PageMargin.Right;
                }
            }
            return new SizeF(maxX, maxY);
        }

        private SizeF CalcTilesVerticalNoChangeSize()
        {
            _renderRects = new RectangleF[Document.Pages.Count];
            float maxX = 0;
            float maxY = 0;
            for (int i = 0; i < _renderRects.Length; i += TilesCount)
            {
                float x = 0;
                float y = maxY;
                for (int j = i; j < i + TilesCount; j++)
                {
                    if (j >= _renderRects.Length)
                        break;
                    var page = Document.Pages[j];
                    var rrect = GetRenderRect(j);
 
                    _renderRects[j] = new RectangleF(
                        x + PageMargin.Left,
                        y + PageMargin.Top,
                        rrect.Width - PageMargin.Left - PageMargin.Right,
                        rrect.Height - PageMargin.Top - PageMargin.Bottom);
                    x += rrect.Width;

                    if (maxY < _renderRects[j].Y + _renderRects[j].Height+PageMargin.Bottom)
                        maxY = _renderRects[j].Y + _renderRects[j].Height + PageMargin.Bottom;
                    if (maxX < _renderRects[j].X + _renderRects[j].Width + PageMargin.Right)
                        maxX = _renderRects[j].X + _renderRects[j].Width + PageMargin.Right;
                }
            }
            return new SizeF(maxX, maxY);
        }

        private SizeF CalcHorizontal()
		{
			_renderRects = new RectangleF[Document.Pages.Count];
			float height = 0;
			float x = 0;
			for (int i = 0; i < _renderRects.Length; i++)
			{
				var page = Document.Pages[i];
				var rrect = GetRenderRect(i);
				_renderRects[i] = new RectangleF(
					x + PageMargin.Left,
					rrect.Y+PageMargin.Top,
					rrect.Width - PageMargin.Left - PageMargin.Right,
					rrect.Height - PageMargin.Top - PageMargin.Bottom);
				x += rrect.Width;
				if (height < rrect.Height)
					height = rrect.Height;
			}
			return new SizeF(x, height);
		}

		private SizeF CalcSingle()
		{
			_renderRects = new RectangleF[Document.Pages.Count];
			SizeF ret = new SizeF(0, 0);
			for (int i = 0; i < _renderRects.Length; i++)
			{
				var page = Document.Pages[i];
				var rrect = GetRenderRect(i);
				_renderRects[i] = new RectangleF(
					rrect.X + PageMargin.Left,
					rrect.Y + PageMargin.Top,
					rrect.Width - PageMargin.Left - PageMargin.Right,
					rrect.Height - PageMargin.Top - PageMargin.Bottom);
				if (i == Document.Pages.CurrentIndex)
					ret = new SizeF(rrect.Width, rrect.Height);
			}
			return ret;
		}

		private RectangleF renderRects(int index)
		{
			return _renderRects[index];
		}

		private void SetCurrentPage(int index)
		{
			try
			{
				Document.Pages.CurrentPageChanged -= Pages_CurrentPageChanged;
				Document.Pages.CurrentIndex = index;
				OnCurrentPageChanged(EventArgs.Empty);
			}
			finally
			{
				Document.Pages.CurrentPageChanged += Pages_CurrentPageChanged;
			}
		}

		private bool CalcIntersectEntries(HighlightInfo existEntry, HighlightInfo addingEntry, out List<HighlightInfo> calcEntries)
		{
			calcEntries = new List<HighlightInfo>();
			int eStart = existEntry.CharIndex;
			int eEnd = existEntry.CharIndex + existEntry.CharsCount - 1;
			int aStart = addingEntry.CharIndex;
			int aEnd = addingEntry.CharIndex + addingEntry.CharsCount - 1;

			if (eStart < aStart && eEnd >= aStart && eEnd <= aEnd)
			{
				calcEntries.Add(new HighlightInfo()
				{
					CharIndex = eStart,
					CharsCount = aStart - eStart,
					Color = existEntry.Color
				});
				return true;
			}
			else if (eStart >= aStart && eStart <= aEnd && eEnd > aEnd)
			{
				calcEntries.Add(new HighlightInfo()
				{
					CharIndex = aEnd + 1,
					CharsCount = eEnd - aEnd,
					Color = existEntry.Color
				});
				return true;
			}
			else if (eStart >= aStart && eEnd <= aEnd)
				return true;
			else if (eStart < aStart && eEnd > aEnd)
			{
				calcEntries.Add(new HighlightInfo()
				{
					CharIndex = eStart,
					CharsCount = aStart - eStart,
					Color = existEntry.Color
				});
				calcEntries.Add(new HighlightInfo()
				{
					CharIndex = aEnd + 1,
					CharsCount = eEnd - aEnd,
					Color = existEntry.Color
				});
				return true;
			}
			//no intersection
			return false;
		}

		private void SetupControls()
		{
			if (BookmarksViewer != null)
				BookmarksViewer.Document = _document;

			if (NamedDestinationsViewer != null)
				NamedDestinationsViewer.Document = _document;
		}

		private void ReleaseControls()
		{
			if (BookmarksViewer != null)
				BookmarksViewer.Document = null;

			if (NamedDestinationsViewer != null)
				NamedDestinationsViewer.Document = null;
		}

		private bool GetWord(PdfText text, int ci, out int si, out int ei)
		{
			si = ei = ci;
			if (text == null)
				return false;

			if (ci < 0)
				return false;
			
			for(int i= ci-1; i>=0; i--)
			{
				var c = text.GetCharacter(i);

				if (
					char.IsSeparator(c) || char.IsPunctuation(c) || char.IsControl(c) || 
					char.IsWhiteSpace(c) || c == '\r' || c == '\n'
					)
					break;
				si = i;
			}

			int last = text.CountChars;
            for (int i = ci + 1; i < last; i++ )
			{
				var c = text.GetCharacter(i);

				if (
					char.IsSeparator(c) || char.IsPunctuation(c) || char.IsControl(c) ||
					char.IsWhiteSpace(c) || c == '\r' || c == '\n'
					)
					break;
				ei = i;
			}
			return true;
		}
		#endregion

		#region Private fields, event handlers, methods, etc for bookmarks control
		private BookmarksViewer _bookmarksViewer;

		private void SubscribeBookmarksEvent()
		{
			if (_bookmarksViewer != null)
				_bookmarksViewer.AfterSelect += _bookmarksViewer_AfterSelect;
		}

		private void UnsubscribeBookmarksEvent()
		{
			if (_bookmarksViewer != null)
				_bookmarksViewer.AfterSelect -= _bookmarksViewer_AfterSelect;
		}

		private void _bookmarksViewer_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (NamedDestinationsViewer != null)
				NamedDestinationsViewer.Document = _document;

			var node = e.Node as BookmarksViewerNode;
			if (node == null || node.Bookmark == null)
				return;

			if (node.Bookmark.Action != null)
				ProcessAction(node.Bookmark.Action);
			else if (node.Bookmark.Destination != null)
				ProcessDestination(node.Bookmark.Destination);
		}
		#endregion

		#region Private fields, event handlers, methods, etc for named dest control
		private NamedDestinationsViewer _namedDestinationsViewer;
	
		private void SubscribeNamedDestinationsEvent()
		{
			if (_namedDestinationsViewer != null)
			{
				_namedDestinationsViewer.MouseDoubleClick += _namedDestinationsViewer_MouseDoubleClick;
				_namedDestinationsViewer.KeyDown += _namedDestinationsViewer_KeyDown;
			}
		}

		private void UnsubscribeNamedDestinationsEvent()
		{
			if (_namedDestinationsViewer != null)
			{
				_namedDestinationsViewer.MouseDoubleClick += _namedDestinationsViewer_MouseDoubleClick;
				_namedDestinationsViewer.KeyDown += _namedDestinationsViewer_KeyDown;
			}
		}

		private void _namedDestinationsViewer_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
				ProcessNamedDestinationsClick();
		}

		private void _namedDestinationsViewer_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			ProcessNamedDestinationsClick();
		}


		private void ProcessNamedDestinationsClick()
		{
			foreach (int index in _namedDestinationsViewer.SelectedIndices)
			{
				var item = _namedDestinationsViewer.Items[index] as NamedDestinationsViewerItem;
				if (item == null)
					continue;

				if (item.Destination != null)
					ProcessDestination(item.Destination);
			}
		}
		#endregion

		#region FillForms event handlers
		void _forms_Invalidate(object sender, EventArguments.InvalidatePageEventArgs e)
		{
			Invalidate();
		}

		void _forms_GotoPage(object sender, EventArguments.EventArgs<int> e)
		{
			if (Document == null)
				return;
			SetCurrentPage(e.Value);
			ScrollToPage(e.Value);
		}

		void _forms_DoURIAction(object sender, EventArguments.EventArgs<string> e)
		{
			Process.Start(e.Value);
		}

		void _forms_DoNamedAction(object sender, EventArguments.EventArgs<string> e)
		{
			if (Document == null)
				return;
			var dest = Document.NamedDestinations.GetByName(e.Value);
			if (dest != null)
			{
				SetCurrentPage(dest.PageIndex);
				ScrollToPage(dest.PageIndex);
			}
		}

		void _forms_DoGotoAction(object sender, EventArguments.DoGotoActionEventArgs e)
		{
			if (Document == null)
				_onstartPageIndex = e.PageIndex;
			else
			{
				SetCurrentPage(e.PageIndex);
				ScrollToPage(e.PageIndex);
			}
		}

		void _forms_BrowseFile(object sender, EventArguments.BrowseFileEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Multiselect = false;
			if (ofd.ShowDialog(this) == DialogResult.OK)
				e.FilePath = ofd.FileName;
		}

		void _forms_AppBeep(object sender, EventArguments.EventArgs<BeepTypes> e)
		{
			switch (e.Value)
			{
				case BeepTypes.Default: System.Media.SystemSounds.Beep.Play(); break;
				case BeepTypes.Error: System.Media.SystemSounds.Asterisk.Play(); break;
				case BeepTypes.Question: System.Media.SystemSounds.Question.Play(); break;
				case BeepTypes.Warning: System.Media.SystemSounds.Exclamation.Play(); break;
				case BeepTypes.Status: System.Media.SystemSounds.Beep.Play(); break;
				default: System.Media.SystemSounds.Beep.Play(); break;
			}

		}

		void _forms_AppAlert(object sender, EventArguments.AppAlertEventEventArgs e)
		{
			MessageBoxButtons bt = MessageBoxButtons.OK;
			MessageBoxIcon mbi = MessageBoxIcon.None;
			switch (e.ButtonType)
			{
				case ButtonTypes.OkCancel: bt = MessageBoxButtons.OKCancel; break;
				case ButtonTypes.YesNo: bt = MessageBoxButtons.YesNo; break;
				case ButtonTypes.YesNoCancel: bt = MessageBoxButtons.YesNoCancel; break;
			}
			switch (e.IconType)
			{
				case IconTypes.Error: mbi = MessageBoxIcon.Error; break;
				case IconTypes.Question: mbi = MessageBoxIcon.Question; break;
				case IconTypes.Status: mbi = MessageBoxIcon.Information; break;
				case IconTypes.Warning: mbi = MessageBoxIcon.Warning; break;
			}
			var ret = MessageBox.Show(this, e.Text, e.Title, bt, mbi);
			switch (ret)
			{
				case DialogResult.OK: e.DialogResult = DialogResults.Ok; break;
				case DialogResult.Yes: e.DialogResult = DialogResults.Yes; break;
				case DialogResult.No: e.DialogResult = DialogResults.No; break;
				case DialogResult.Cancel: e.DialogResult = DialogResults.Cancel; break;
			}
		}

		void Forms_GetDocumentPath(object sender, EventArgs<string> e)
		{
		}

		void _forms_AppResponse(object sender, EventArguments.AppResponseEventArgs e)
		{
		}

		void _forms_OutputSelectedRect(object sender, EventArguments.InvalidatePageEventArgs e)
		{
			if (Document == null)
				return;
			var idx = Document.Pages.GetPageIndex(e.Page);
			var pt1 = PageToDevice(e.Rect.left, e.Rect.top, idx);
			var pt2 = PageToDevice(e.Rect.right, e.Rect.bottom, idx);
			_selectedRectangles.Add(new Rectangle(pt1.X, pt1.Y, pt2.X - pt1.X, pt2.Y - pt1.Y));
			Invalidate();
		}

		void Forms_SetCursor(object sender, SetCursorEventArgs e)
		{
			switch (e.Cursor)
			{
				case CursorTypes.Hand: Cursor = Cursors.Hand; break;
				case CursorTypes.HBeam: Cursor = Cursors.IBeam; break;
				case CursorTypes.VBeam: Cursor = Cursors.IBeam; break;
				case CursorTypes.NESW: Cursor = Cursors.SizeNESW; break;
				case CursorTypes.NWSE: Cursor = Cursors.SizeNWSE; break;
				default: Cursor = DefaultCursor; break;
			}
		}
		#endregion

		#region Miscellaneous event handlers
		void Pages_CurrentPageChanged(object sender, EventArgs e)
		{
			OnCurrentPageChanged(EventArgs.Empty);
			Invalidate();
		}

		void Pages_PageInserted(object sender, PageCollectionChangedEventArgs e)
		{
			UpdateLayout();
		}

		void Pages_PageDeleted(object sender, PageCollectionChangedEventArgs e)
		{
			UpdateLayout();

		}

		#endregion


	}
}
