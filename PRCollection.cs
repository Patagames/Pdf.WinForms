using System;
using System.Collections.Generic;
using System.Drawing;
using Patagames.Pdf.Enums;

namespace Patagames.Pdf.Net.Controls.WinForms
{
	internal class PRCollection : Dictionary<PdfPage, PRItem>
	{
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
			var ms = TimeSpan.FromTicks(currentTicks - this[page].prevTicks).Milliseconds;
			var ret = ms > this[page].waitTime;
			this[page].prevTicks = currentTicks;
			if (ret)
				this[page].waitTime += 10;
			return ret;
		}

		/// <summary>
		/// Start/Continue progressive rendering for specified page
		/// </summary>
		/// <param name="page">Pdf page object</param>
		/// <param name="width">Actual page's width. </param>
		/// <param name="height">Actual page's height. </param>
		/// <param name="pageRotate">Page orientation: 0 (normal), 1 (rotated 90 degrees clockwise), 2 (rotated 180 degrees), 3 (rotated 90 degrees counter-clockwise).</param>
		/// <param name="renderFlags">0 for normal display, or combination of flags defined above.</param>
		/// <returns>Null if page still painting, PdfBitmap object if page successfully rendered.</returns>
		internal PdfBitmap RenderPage(PdfPage page, int width, int height, PageRotate pageRotate, RenderFlags renderFlags)
		{
			if (this.ContainsKey(page))
			{
				if (this[page].width != width || this[page].height != height)
				{
					//Process page as new if its actual size was changed.
					PageRemove(page);
					ProcessNew(page, width, height);
					return null;
				}
				//Continue painting for this page
				return ProcessExisting(page, width, height, pageRotate, renderFlags);
			}
			else
				ProcessNew(page, width, height); //Add new page into collection

			return null;
		}

		/// <summary>
		/// Process existing pages
		/// </summary>
		/// <returns>Null if page still painting, PdfBitmap object if page successfully rendered.</returns>
		private PdfBitmap ProcessExisting(PdfPage page, int width, int height, PageRotate pageRotate, RenderFlags renderFlags)
		{
			switch (this[page].status)
			{
				case ProgressiveRenderingStatuses.RenderReader:
					this[page].status = page.StartProgressiveRender(this[page].bmp, 0, 0, width, height, pageRotate, renderFlags, null);
					return null; //Start rendering. Return nothing.

				case ProgressiveRenderingStatuses.RenderDone:
					page.CancelProgressiveRender();
					this[page].status = ProgressiveRenderingStatuses.RenderDone + 2;
					return this[page].bmp; //Stop rendering. Return image.

				case ProgressiveRenderingStatuses.RenderDone + 2:
					return this[page].bmp; //Rendering already dtoped. return image

				case ProgressiveRenderingStatuses.RenderTobeContinued:
					this[page].status = page.ContinueProgressiveRender();
					return null; //Continue rendering. Return nothing.

				case ProgressiveRenderingStatuses.RenderFailed:
				default:
					this[page].bmp.FillRect(0, 0, width, height, Color.Red);
					this[page].bmp.FillRect(5, 5, width - 10, height - 10, Color.White);
					page.CancelProgressiveRender();
					this[page].status = ProgressiveRenderingStatuses.RenderDone + 2;
					return this[page].bmp; //Error occur. Stop rendering. return image with error
			}
		}

		/// <summary>
		/// Adds page into collection
		/// </summary>
		private void ProcessNew(PdfPage page, int width, int height)
		{
			var item = new PRItem()
			{
				bmp = new PdfBitmap(width, height, true),
				status = ProgressiveRenderingStatuses.RenderReader,
				waitTime = 10,
				prevTicks = DateTime.Now.Ticks,
				width = width,
				height = height
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
				if (this[page].status == ProgressiveRenderingStatuses.RenderTobeContinued)
					page.CancelProgressiveRender();
				this[page].bmp.Dispose();
				page.Disposed -= Page_Disposed;
				this.Remove(page);
			}
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
