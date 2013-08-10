using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Fyles.Extensions;
using Microsoft.Win32;

namespace Fyles {
	class Program {
		private const string FYLES = "fyles";
		private const String DEFAULT = "___DEFAULT___";
		static void Main ( string[] args ) {
			var path = Path.Combine ( Path.GetDirectoryName ( Assembly.GetAssembly ( typeof ( Program ) ).Location ), "Output" );
			var directory = new DirectoryInfo ( path );
			if ( !directory.Exists ) {
				directory.Create ( );
			}
			// all the icons
			var icons = IconReader.GetFileTypeAndIcon ( );

			// add special UNKNOWN/DEFAULT
			icons.Add ( ".{0}".With ( DEFAULT ), "" );

			// the widths of the icons
			var widths = new List<int> ( );

			// save the hashes of each icon
			var hashes = new Dictionary<String, String> ( );
			// a place to track the dupes so we can delete them.
			var dupes = new Dictionary<String, List<String>> ( );

			foreach ( var icon in icons ) {
				foreach ( var esize in Enum.GetValues ( typeof ( IconReader.IconSize ) ) ) {
					var shfi = new Fyles.WinApi.Shell32.SHFILEINFO ( );

					using ( var ico = IconReader.ExtractIconFromFileEx ( FYLES + icon.Key, (IconReader.IconSize)esize, ref shfi ) ) {
						if ( ico == null ) {
							continue;
						}

						var size = String.Format ( "{0}", ico.Height, ico.Width );
						if ( !widths.Contains ( ico.Width ) ) {
							widths.Add ( ico.Width );
						}
						using ( var img = ico.ToBitmap ( ) ) {
							var iconKey = icon.Key.Substring ( 1 );
							var iks = String.Format ( "{0}-{1}.png", iconKey, size );
							if ( hashes.ContainsKey ( iks ) ) {
								continue;
							}
							var filename = Path.Combine ( directory.FullName, iks.PathSafe ( ) );
							Console.WriteLine ( "Saving: {0}", Path.GetFileNameWithoutExtension ( filename ) );
							var hash = Hash ( img );
							if ( hashes.ContainsValue ( hash ) && iconKey != DEFAULT ) {
								if ( dupes.ContainsKey ( hash ) ) {
									dupes[hash].Add ( Path.GetFileNameWithoutExtension ( iks ) );
								} else {
									dupes.Add ( hash, new List<string> { Path.GetFileNameWithoutExtension ( iks ) } );
								}
							}

							hashes.Add ( iks, hash );

							img.Save ( filename, ImageFormat.Png );
						}
					}
				}

			}
			Console.WriteLine ( "Remove Dupes..." );
			foreach ( var f in dupes.SelectMany ( dupe => dupe.Value ) ) {
				hashes.Remove ( String.Format ( "{0}.png", f ) );
				// create the map
				File.Delete ( Path.Combine ( directory.FullName, String.Format ( "{0}.png", f ) ) );
			}

			var classList = new Dictionary<String, String> ( );
			foreach ( var dupe in dupes ) {
				var hashKey = hashes.First ( h => h.Value == dupe.Key ).Key;
				var classKey = Path.GetFileNameWithoutExtension ( hashKey );
				hashes.Remove ( hashKey );
				var classes = String.Format ( ".{2}-{0}, .{2}-{1}", classKey, String.Join ( String.Format ( ", .{0}-", FYLES ), dupe.Value ), FYLES );
				Console.WriteLine ( "Adding Group: {0}", hashKey );
				classList.Add ( classKey, classes );
			}

			var remainingFiles = directory.GetFiles ( ).OrderBy
				( f => {
					var fn = Path.GetFileNameWithoutExtension ( f.Name );
					return fn.Substring ( 0, fn.LastIndexOf ( "-" ) );
				} ).ThenBy ( f => {
					var name = Path.GetFileNameWithoutExtension ( f.Name );
					return int.Parse ( name.Substring ( name.LastIndexOf ( "-" ) + 1 ) );
				} );

			const int perRow = 6;
			const int padding = 5;

			var rowCount = (int)Math.Round ( (double)remainingFiles.Count ( ) / ( (double)widths.Count * perRow ) );
			var totalPadding = widths.Count * padding;


			var maxWidth = ( widths.Sum ( ) + totalPadding ) * perRow;
			var rowHeight = widths.Max ( ) + padding;
			var maxHeight = rowHeight * rowCount;

			var defaultPositions = new Dictionary<int, Point> ( );

			using ( var sprite = new Bitmap ( maxWidth, maxHeight ) ) {

				using ( var graphics = Graphics.FromImage ( sprite ) ) {
					using (
						var css = new FileStream ( Path.Combine ( directory.Parent.FullName, FYLES + ".css" ), FileMode.Create,
																			 FileAccess.Write ) ) {
						using ( var writer = new StreamWriter ( css ) ) {

							var line = new StringBuilder ( );
							line.AppendFormat ( ".{0}{{background-image: url(../images/{0}.png); background-repeat: no-repeat; display: inline-block;}}{1}", FYLES, Environment.NewLine );
							var location = new Point ( 0, 0 );
							var mainLines = new StringBuilder ( );
							foreach ( var file in remainingFiles ) {
								var fn = Path.GetFileNameWithoutExtension ( file.Name );
								var name = fn.Substring ( 0, fn.LastIndexOf ( "-" ) );
								var size =
									int.Parse ( fn.Substring ( fn.LastIndexOf ( "-" ) + 1 ) );

								if ( name == DEFAULT ) {
									defaultPositions.Add ( size, location );
								}

								Console.WriteLine ( "Adding {0} to sprite", name );

								using ( var fbmp = new Bitmap ( file.FullName ) ) {
									graphics.DrawImage ( fbmp, location );
								}
								var selectorKey = String.Format ( "{0}-{1}", name, size );
								var selector = classList.ContainsKey ( selectorKey )
																 ? classList[selectorKey]
																 : String.Format ( ".{0}-{1}", FYLES, selectorKey );
								mainLines.AppendFormat ( "{0} {{", selector.ToLower ( ) );
								mainLines.AppendFormat ( "background-position: {0}{1} {2}{3}; height: {4}px; width: {4}px;", -location.X, location.X == 0 ? "" : "px",
																		-location.Y, location.Y == 0 ? "" : "px", size );
								mainLines.Append ( "}" );
								location.Offset ( size + padding, 0 );
								if ( location.X >= maxWidth ) {
									location.X = 0;
									location.Offset ( 0, rowHeight );
								}

								Console.WriteLine ( "Location: {0}", location );
							}

							foreach ( var w in widths ) {
								var pos = defaultPositions[w];
								line.AppendFormat ( ".{0}-{1}{{height: {1}px; width: {1}px; background-position: {2}px {3}px;}}{4}", FYLES, w,
																		-pos.X, -pos.Y, Environment.NewLine );
							}

							writer.WriteLine ( line );
							writer.Write(mainLines);

						}
					}
				}
				sprite.Save ( Path.Combine ( directory.Parent.FullName, FYLES + ".png" ), ImageFormat.Png );
			}

			Console.WriteLine ( "Press {ENTER} to exit..." );
			Console.ReadLine ( );
		}

		static String Hash ( Image img ) {
			// get the bytes from the image
			byte[] bytes = null;
			using ( var ms = new MemoryStream ( ) ) {
				img.Save ( ms, ImageFormat.Gif ); // gif for example
				bytes = ms.ToArray ( );
			}

			// hash the bytes
			var md5 = new MD5CryptoServiceProvider ( );
			byte[] hash = md5.ComputeHash ( bytes );

			// make a hex string of the hash for display or whatever
			var sb = new StringBuilder ( );
			foreach ( var b in hash ) {
				sb.Append ( b.ToString ( "x2" ).ToLower ( ) );
			}
			return sb.ToString ( );
		}
	}
}
