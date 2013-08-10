using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fyles.Extensions {
	public static partial class FylesExtensions {
		
		public static String With ( this String s, params object[] args ) {
			return String.Format ( s, args );
		}


		public static String PathSafe ( this String s ) {
			return Path.GetInvalidFileNameChars ( ).Aggregate ( s, ( current, c ) => current.Replace ( c, '_' ) );
		}

	}
}
