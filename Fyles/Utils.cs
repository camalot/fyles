using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fyles {
	/// <summary>
	/// Some utilities
	/// </summary>
	public static class Utils {
		/// <summary>
		/// Is current operative system XP or above?
		/// </summary>
		public static bool IsXpOrAbove ( ) {
			if ( Environment.OSVersion.Version.Major > 5 )
				return true;
			return ( Environment.OSVersion.Version.Major == 5 ) &&
						 ( Environment.OSVersion.Version.Minor >= 1 );
		}
		/// <summary>
		/// Is current operative system Vista or above?
		/// </summary>
		public static bool IsVistaOrAbove ( ) {
			if ( Environment.OSVersion.Version.Major > 6 )
				return true;
			return ( Environment.OSVersion.Version.Major == 6 ) &&
						 ( Environment.OSVersion.Version.Minor >= 0 );
		}
		/// <summary>
		/// Is current operative system Seven or above?
		/// </summary>
		public static bool IsSevenOrAbove ( ) {
			if ( Environment.OSVersion.Version.Major > 6 )
				return true;
			return ( Environment.OSVersion.Version.Major == 6 ) &&
						 ( Environment.OSVersion.Version.Minor >= 1 );
		}
	}
}
