/*
  Copyright (C) 2005-2014 Govert van Drimmelen

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.


  Govert van Drimmelen
  govert@icon.co.za
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ExcelDna.Integration
{
    // CAUTION: The ExcelReference class is also called via reflection by the ExcelDna.Loader marshaler.
	public class ExcelReference
	{
	    class ExcelRectangle
        {
            public readonly int RowFirst;
            public readonly int RowLast;
            public readonly int ColumnFirst;
            public readonly int ColumnLast;

            internal ExcelRectangle(int rowFirst, int rowLast, int columnFirst, int columnLast)
            {
                // CONSIDER: Throw or truncate for errors
                RowFirst    = GetInRange(rowFirst, 0, ExcelDnaUtil.ExcelLimits.MaxRows - 1);
                RowLast     = GetInRange(rowLast, 0, ExcelDnaUtil.ExcelLimits.MaxRows - 1);
                ColumnFirst = GetInRange(columnFirst, 0, ExcelDnaUtil.ExcelLimits.MaxColumns - 1);
                ColumnLast  = GetInRange(columnLast, 0, ExcelDnaUtil.ExcelLimits.MaxColumns - 1);
                
                // CONSIDER: Swap or truncate rect ??
                //if (RowLast < RowFirst) RowLast = RowFirst;
                //if (ColumnLast < ColumnFirst) ColumnLast = RowFirst;
            }

            private int GetInRange(int value, int min, int max)
            {
                Debug.Assert(min <= max);
                if (value < min) return min;
                if (value > max) return max;
                return value;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof (ExcelRectangle)) return false;
                return Equals((ExcelRectangle) obj);
            }

            bool Equals(ExcelRectangle other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return other.RowFirst == RowFirst && other.RowLast == RowLast && other.ColumnFirst == ColumnFirst && other.ColumnLast == ColumnLast;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int result = RowFirst;
                    result = (result*397) ^ RowLast;
                    result = (result*397) ^ ColumnFirst;
                    result = (result*397) ^ ColumnLast;
                    return result;
                }
            }
        }

        // CONSIDER: Rather use a derived class so that we can implement Equals properly.
        //           The implementation as a List here is actually hidden.
	    readonly List<ExcelRectangle> rectangles = new List<ExcelRectangle>();
		readonly IntPtr sheetId;

		public ExcelReference(int row, int column)
			: this(row, row, column, column)
		{
		}

		// DOCUMENT: If no SheetId is given, assume the Active (Front) Sheet
		public ExcelReference(int rowFirst, int rowLast, int columnFirst, int columnLast) :
			this(rowFirst, rowLast, columnFirst, columnLast, IntPtr.Zero)
		{
			try
			{
				ExcelReference r = (ExcelReference)XlCall.Excel(XlCall.xlSheetId);
				sheetId = r.sheetId;
			}
			catch
			{
				// CONSIDER: throw or 'default' behaviour?
			}
		}

		public ExcelReference(int rowFirst, int rowLast, int columnFirst, int columnLast, IntPtr sheetId)
		{
			this.sheetId = sheetId;
			ExcelRectangle rect = new ExcelRectangle(rowFirst, rowLast, columnFirst, columnLast);
			rectangles.Add(rect);
		}

        // TODO: Consider how to deal with invalid sheetName. I presume xlSheetId will fail.
        // Perhaps throw a custom exception...?
        public ExcelReference(int rowFirst, int rowLast, int columnFirst, int columnLast, string sheetName)
        {
            ExcelReference sheetRef = (ExcelReference)XlCall.Excel(XlCall.xlSheetId, sheetName);
            this.sheetId = sheetRef.SheetId;
            ExcelRectangle rect = new ExcelRectangle(rowFirst, rowLast, columnFirst, columnLast);
            rectangles.Add(rect);
        }

		// THROWS: OverFlowException if the arguments exceed the allowed size
		// or if the number of Inner References exceeds 65000
		public void AddReference(int rowFirst, int rowLast, int columnFirst, int columnLast)
		{
			if (rectangles.Count < ushort.MaxValue)
				rectangles.Add(new ExcelRectangle(rowFirst, rowLast, columnFirst, columnLast));
			else 
				throw new OverflowException("Maximum number of references exceeded");
		}

		public int RowFirst
		{
			get { return rectangles[0].RowFirst; }
		}

		public int RowLast
		{
			get { return rectangles[0].RowLast; }
		}

		public int ColumnFirst
		{
			get { return rectangles[0].ColumnFirst; }
		}

		public int ColumnLast
		{
			get { return rectangles[0].ColumnLast; }
		}

		public IntPtr SheetId
		{
			get { return sheetId; }
		}

		public List<ExcelReference> InnerReferences
		{
			get 
			{
				List<ExcelReference> inner = new List<ExcelReference>();
				foreach (ExcelRectangle rect in rectangles)
				{
					inner.Add(new ExcelReference(rect.RowFirst, rect.RowLast, 
						rect.ColumnFirst, rect.ColumnLast, sheetId));
				}
				return inner;
			}
		}

		public object GetValue()
		{
			return XlCall.Excel(XlCall.xlCoerce, this);
		}

        // DOCUMENT: Strange behaviour with SetValue...
		public bool SetValue(object value)
		{
			return (bool)XlCall.Excel(XlCall.xlSet, this, value);
		}

        // CAUTION: These 'private' functions are called via reflection by the ExcelDna.Loader marshaler
        // Returns arrays containing all the inner rectangles (including the one we pretend is outside).
        private int[][] GetRectangles()
        {
            int[][] intRects = new int[rectangles.Count][];
            for (int i = 0; i < rectangles.Count; i++)
            {
                ExcelRectangle rect = rectangles[i];
                intRects[i] = new int[] {rect.RowFirst, rect.RowLast,
                    rect.ColumnFirst, rect.ColumnLast};
            }
            return intRects;
        }

        private int GetRectangleCount()
        {
            return rectangles.Count;
        }

        // Structural equality implementation
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (ExcelReference)) return false;
            return Equals((ExcelReference) obj);
        }

	    bool Equals(ExcelReference other)
	    {
	        if (ReferenceEquals(null, other)) return false;
	        if (ReferenceEquals(this, other)) return true;
            // Implement equality check based on contents.
            // CONSIDER: Implement in class derived from List.
            if (rectangles.Count != other.rectangles.Count) return false;
            for (int i = 0; i < rectangles.Count; i++)
            {
                if (!Equals(rectangles[i], other.rectangles[i])) return false; 
            }
	        return other.sheetId.Equals(sheetId);
	    }

        // We need to take some care with the Hash Code here, since we use the ExcelReference with structural comparison
        // in some Dictionaries.
	    public override int GetHashCode()
	    {
            // One of the ideas from http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
            const int b = 378551;
            int a = 63689;
            int hash = 0;
            
            unchecked
	        {
                for (int i = 0; i < rectangles.Count; i++)
                {
                    if (rectangles[i] != null)
                    {
                        hash = hash * a + rectangles[i].GetHashCode();
                        a = a * b;
                    }
                }
                hash *= 397;
            }
	        return hash ^ sheetId.GetHashCode();
	    }

        public override string ToString()
        {
            return string.Format("({0},{1} : {2},{3}) - {4}", RowFirst, ColumnFirst, RowLast, ColumnLast, SheetId);
        }
	}
}
