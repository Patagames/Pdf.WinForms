namespace Patagames.Pdf.Net.Controls.WinForms
{
	/// <summary>
	/// Represents the information about the selected text in the control
	/// </summary>
	public struct SelectInfo
	{
		/// <summary>
		/// Zero-based index of a starting page.
		/// </summary>
		public int StartPage;
		
		/// <summary>
		/// Zero-based char index on a startPage.
		/// </summary>
		public int StartIndex;

		/// <summary>
		/// Zero-based index of a ending page.
		/// </summary>
		public int EndPage;

		/// <summary>
		/// Zero-based char index on a endPage.
		/// </summary>
		public int EndIndex;

        internal FS_POINTF PagePoint;
        internal int Before;
        internal int After;

		internal void Normalize()
		{
			if (StartPage >= 0 && EndPage >= 0)
            {
				if (StartPage > EndPage || (StartPage == EndPage && StartIndex > EndIndex))
				{
					Swap(ref StartPage, ref EndPage);
                    Swap(ref StartIndex, ref EndIndex);
				}
			}
        }

        private static void Swap(ref int a, ref int b)
		{
			int tmp = a;
			a = b;
			b = tmp;
		}

		internal static SelectInfo Empty => new SelectInfo { StartPage = -1, EndPage = -1, StartIndex = -1, EndIndex = -1 };

		internal bool IsAnySelected
        {
            get
            {
				//Normalize is not required
                if (StartPage < 0 || EndPage < 0)
                    return false;
                if ((EndIndex < 0 || StartIndex < 0) && StartPage == EndPage)
                    return false;
                return true;
            }
        }

		internal bool IsOnPageSelected(int pageIndex)
		{
            //Normalize is not required
            if (pageIndex < 0 || StartPage < 0 || EndPage < 0)
				return false;
			if (pageIndex == StartPage && StartIndex < 0)
				return false;
			if (pageIndex == EndPage && EndIndex < 0)
				return false;
			int s = StartPage;
			int e = EndPage;
			if (StartPage > EndPage)
			{ 
				s = e;
				e = StartPage; 
			}
			if (pageIndex < s || pageIndex > e) 
				return false;
			return true;
		}
    }
}
