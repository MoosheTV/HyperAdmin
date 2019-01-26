using System;
using System.Text;

namespace HyperAdmin.Shared
{
	public static class StringExtents
	{
		public static string ToTitleCase( this string s ) {
			if( s == null ) return null;

			var words = s.AddSpacesToCamelCase().Split( ' ' );
			for( var i = 0; i < words.Length; i++ ) {
				if( words[i].Length == 0 ) continue;

				var firstChar = char.ToUpper( words[i][0] );
				var rest = "";
				if( words[i].Length > 1 ) {
					rest = words[i].Substring( 1 ).ToLower();
				}
				words[i] = firstChar + rest;
			}
			return string.Join( " ", words );
		}

		public static string AddSpacesToCamelCase( this string s ) {
			var chars = s.ToCharArray();
			var sb = new StringBuilder();
			foreach( var c in chars ) {
				if( char.IsUpper( c ) ) {
					sb.Append( $" {c}" );
				}
				else {
					sb.Append( c );
				}
			}
			return sb.ToString().Trim();
		}

		public static string GetTimeLeft( this DateTime futureTime ) {
			if( futureTime == DateTime.MaxValue ) {
				return "Never";
			}

			var now = futureTime.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now;
			if( futureTime < now ) {
				return "Now";
			}

			var total = (futureTime - now).TotalSeconds;
			var text = "";
			var days = $"{total / 86400f:n0} Days ";
			var hours = $"{total / 3600f % 24f:n0} Hours ";
			var mins = $"{total / 60f % 60f:n0} Minutes ";

			if( total >= 86400 )
				text += days;
			if( total >= 3600 )
				text += hours;
			if( total >= 60 )
				text += mins;
			return text.Trim();
		}
	}
}
