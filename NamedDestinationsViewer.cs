using System.ComponentModel;
using System.Windows.Forms;

namespace Patagames.Pdf.Net.Controls.WinForms
{
	/// <summary>
	/// Represents the NamedDestinationsViewer control for displaying named destinations contained in PDF document
	/// </summary>
	public partial class NamedDestinationsViewer : ListView
	{
		// <code language="c#" source="..\Pdfium\Examples\Examples.cs" region="NamedDestinationsViewer class"></code>
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
					if (_document != null)
						BuildList(Document.NamedDestinations);
					else
						VirtualListSize = 0;
				}
			}
		}
		#endregion

		#region Constructors and initialization
		/// <summary>
		/// Initializes a new instance of the <see cref="NamedDestinationsViewer"/> class. 
		/// </summary>
		public NamedDestinationsViewer()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NamedDestinationsViewer"/> class. 
		/// </summary>
		/// <param name="container">Container</param>
		public NamedDestinationsViewer(IContainer container)
		{
			container.Add(this);

			InitializeComponent();
		}
		#endregion

		#region Private methods
		private void BuildList(PdfDestinationCollections destinations = null)
		{
			VirtualListSize = Document.NamedDestinations.Count;
			if (!VirtualMode)
			{
				BeginUpdate();
				Items.Clear();
				foreach (var b in destinations)
				{
					var item = new NamedDestinationsViewerItem(b);
					Items.Add(item);
				}
				EndUpdate();
			}
		}
		#endregion

		#region Event handlers
		private void NamedDestinationsViewer_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
		{
			e.Item = new NamedDestinationsViewerItem(Document.NamedDestinations[e.ItemIndex]);
		}
		#endregion
	}
}
