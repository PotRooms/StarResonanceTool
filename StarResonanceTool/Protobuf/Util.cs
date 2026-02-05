// COPYRIGHT 2025 PotRooms

using System.Text;

namespace ProtoDescDumper;

// Common helper utilities shared across the core layer.
static class Util
{
    public static string ToLiteral( string input )
    {
        StringBuilder literal = new( input.Length + 2 );
        literal.Append( '"' );
        foreach ( var c in input )
        {
            switch ( c )
            {
                case '\a':
                    literal.Append( @"\a" );
                    break;
                case '\b':
                    literal.Append( @"\b" );
                    break;
                case '\f':
                    literal.Append( @"\f" );
                    break;
                case '\n':
                    literal.Append( @"\n" );
                    break;
                case '\r':
                    literal.Append( @"\r" );
                    break;
                case '\t':
                    literal.Append( @"\t" );
                    break;
                case '\v':
                    literal.Append( @"\v" );
                    break;
                case '\\':
                    literal.Append( @"\\" );
                    break;
                case '\"':
                    literal.Append( "\\\"" );
                    break;
                default:
                    literal.Append( c );
                    break;
            }
        }

        literal.Append( '"' );
        return literal.ToString();
    }
}
