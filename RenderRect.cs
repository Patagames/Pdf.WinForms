namespace Patagames.Pdf.Net.Controls.WinForms
{
	internal struct RenderRect 
	{
		public bool IsChecked { get; set; }
		public float X { get; set; }
		public float Y { get; set; }
		public float Left { get; }
		public float Top { get; }
		public float Right { get; }
		public float Bottom { get; }
		public float Width { get; set; }
		public float Height { get; set; }

		public RenderRect(float x, float y, float width, float height, bool isChecked)
		{
			X = Left = x;
			Y = Top = y;
			Width = width;
			Height = height;
			Right = Left + Width;
			Bottom = Top + Height;
			IsChecked = isChecked;
		}
	}
}
