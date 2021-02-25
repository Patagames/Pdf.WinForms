using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Patagames.Pdf.Net.Controls.WinForms
{
	/// <summary>
	/// Represents the BookmarksViewer control for displaying bookmarks contained in PDF document.
	/// </summary>
	public partial class BookmarksViewer : TreeView
	{

		#region Private fields
		private PdfViewer _pdfViewer = null;
		private Dictionary<IntPtr, int> _processed = new Dictionary<IntPtr, int>();
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets PdfViewer control associated with this BookmarkViewer control
		/// </summary>
		public PdfViewer PdfViewer
		{
			get
			{
				return _pdfViewer;
			}
			set
			{
				if (_pdfViewer != value)
					OnPdfViewerChanging(_pdfViewer, value);
			}
		}
		#endregion

		#region Constructors and initialization
		/// <summary>
		/// Initializes a new instance of the <see cref="BookmarksViewer"/> class. 
		/// </summary>
		public BookmarksViewer()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BookmarksViewer"/> class. 
		/// </summary>
		/// <param name="container">Container</param>
		public BookmarksViewer(IContainer container)
		{
			container.Add(this);

			InitializeComponent();
		}
		#endregion

		#region Overrides
		/// <summary>
		/// Raises the System.Windows.Forms.TreeView.AfterSelect event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.TreeViewEventArgs that contains the event data.</param>
		protected override void OnAfterSelect(TreeViewEventArgs e)
		{
			var node = e.Node as BookmarksViewerNode;
			if (node == null || node.Bookmark == null)
				return;

			if (node.Bookmark.Action != null)
				ProcessAction(node.Bookmark.Action);
			else if (node.Bookmark.Destination != null)
				ProcessDestination(node.Bookmark.Destination);

			base.OnAfterSelect(e);
		}
		#endregion

		#region Protected methods
		/// <summary>
		/// Called when the current PdfViewer control associated with the ToolStrip is changing.
		/// </summary>
		/// <param name="oldValue">PdfViewer control of which was associated with the ToolStrip.</param>
		/// <param name="newValue">PdfViewer control of which will be associated with the ToolStrip.</param>
		protected virtual void OnPdfViewerChanging(PdfViewer oldValue, PdfViewer newValue)
		{
			if (oldValue != null)
			{
				oldValue.AfterDocumentChanged -= pdfViewer_DocumentChanged;
				oldValue.DocumentClosed -= pdfViewer_DocumentClosed;
				oldValue.DocumentLoaded -= pdfViewer_DocumentLoaded;
			}
			if (newValue != null)
			{
				newValue.AfterDocumentChanged += pdfViewer_DocumentChanged;
				newValue.DocumentClosed += pdfViewer_DocumentClosed;
				newValue.DocumentLoaded += pdfViewer_DocumentLoaded;
			}

			_pdfViewer = newValue;
			RebuildTree();
		}

		/// <summary>
		/// Creates child nodes and raises the System.Windows.Forms.TreeView.BeforeExpand event.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
		{
			if (e.Node.Nodes.Count == 1 && e.Node.Nodes.ContainsKey("{C5C14465-60FB-448D-A3BD-8F5E855C081D}"))
			{
				e.Node.Nodes.Clear();
				BuildTree(e.Node.Nodes, (e.Node as BookmarksViewerNode).Bookmark.Childs);
			}
			base.OnBeforeExpand(e);
		}

        /// <summary>
        /// Process the <see cref="PdfAction"/>.
        /// </summary>
        /// <param name="pdfAction">PdfAction to be performed.</param>
        protected virtual void ProcessAction(PdfAction pdfAction)
		{
			if (_pdfViewer == null)
				return;
			_pdfViewer.ProcessAction(pdfAction);
		}


		/// <summary>
		/// Process the <see cref="PdfDestination"/>.
		/// </summary>
		/// <param name="pdfDestination">PdfDestination to be performed.</param>
		protected virtual void ProcessDestination(PdfDestination pdfDestination)
		{
			if (_pdfViewer == null)
				return;
			_pdfViewer.ProcessDestination(pdfDestination);
		}
		#endregion

		#region Private event handlers
		private void pdfViewer_DocumentChanged(object sender, EventArgs e)
		{
			RebuildTree();
		}

		private void pdfViewer_DocumentLoaded(object sender, EventArgs e)
		{
			RebuildTree();
		}

		private void pdfViewer_DocumentClosed(object sender, EventArgs e)
		{
			RebuildTree();
		}
		#endregion

		#region Private methods
		private void BuildTree(TreeNodeCollection nodes, PdfBookmarkCollections bookmarks)
		{
			if (bookmarks == null)
				return;

			foreach (var b in bookmarks)
			{
				if (_processed.ContainsKey(b.Handle))
					continue;
				_processed.Add(b.Handle, 1);
				var node = new BookmarksViewerNode(b);
				nodes.Add(node);
				if (b.Childs != null && b.Childs.Count > 0)
					node.Nodes.Add("{C5C14465-60FB-448D-A3BD-8F5E855C081D}", "Loading...");
			}
		}
		#endregion

		#region Public methods
		/// <summary>
		/// Constructs the tree of bookmarks
		/// </summary>
		public void RebuildTree()
		{
			Nodes.Clear();
			_processed.Clear();
			if (_pdfViewer != null && _pdfViewer.Document != null)
				BuildTree(Nodes, _pdfViewer.Document.Bookmarks);
		}
        #endregion
    }
}
