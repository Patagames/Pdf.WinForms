using System.Windows.Forms;

namespace Patagames.Pdf.Net.Controls.WinForms
{
	internal class BookmarksViewerNode : TreeNode
	{
		public PdfBookmark Bookmark { get; private set; }

		public BookmarksViewerNode(PdfBookmark bookmark)
			: base(bookmark.Title)
		{
			Bookmark = bookmark;
		}
	}

}
