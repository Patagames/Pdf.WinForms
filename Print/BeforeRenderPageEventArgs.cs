using System;
using System.Drawing;
using Patagames.Pdf.Enums;

namespace Patagames.Pdf.Net.Controls.WinForms
{
	/// <summary>
	/// Represents the class that contain event data for the PrintPageLoaded event.
	/// </summary>
	public class BeforeRenderPageEventArgs : EventArgs
	{
		/// <summary>
		/// Gets drawing surface.
		/// </summary>
		public Graphics Graphics { get; private set; }

		/// <summary>
		/// The page what will be printed.
		/// </summary>
		public PdfPage Page { get; private set; }

		/// <summary>
		/// Horizontal position of the <see cref="Page"/> on the drawing surface.
		/// </summary>
		public int X { get; set; }

		/// <summary>
		/// Vertical position of the <see cref="Page"/> on the drawing surface.
		/// </summary>
		public int Y { get; set; }

		/// <summary>
		/// The page's width calculated to match the sheet size.
		/// </summary>
		public int Width { get; set; }

		/// <summary>
		/// The page's height calculated to match the sheet size.
		/// </summary>
		public int Height { get; set; }

		/// <summary>
		/// The page rotation.
		/// </summary>
		public PageRotate Rotation { get; private set; }


		/// <summary>
		/// Construct new instnace of the PrintPageLoadedEventArgs class
		/// </summary>
		/// <param name="g">Drawing surface.</param>
		/// <param name="page">The page what will be printed.</param>
		/// <param name="x">Horizontal position of the <paramref name="page"/> on the drawing surface.</param>
		/// <param name="y">Vertical position of the <paramref name="page"/> on the drawing surface.</param>
		/// <param name="width">The page's width calculated to match the sheet size.</param>
		/// <param name="height">The page's height calculated to match the sheet size.</param>
		/// <param name="rotation">The page rotation.</param>
		public BeforeRenderPageEventArgs(Graphics g, PdfPage page, int x, int y, int width, int height, PageRotate rotation)
		{
			Graphics = g;
			Page = page;
			X = x;
			Y = y;
			Width = width;
			Height = height;
			Rotation = rotation;
		}
	}
}