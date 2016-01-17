using Patagames.Pdf.Enums;
using System.Drawing;

namespace Patagames.Pdf.Net.Controls.WinForms
{
	internal class PRItem
	{
		public PdfBitmap bmp;
		public ProgressiveRenderingStatuses status;
		public int waitTime;
		public long prevTicks;
		public Rectangle actualRect;
	}
}
