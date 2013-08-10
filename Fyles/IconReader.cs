using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Fyles.WinApi;
using Microsoft.Win32;

namespace Fyles {
	/// <summary>
	/// Provides static methods to read system icons for both folders and files.
	/// </summary>
	/// <example>
	/// <code>IconReader.GetFileIcon("c:\\general.xls");</code>
	/// </example>
	public static class IconReader {
		#region Enums
		/// <summary>
		/// Options to specify the size of icons to return.
		/// </summary>
		[Flags]
		public enum IconSize : uint {

			/// <summary>
			/// Specify large icon - 32 pixels by 32 pixels.
			/// </summary>
			Large = 0x0,
			/// <summary>
			/// Specify small icon - 16 pixels by 16 pixels.
			/// </summary>
			Small = 0x1,
			/// <summary>
			/// Specify extra large icon - 48 pixels by 48 pixels.
			/// Only available under XP and latter; other OS return the Large Icon ImageList.
			/// </summary>
			ExtraLarge = 0x2,
			/// <summary>
			/// These images are the size specified by GetSystemMetrics called with SM_CXSMICON and GetSystemMetrics called with SM_CYSMICON.
			/// </summary>
			SysSmall = 0x3,
			/// <summary>
			/// Windows Vista and later. The image is normally 256x256 pixels.
			/// </summary>
			Jumbo = 0x4
		}
		//  IID_IImageList: TGUID= '{46EB5926-582E-4017-9FDF-E8998DAA0950}';
		/// <summary>
		/// Options to specify whether folders should be in the open or closed state.
		/// </summary>
		public enum FolderType : uint {
			/// <summary>
			/// Specify open folder.
			/// </summary>
			Open = 0,
			/// <summary>
			/// Specify closed folder.
			/// </summary>
			Closed = 1
		}
		#endregion
		#region Structs
		/// <summary>
		/// Structure that encapsulates basic information of icon embedded in a file.
		/// </summary>
		public struct EmbeddedIconInfo {
			public string FileName;
			public int IconIndex;
		}
		#endregion
		#region ExtractIcon
		/// <summary>
		/// Extract the icon from file giving an index.
		/// </summary>
		/// <param name="path">Path to file.</param>
		/// <param name="index">Icon index.</param>
		/// <returns>This method always returns an icon.</returns>
		public static Icon ExtractIconFromFile ( string path, int index ) {
			//Win32 
			//Extract the icon handle 
			var hIcon = Shell32.ExtractIcon ( Process.GetCurrentProcess ( ).Handle, Environment.ExpandEnvironmentVariables ( path ), index );
			//Get icon form .dll or .exe 
			return hIcon == IntPtr.Zero ? null : GetManagedIcon ( hIcon );
		}

		/// <summary>
		/// Extract the icon from file.
		/// </summary>
		/// <param name="path">File path, 
		/// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
		/// <returns>This method always returns the large size of the icon (may be 32x32 px).</returns>
		public static Icon ExtractIconFromFile ( string path ) {
			var embeddedIcon = GetEmbeddedIconInfo ( Environment.ExpandEnvironmentVariables ( path ) );
			//Gets the handle of the icon.
			return ExtractIconFromFile ( embeddedIcon.FileName, embeddedIcon.IconIndex );
		}

		/// <summary>
		/// Extract the icon from file, and return icon information
		/// </summary>
		/// <param name="path">File path, 
		/// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
		/// <param name="size">The desired icon size</param>
		/// <param name="shfi">The icon size</param>
		/// <returns>This method always returns an icon with the especified size and thier information.</returns>
		public static Icon ExtractIconFromFileEx ( string path, IconSize size, ref Shell32.SHFILEINFO shfi ) {
			var embeddedIcon = GetEmbeddedIconInfo ( Environment.ExpandEnvironmentVariables ( path ) );
			//Gets the handle of the icon.
			return GetFileIcon ( embeddedIcon.FileName, size, false, ref shfi );
		}

