namespace Patagames.Pdf.Net.Controls.WinForms
{
	/// <summary>
	/// Specifies how the PdfViewer will display pages
	/// </summary>
	public enum ViewModes
	{
		/// <summary>
		/// The vertical arrangement of pages
		/// </summary>
		Vertical,

		/// <summary>
		/// The horizontal arrangement of pages
		/// </summary>
		Horizontal,

		/// <summary>
		/// Vertical-oriented tiles
		/// </summary>
		TilesVertical,

		/// <summary>
		/// Vertical-oriented tiles. The pages are not resizes
		/// </summary>
		TilesVerticalNoResize,

		/// <summary>
		/// Displays the current page
		/// </summary>
		SinglePage
	}
}
