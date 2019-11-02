using Patagames.Pdf.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Patagames.Pdf.Net.Controls.WinForms.ToolBars
{
    /// <summary>
    /// Provides a container for Windows toolbar objects with predefined functionality for searching
    /// </summary>
    public class PdfToolStripSearch : PdfToolStrip
    {
        #region Private fields
        List<PdfSearch.FoundText> _foundText = new List<PdfSearch.FoundText>();
        List<PdfSearch.FoundText> _forHighlight = new List<PdfSearch.FoundText>();
        Timer _searchTimer;
        int _searchPageIndex;
        string _searchText;
        FindFlags _searchFlags;
        int _prevRecord;
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets delta values for each edge of the rectangles of the highlighted text.
        /// </summary>
        public FS_RECTF InflateHighlight { get; set; }

        /// <summary>
        /// Gets or sets the color of the found text.
        /// </summary>
        public Color HighlightColor { get; set; }

        /// <summary>
        /// Gets or sets the color of the found text.
        /// </summary>
        public Color ActiveRecordColor { get; set; }

        /// <summary>
        /// Gets or sets search text
        /// </summary>
        public string SearchText
        {
            get
            {
                return (this.Items["btnSearchBar"] as ToolStripSearchBar).SearchBar.SearchText;
            }
            set
            {
                (this.Items["btnSearchBar"] as ToolStripSearchBar).SearchBar.SearchText = value;
            }
        }

        /// <summary>
        /// Gets or sets search flags
        /// </summary>
        public FindFlags SearchFlags
        {
            get
            {
                var tssb = this.Items["btnSearchBar"] as ToolStripSearchBar;
                if (tssb == null)
                    return FindFlags.None;
                return tssb.SearchBar.FindFlags;
            }
            set
            {
                var tssb = this.Items["btnSearchBar"] as ToolStripSearchBar;
                if (tssb == null)
                    return;
                tssb.SearchBar.FindFlags = value;
            }
        }

        /// <summary>
        /// Gets or sets the current found record
        /// </summary>
        public int CurrentRecord
        {
            get
            {
                var tssb = this.Items["btnSearchBar"] as ToolStripSearchBar;
                if (tssb == null)
                    return 0;
                return tssb.SearchBar.CurrentRecord;
            }
            set
            {
                var tssb = this.Items["btnSearchBar"] as ToolStripSearchBar;
                if (tssb == null)
                    return;
                tssb.SearchBar.CurrentRecord = value;
            }
        }

        /// <summary>
        /// Gets the total number of found records
        /// </summary>
        public int TotalRecords
        {
            get
            {
                var tssb = this.Items["btnSearchBar"] as ToolStripSearchBar;
                if (tssb == null)
                    return 0;
                return tssb.SearchBar.TotalRecords;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initialize the new instance of PdfToolStripSearch class
        /// </summary>
        public PdfToolStripSearch()
        {
            _searchTimer = new Timer();
            _searchTimer.Interval = 1;
            _searchTimer.Tick += _searchTimer_Tick;
            HighlightColor = Color.FromArgb(50, 255, 255, 0);
            ActiveRecordColor = Color.FromArgb(255, 255, 255, 0);
            InflateHighlight = new FS_RECTF(2.0, 3.5, 2.0, 2.0);
        }
    #endregion

    #region Overriding
    /// <summary>
    /// Create all buttons and add its into toolbar. Override this method to create custom buttons
    /// </summary>
    protected override void InitializeButtons()
        {
            var btn = new ToolStripSearchBar();
            btn.Name = "btnSearchBar";
            btn.SearchBar.CurrentRecordChanged += SearchBar_CurrentRecordChanged;
            btn.SearchBar.NeedSearch += SearchBar_NeedSearch;
            this.Items.Add(btn);
        }

        /// <summary>
        /// Called when the ToolStrip's items need to change its states
        /// </summary>
        protected override void UpdateButtons()
        {
            var tsi = this.Items["btnSearchBar"] as ToolStripSearchBar;
            if (tsi != null)
            {
                tsi.Enabled = (PdfViewer != null) && (PdfViewer.Document != null);
                tsi.SearchBar.TotalRecords = 0;
                tsi.SearchBar.SearchText = "";
            }

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

            if (oldValue != null && oldValue.Document != null)
                PdfViewer_DocumentClosed(this, EventArgs.Empty);
            if (newValue != null && newValue.Document != null)
                PdfViewer_DocumentLoaded(this, EventArgs.Empty);
        }

        #endregion

        #region Event handlers for PdfViewer
        private void PdfViewer_DocumentClosed(object sender, EventArgs e)
        {
            UpdateButtons();
            StopSearch();
        }

        private void PdfViewer_DocumentLoaded(object sender, EventArgs e)
        {
            UpdateButtons();
        }
        #endregion

        #region Event handlers for buttons
        private void SearchBar_NeedSearch(object sender, EventArgs e)
        {
            OnNeedSearch(SearchFlags, SearchText);
        }

        private void SearchBar_CurrentRecordChanged(object sender, EventArgs e)
        {
            OnCurrentRecordChanged(CurrentRecord, TotalRecords);
        }

        #endregion

        #region Protected methods
        /// <summary>
        /// Called when current recordchanged
        /// </summary>
        /// <param name="currentRecord">The number of current record</param>
        /// <param name="totalRecords">The total number of records</param>
        protected virtual void OnCurrentRecordChanged(int currentRecord, int totalRecords)
        {
            ScrollToRecord(currentRecord);
            HighlightRecord(_prevRecord, currentRecord);
            _prevRecord = currentRecord;
        }

        /// <summary>
        /// Called when the search routine should be launched
        /// </summary>
        /// <param name="searchFlags">Search flags</param>
        /// <param name="searchText">Text for search</param>
        protected virtual void OnNeedSearch(FindFlags searchFlags, string searchText)
        {
            StartSearch(searchFlags, searchText);
        }
        #endregion

        #region Private methods
        private void UnsubscribePdfViewEvents(PdfViewer oldValue)
        {
            oldValue.AfterDocumentChanged -= PdfViewer_DocumentLoaded;
            oldValue.DocumentLoaded -= PdfViewer_DocumentLoaded;
            oldValue.DocumentClosed -= PdfViewer_DocumentClosed;
        }

        private void SubscribePdfViewEvents(PdfViewer newValue)
        {
            newValue.AfterDocumentChanged += PdfViewer_DocumentLoaded;
            newValue.DocumentLoaded += PdfViewer_DocumentLoaded;
            newValue.DocumentClosed += PdfViewer_DocumentClosed;
        }

        private void StartSearch(FindFlags searchFlags, string searchText)
        {
            StopSearch();
            if (searchText == "")
                return;
            _prevRecord = -1;
            _searchText = searchText;
            _searchFlags = searchFlags;
            _searchPageIndex = 0;
            _searchTimer.Start();
        }

        private void StopSearch()
        {
            _searchPageIndex = -1;
            _searchTimer.Stop();
            _foundText.Clear();
            _forHighlight.Clear();
            PdfViewer.RemoveHighlightFromText();

            var tssb = this.Items["btnSearchBar"] as ToolStripSearchBar;
            if (tssb == null)
                return;
            tssb.SearchBar.CurrentRecord = 0;

        }

        private void ScrollToRecord(int currentRecord)
        {
            if (currentRecord < 1 || currentRecord > _foundText.Count)
                return;
            var ft = _foundText[currentRecord - 1];

            PdfViewer.CurrentIndex = ft.PageIndex; 

            var rects = PdfViewer.GetHighlightedRects(ft.PageIndex, new HighlightInfo() { CharIndex = ft.CharIndex, CharsCount = ft.CharsCount, Inflate = InflateHighlight });
            if (rects.Count > 0 && !PdfViewer.ClientRectangle.Contains(rects[0]))
            {
                var p = PdfViewer.ClientToPage(ft.PageIndex, new Point(rects[0].Left, rects[0].Top));
                PdfViewer.ScrollToPoint(ft.PageIndex, p);
            }
        }

        private void HighlightRecord(int prevRecord, int currentRecord)
        {
            if (prevRecord >=1 && prevRecord <= _foundText.Count)
            {
                var ft = _foundText[prevRecord - 1];
                PdfViewer.HighlightText(ft.PageIndex, ft.CharIndex, ft.CharsCount, HighlightColor, InflateHighlight);
            }
            if (currentRecord >= 1 && currentRecord <= _foundText.Count)
            {
                var ft = _foundText[currentRecord - 1];
                PdfViewer.HighlightText(ft.PageIndex, ft.CharIndex, ft.CharsCount, ActiveRecordColor, InflateHighlight);
            }
        }

        private void UpdateResults()
        {
            var tssb = this.Items["btnSearchBar"] as ToolStripSearchBar;
            if (tssb == null)
                return;

            tssb.SearchBar.TotalRecords = _foundText.Count;
            foreach (var ft in _forHighlight)
            {
                var item = new HighlightInfo() { CharIndex = ft.CharIndex, CharsCount = ft.CharsCount, Color = HighlightColor, Inflate = InflateHighlight };
                if (!PdfViewer.HighlightedTextInfo.ContainsKey(ft.PageIndex))
                    PdfViewer.HighlightedTextInfo.Add(ft.PageIndex, new List<HighlightInfo>());
                 PdfViewer.HighlightedTextInfo[ft.PageIndex].Add(item);
            }
            _forHighlight.Clear();
        }

        private void _searchTimer_Tick(object sender, EventArgs e)
        {
            if (_searchPageIndex < 0)
                return;
            var doc = PdfViewer.Document;
            int cnt = doc.Pages.Count;
            if (_searchPageIndex >= cnt)
            {
                _searchTimer.Stop();
                return;
            }

            IntPtr page = Pdfium.FPDF_LoadPage(doc.Handle, _searchPageIndex);
            if (page == IntPtr.Zero)
            {
                _searchTimer.Stop();
                return;
            }

            IntPtr text = Pdfium.FPDFText_LoadPage(page);
            if (text == IntPtr.Zero)
            {
                _searchTimer.Stop();
                return;
            }

            var sh = Pdfium.FPDFText_FindStart(text, _searchText, _searchFlags, 0);
            if (sh == IntPtr.Zero)
            {
                _searchTimer.Stop();
                return;
            }

            while (Pdfium.FPDFText_FindNext(sh))
            {
                int idx = Pdfium.FPDFText_GetSchResultIndex(sh);
                int len = Pdfium.FPDFText_GetSchCount(sh);
                var ft = new PdfSearch.FoundText() { CharIndex = idx, CharsCount = len, PageIndex = _searchPageIndex };
                _foundText.Add(ft);
                _forHighlight.Add(ft);
            }
            Pdfium.FPDFText_FindClose(sh);
            Pdfium.FPDFText_ClosePage(text);
            Pdfium.FPDF_ClosePage(page);
            UpdateResults();
            _searchPageIndex++;
        }
        #endregion
    }

}