		/// <summary>
		/// Extract the icon from file.
		/// </summary>
		/// <param name="path">File path, 
		/// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
		/// <param name="isLarge">
		/// Determines the returned icon is a large (may be 32x32 px) 
		/// or small icon (16x16 px).</param>
		public static Icon ExtractIconFromFile ( string path, bool isLarge ) {
			var hDummy = new IntPtr[] { IntPtr.Zero };
			var hIconEx = new IntPtr[] { IntPtr.Zero };

			try {
				var embeddedIcon = GetEmbeddedIconInfo ( Environment.ExpandEnvironmentVariables ( path ) );
				var readIconCount = isLarge ? Shell32.ExtractIconEx ( embeddedIcon.FileName, 0, hIconEx, hDummy, 1 ) : Shell32.ExtractIconEx ( embeddedIcon.FileName, 0, hDummy, hIconEx, 1 );
				if ( readIconCount > 0 && hIconEx[0] != IntPtr.Zero ) {
					// Get first icon.
					return GetManagedIcon ( hIconEx[0] );
				} else // No icon read
					return null;
			} catch ( Exception exc ) {
				// Extract icon error.
				throw new ApplicationException ( "Could not extract icon", exc );
			} finally {
				// Release resources.
				foreach ( var ptr in hIconEx.Where ( ptr => ptr != IntPtr.Zero ) )
					User32.DestroyIcon ( ptr );
				foreach ( var ptr in hDummy.Where ( ptr => ptr != IntPtr.Zero ) )
					User32.DestroyIcon ( ptr );
			}
		}
		/// <summary>
		/// Extract all icons from a file
		/// </summary>
		/// <param name="path">File path</param>
		/// <param name="isLarge">Large (32x32) or Small (16x16) icon</param>
		/// <returns>Icon[] array</returns>
		public static Icon[] ExtractIconsFromFile ( string path, bool isLarge ) {
			path = Environment.ExpandEnvironmentVariables ( path );
			var iconsCount = (int)GetTotalIcons ( path );
			//checks how many icons.

			var iconPtr = new IntPtr[iconsCount];

			//extracts the icons by the size that was selected.

			var readIconCount = isLarge ? Shell32.ExtractIconEx ( path, 0, iconPtr, null, iconsCount ) : Shell32.ExtractIconEx ( path, 0, null, iconPtr, iconsCount );

			var iconList = new Icon[iconsCount];

			//gets the icons in a list.

			for ( var i = 0; i < iconsCount; i++ )
				if ( iconPtr[i] != IntPtr.Zero )
					iconList[i] = GetManagedIcon ( iconPtr[i] );
			return iconList;
		}

		/// <summary>
		/// Extract a icon from assembly
		/// </summary>
		/// <param name="resourceName">Icon resource name in assembly.</param>
		/// <returns>Icon for the resourceName, null if not exists</returns>
		public static Icon ExtractIconFromResource ( string resourceName ) {
			var assembly = Assembly.GetCallingAssembly ( );
			return assembly == null ? null : new Icon ( assembly.GetManifestResourceStream ( resourceName ) );
		}

		#endregion
		#region Get all files extensions from regedit
		/// <summary>
		/// Gets registered file types and their associated icon in the system.
		/// </summary>
		/// <returns>Returns a Dictionary which contains the file extension as keys, the icon file and param as values.</returns>
		public static Dictionary<string, string> GetFileTypeAndIcon ( ) {
			try {
				// Create a registry key object to represent the HKEY_CLASSES_ROOT registry section
				var rkRoot = Registry.ClassesRoot;

				//Gets all sub keys' names.
				var keyNames = rkRoot.GetSubKeyNames ( ).Where ( k => k.StartsWith(".") );
				var iconsInfo = new Dictionary<string, string> ( );

				//Find the file icon.
				foreach ( var keyName in keyNames ) {
					if ( String.IsNullOrEmpty ( keyName ) )
						continue;

					var rkFileType = rkRoot.OpenSubKey ( keyName );
					if ( rkFileType == null )
						continue;

					//Gets the default value of this key that contains the information of file type.
					var defaultValue = rkFileType.GetValue ( "" );
					if ( defaultValue == null )
						continue;

					if ( keyName == ".ascx" ) {
						Console.WriteLine("ASCX");
					}

					//Go to the key that specifies the default icon associates with this file type.
					var defaultIcon = string.Format ( "{0}\\DefaultIcon", defaultValue );
					var rkFileIcon = rkRoot.OpenSubKey ( defaultIcon );
					if ( rkFileIcon != null ) {
						//Get the file contains the icon and the index of the icon in that file.
						var value = rkFileIcon.GetValue ( "" );
						if ( value != null ) {
							//Clear all unnecessary " sign in the string to avoid error.
							var fileParam = value.ToString ( ).Replace ( "\"", "" );
							iconsInfo.Add ( keyName, fileParam );
						}
						rkFileIcon.Close ( );
					}
					rkFileType.Close ( );
				}
				rkRoot.Close ( );
				return iconsInfo;
			} catch ( Exception exc ) {
				throw;
			}
		}
		#endregion
		#region Utils
		/// <summary>
		/// Parses the parameters string to the structure of EmbeddedIconInfo.
		/// </summary>
		/// <param name="fileAndParam">The params string, 
		/// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
		/// <returns></returns>
		public static EmbeddedIconInfo GetEmbeddedIconInfo ( string fileAndParam ) {
			var embeddedIcon = new EmbeddedIconInfo ( );
			if ( string.IsNullOrEmpty ( fileAndParam ) )
				return embeddedIcon;

			//Use to store the file contains icon.
			string fileName;

			//The index of the icon in the file.
			var iconIndex = 0;
			var iconIndexString = String.Empty;

			var commaIndex = fileAndParam.IndexOf ( "," );
			//if fileAndParam is some thing likes that: "C:\\Program Files\\NetMeeting\\conf.exe,1".
			if ( commaIndex > 0 ) {
				fileName = fileAndParam.Substring ( 0, commaIndex );
				iconIndexString = fileAndParam.Substring ( commaIndex + 1 );
			} else
				fileName = fileAndParam;

			if ( !string.IsNullOrWhiteSpace ( iconIndexString ) ) {
				//Get the index of icon.
				iconIndex = int.Parse ( iconIndexString );
				//if ( iconIndex < 0 )
				//	iconIndex = 0;  //To avoid the invalid index.
			}
			Console.WriteLine("Getting icon from file: {0},{1}",fileName,iconIndex);
			embeddedIcon.FileName = Path.IsPathRooted(fileName) ? fileName : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), fileName);
			embeddedIcon.IconIndex = iconIndex;

