using System.ComponentModel;
using System.Windows.Forms;

namespace Patagames.Pdf.Net.Controls.WinForms
{
	/// <summary>
	/// Represents the BookmarksViewer control for displaying bookmarks contained in PDF document.
	/// </summary>
	public partial class BookmarksViewer : TreeView
	{
		// <code language="c#" source="..\Pdfium\Examples\Examples.cs" region="BookmarksViewer class"></code>
		#region Private fields
		private PdfDocument _document;
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the <see cref="PdfDocument"/> associated with this control.
		/// </summary>
		public PdfDocument Document
		{
			get
			{
				return _document;
			}
			set
			{
				if (_document != value)
				{
					_document = value;
					Nodes.Clear();
					BuildTree(Nodes, Document.Bookmarks);
				}
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

		#region Private methods
		private void BuildTree(TreeNodeCollection nodes, PdfBookmarkCollections bookmarks = null)
		{
			foreach (var b in bookmarks)
			{
				var node = new BookmarksViewerNode(b);
				nodes.Add(node);
				if (b.Childs != null && b.Childs.Count > 0)
					BuildTree(node.Nodes, b.Childs);
			}
		}
		#endregion
	}
}
