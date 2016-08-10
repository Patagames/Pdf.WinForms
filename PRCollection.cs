using System;
using System.Collections.Generic;
using System.Drawing;
using Patagames.Pdf.Enums;

namespace Patagames.Pdf.Net.Controls.WinForms
{
	internal class PRCollection : Dictionary<PdfPage, PRItem>
	{
		private PdfBitmap _canvasBitmap = null;
		public int _waitTime;
		public long _prevTicks;

		public void InitCanvas(Size size)
		{
			if (_canvasBitmap == null)
				_canvasBitmap = new PdfBitmap(size.Width, size.Height, true);
			_waitTime = 70;
			_prevTicks = DateTime.Now.Ticks;
		}

		public void ReleaseCanvas()
		{
			if (_canvasBitmap != null)
				_canvasBitmap.Dispose();
			_canvasBitmap = null;
			foreach (var i in this)
				ReleasePage(i.Key);
			this.Clear();
		}

		public PdfBitmap CanvasBitmap { get { return _canvasBitmap; } }

		/// <summary>
		/// Checks whether all visible pages are rendered
		/// </summary>
		/// <value>True if they still need to be painted, False otherwise.</value>
		public bool IsNeedContinuePaint
		{
			get
			{
				foreach (var item in this)
					if (item.Value.status == ProgressiveRenderingStatuses.RenderTobeContinued
						|| item.Value.status == ProgressiveRenderingStatuses.RenderReader)
						return true;
				return false;
			}
		}

		/// <summary>
		/// Checks whether rendering should be paused and control should be returned to UI thread.
		/// </summary>
		/// <returns>True means that application shoul continue painting.</returns>
		internal bool IsNeedPause(PdfPage page)
		{
			if (!this.ContainsKey(page))
				return false;

			var currentTicks = DateTime.Now.Ticks;
			var ms = TimeSpan.FromTicks(currentTicks - _prevTicks).Milliseconds;
			var ret = ms > _waitTime;
			return ret;
		}

		/// <summary>
		/// Start/Continue progressive rendering for specified page
		/// </summary>
		/// <param name="page">Pdf page object</param>
		/// <param name="pageRect">Actual page's rectangle. </param>
		/// <param name="pageRotate">Page orientation: 0 (normal), 1 (rotated 90 degrees clockwise), 2 (rotated 180 degrees), 3 (rotated 90 degrees counter-clockwise).</param>
		/// <param name="renderFlags">0 for normal display, or combination of flags defined above.</param>
		/// <param name="useProgressiveRender">True for use progressive render</param>
		/// <returns>Null if page still painting, PdfBitmap object if page successfully rendered.</returns>
		internal bool RenderPage(PdfPage page, Rectangle pageRect, PageRotate pageRotate, RenderFlags renderFlags, bool useProgressiveRender)
		{
			if (!this.ContainsKey(page))
				ProcessNew(page); //Add new page into collection

			if (!useProgressiveRender)
				this[page].status = ProgressiveRenderingStatuses.RenderDone + 3;
			return ProcessExisting(page, pageRect, pageRotate, renderFlags);
		}

		/// <summary>
		/// Process existing pages
		/// </summary>
		/// <returns>Null if page still painting, PdfBitmap object if page successfully rendered.</returns>
		private bool ProcessExisting(PdfPage page, Rectangle pageRect, PageRotate pageRotate, RenderFlags renderFlags)
		{
			switch (this[page].status)
			{
				case ProgressiveRenderingStatuses.RenderReader:
					this[page].status = page.StartProgressiveRender(CanvasBitmap, pageRect.X,pageRect.Y, pageRect.Width, pageRect.Height, pageRotate, renderFlags, null);
					if (this[page].status == ProgressiveRenderingStatuses.RenderDone)
						return true;
					return false; //Start rendering. Return nothing.

				case ProgressiveRenderingStatuses.RenderDone:
					page.CancelProgressiveRender();
					this[page].status = ProgressiveRenderingStatuses.RenderDone + 2;
					return true; //Stop rendering. Return image.

				case ProgressiveRenderingStatuses.RenderDone + 2:
					return true; //Rendering already dtoped. return image

				case ProgressiveRenderingStatuses.RenderDone + 3:
					this[page].status = ProgressiveRenderingStatuses.RenderDone + 2;
					page.RenderEx(CanvasBitmap, pageRect.X, pageRect.Y, pageRect.Width, pageRect.Height, pageRotate, renderFlags);
					return true; //Rendering in non progressive mode

				case ProgressiveRenderingStatuses.RenderTobeContinued:
					this[page].status = page.ContinueProgressiveRender();
					return false; //Continue rendering. Return nothing.

				case ProgressiveRenderingStatuses.RenderFailed:
				default:
					CanvasBitmap.FillRect(pageRect.X, pageRect.Y, pageRect.Width, pageRect.Height, Color.Red);
					CanvasBitmap.FillRect(pageRect.X + 5, pageRect.Y + 5, pageRect.Width - 10, pageRect.Height - 10, Color.White);
					page.CancelProgressiveRender();
					this[page].status = ProgressiveRenderingStatuses.RenderDone + 2;
					return true; //Error occur. Stop rendering. return image with error
			}
		}

		/// <summary>
		/// Adds page into collection
		/// </summary>
		private void ProcessNew(PdfPage page)
		{
			var item = new PRItem()
			{
				status = ProgressiveRenderingStatuses.RenderReader,
			};
			this.Add(page, item);
			page.Disposed += Page_Disposed;
		}

		/// <summary>
		/// Delete page from colection
		/// </summary>
		private void PageRemove(PdfPage page)
		{
			if (this.ContainsKey(page))
			{
				ReleasePage(page);
				this.Remove(page);
			}
		}

		/// <summary>
		/// Stops progressive rendering if need and unsubscribe events
		/// </summary>
		private void ReleasePage(PdfPage page)
		{
			if (this[page].status == ProgressiveRenderingStatuses.RenderTobeContinued)
				page.CancelProgressiveRender();
			page.Disposed -= Page_Disposed;
		}

		/// <summary>
		/// Occurs then the page is disposed
		/// </summary>
		private void Page_Disposed(object sender, EventArgs e)
		{
			PageRemove(sender as PdfPage);
		}

	}
}