			return embeddedIcon;
		}
		#endregion
		#region Get Methods
		/// <summary>
		/// Get total icons on a file
		/// </summary>
		/// <param name="path">Path to a file</param>
		/// <example>
		/// <code>IconReader.GetTotalIcons("C:\\Windows\\system32\\shell32.dll");</code>
		/// </example>
		/// <returns>Total icon on selected file</returns>
		public static uint GetTotalIcons ( string path ) {
			return Shell32.ExtractIconEx ( Environment.ExpandEnvironmentVariables ( path ), -1, null, null, 0 );
		}

		/// <summary>
		/// Get managed icon from a unmanaged one,
		/// Copy (clone) the returned icon to a new object, thus allowing us to clean-up properly
		/// </summary>
		/// <param name="unmanagedIcon">Unmanaged icon</param>
		/// <returns>Managed icon instance</returns>
		public static Icon GetManagedIcon ( ref Icon unmanagedIcon ) {
			var managedIcon = (Icon)unmanagedIcon.Clone ( );
			User32.DestroyIcon ( unmanagedIcon.Handle );
			return managedIcon;
		}

		/// <summary>
		/// Get managed icon from a IntPtr pointer,
		/// Copy (clone) the returned icon to a new object, thus allowing us to clean-up properly
		/// </summary>
		/// <param name="pointer">Icon Handler</param>
		/// <returns>Managed icon instance</returns>
		public static Icon GetManagedIcon ( IntPtr pointer ) {
			var managedIcon = (Icon)Icon.FromHandle ( pointer ).Clone ( );
			User32.DestroyIcon ( pointer );
			return managedIcon;
		}
		#endregion
		#region GetFileIcon
		/// <summary>
		/// Returns an icon for a given file - indicated by the name parameter.
		/// </summary>
		/// <param name="name">Extension or pathname for file.</param>
		/// <param name="size">Large or small</param>
		/// <param name="linkOverlay">Whether to include the link icon</param>
		/// <param name="shfi">Return File Information</param>
		/// <returns>System.Drawing.Icon</returns>
		public static Icon GetFileIcon ( string name, IconSize size, bool linkOverlay, ref Shell32.SHFILEINFO shfi ) {
			name = Environment.ExpandEnvironmentVariables ( name );
			shfi = new Shell32.SHFILEINFO ( );
			var flags = Shell32.SHGetFileInfoConstants.SHGFI_TYPENAME | Shell32.SHGetFileInfoConstants.SHGFI_DISPLAYNAME | Shell32.SHGetFileInfoConstants.SHGFI_ICON | Shell32.SHGetFileInfoConstants.SHGFI_SHELLICONSIZE | Shell32.SHGetFileInfoConstants.SHGFI_SYSICONINDEX | Shell32.SHGetFileInfoConstants.SHGFI_USEFILEATTRIBUTES;

			if ( linkOverlay ) flags |= Shell32.SHGetFileInfoConstants.SHGFI_LINKOVERLAY;
			/* Check the size specified for return. */
			if ( IconSize.Small == size )
				flags |= Shell32.SHGetFileInfoConstants.SHGFI_SMALLICON;
			else
				flags |= Shell32.SHGetFileInfoConstants.SHGFI_LARGEICON;

			var hIml = Shell32.SHGetFileInfo ( name,
					Shell32.FILE_ATTRIBUTE.NORMAL,
					ref shfi,
					(uint)Marshal.SizeOf ( shfi ),
					flags );

			if ( shfi.hIcon == IntPtr.Zero ) return null;
			if ( !Utils.IsXpOrAbove ( ) ) return GetManagedIcon ( shfi.hIcon );
			// Get the System IImageList object from the Shell:
			var iidImageList = new Guid ( "46EB5926-582E-4017-9FDF-E8998DAA0950" );
			Shell32.IImageList iImageList = null;
			var ret = Shell32.SHGetImageList (
					(int)size,
					ref iidImageList,
					ref iImageList
					);
			// the image list handle is the IUnknown pointer, but
			// using Marshal.GetIUnknownForObject doesn't return
			// the right value.  It really doesn't hurt to make
			// a second call to get the handle:
			Shell32.SHGetImageListHandle ( (int)size, ref iidImageList, ref hIml );

			var hIcon = IntPtr.Zero;
			if ( iImageList == null ) {
				hIcon = Comctl32.ImageList_GetIcon (
						hIml,
						shfi.iIcon,
						(int)Comctl32.ImageListDrawItemConstants.ILD_TRANSPARENT );
			} else {
				iImageList.GetIcon (
						shfi.iIcon,
						(int)Comctl32.ImageListDrawItemConstants.ILD_TRANSPARENT,
						ref hIcon );
			}
			return hIcon == IntPtr.Zero ? GetManagedIcon ( shfi.hIcon ) : GetManagedIcon ( hIcon );
		}
		#endregion
		#region GetFolderIcon
		/// <summary>
		/// Used to access system folder icons.
		/// </summary>
		/// <param name="size">Specify large or small icons.</param>
		/// <param name="folderType">Specify open or closed FolderType.</param>
		/// <param name="shfi">Return Folder Information</param>
		/// <returns>System.Drawing.Icon</returns>
		public static Icon GetFolderIcon ( IconSize size, FolderType folderType, ref Shell32.SHFILEINFO shfi ) {
			// Need to add size check, although errors generated at present!
			var flags = Shell32.SHGetFileInfoConstants.SHGFI_TYPENAME | Shell32.SHGetFileInfoConstants.SHGFI_DISPLAYNAME | Shell32.SHGetFileInfoConstants.SHGFI_ICON | Shell32.SHGetFileInfoConstants.SHGFI_USEFILEATTRIBUTES;

			if ( FolderType.Open == folderType )
				flags |= Shell32.SHGetFileInfoConstants.SHGFI_OPENICON;

			if ( IconSize.Small == size )
				flags |= Shell32.SHGetFileInfoConstants.SHGFI_SMALLICON;
			else
				flags |= Shell32.SHGetFileInfoConstants.SHGFI_LARGEICON;

			IntPtr hIml;
			// Get the folder icon
			shfi = new Shell32.SHFILEINFO ( );
			if ( Utils.IsSevenOrAbove ( ) ) // Windows 7 FIX
            {
				hIml = Shell32.SHGetFileInfo ( Environment.GetFolderPath ( Environment.SpecialFolder.System ),
				Shell32.FILE_ATTRIBUTE.DIRECTORY,
				ref shfi,
				(uint)Marshal.SizeOf ( shfi ),
				flags );
			} else {
				hIml = Shell32.SHGetFileInfo ( null,
				Shell32.FILE_ATTRIBUTE.DIRECTORY,
				ref shfi,
				(uint)Marshal.SizeOf ( shfi ),
				flags );
			}
			if ( shfi.hIcon == IntPtr.Zero ) return null;
			if ( !Utils.IsXpOrAbove ( ) ) return GetManagedIcon ( shfi.hIcon );
			// Get the System IImageList object from the Shell:
			var iidImageList = new Guid ( "46EB5926-582E-4017-9FDF-E8998DAA0950" );
			Shell32.IImageList iImageList = null;
			var ret = Shell32.SHGetImageList (
					(int)size,
					ref iidImageList,
					ref iImageList
					);

			// the image list handle is the IUnknown pointer, but
			// using Marshal.GetIUnknownForObject doesn't return
			// the right value.  It really doesn't hurt to make
			// a second call to get the handle:
			Shell32.SHGetImageListHandle ( (int)size, ref iidImageList, ref hIml );
			var hIcon = IntPtr.Zero;
			if ( iImageList == null ) {
				hIcon = Comctl32.ImageList_GetIcon (
					 hIml,
					 shfi.iIcon,
					 (int)Comctl32.ImageListDrawItemConstants.ILD_TRANSPARENT );
			} else {
				iImageList.GetIcon (
					 shfi.iIcon,
					 (int)Comctl32.ImageListDrawItemConstants.ILD_TRANSPARENT,
					 ref hIcon );
			}
			return hIcon == IntPtr.Zero ? GetManagedIcon ( shfi.hIcon ) : GetManagedIcon ( hIcon );
		}
		#endregion
	}
}
