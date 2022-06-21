using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBG
{
	public struct Pos
	{
		public int x;
		public int y;

		public Pos(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public override string ToString()
		{
			return string.Format("[{0},{1}]", x, y);
		}

		public bool IsEqual(Pos pos)
		{
			return x == pos.x && y == pos.y;
		}
	}
}
